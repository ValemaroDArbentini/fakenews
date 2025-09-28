using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;

namespace TelegramBlock.Controllers
{
    [ApiController]
    [Route("lexemes")]
    public class LexemesController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<LexemesController> _log;
        private readonly string? _adminToken;

        public LexemesController(GameDbContext db, ILogger<LexemesController> log, IConfiguration _)
        {
            _db = db;
            _log = log;
            _adminToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN");
        }

        // ---------- admin token ----------
        private static bool SecureEquals(string? a, string? b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            var ab = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            try { return CryptographicOperations.FixedTimeEquals(ab, bb); }
            finally { Array.Clear(ab, 0, ab.Length); Array.Clear(bb, 0, bb.Length); }
        }

        private IActionResult? CheckAdmin()
        {
            if (string.IsNullOrEmpty(_adminToken))
                return StatusCode(503, new { error = "admin_token_unset" });

            if (!Request.Headers.TryGetValue("X-Admin-Token", out var h) ||
                !SecureEquals(h.ToString(), _adminToken))
                return Unauthorized(new { error = "admin_token_invalid" });

            return null;
        }

        // ---------- GET: list ----------
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] int? len, [FromQuery] string? q,
                                             [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _db.Lexemes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(locale)) query = query.Where(x => x.Locale == locale);
            if (len is >= 1 and <= 5) query = query.Where(x => x.Word.Length == len);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = q.Trim().ToUpperInvariant().Replace('*', '%');
                query = query.Where(x => EF.Functions.ILike(x.Word, like));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Locale).ThenBy(x => x.Word)
                .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 500))
                .Take(Math.Clamp(pageSize, 1, 500))
                .Select(x => new { x.Id, x.Word, x.PartOfSpeech, x.Locale })
                .ToListAsync();

            return Ok(new { total, items });
        }

        // ---------- POST: single add ----------
        public record UpsertDto(string Word, string PartOfSpeech, string Locale);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] UpsertDto dto)
        {
            var guard = CheckAdmin(); if (guard != null) return guard;

            var (ok, word, pos, loc, reason) = Normalize(dto.Word, dto.PartOfSpeech, dto.Locale);
            if (!ok) return BadRequest(new { error = reason });

            if (await _db.Lexemes.AnyAsync(x => x.Locale == loc && x.Word == word))
                return Conflict(new { error = "duplicate" });

            _db.Lexemes.Add(new Lexeme { Word = word, PartOfSpeech = pos, Locale = loc });
            await _db.SaveChangesAsync();

            var id = await _db.Lexemes.Where(x => x.Locale == loc && x.Word == word).Select(x => x.Id).FirstAsync();
            return Ok(new { id });
        }

        // ---------- POST: bulk import ----------
        // ЕДИНОЕ тело запроса — multipart/form-data (Swagger покажет поля: file и payload).
        // При желании, можно слать сырой NDJSON/CSV/JSON: если form пуст, читаем Request.Body.
        public sealed class BulkForm
        {
            public IFormFile? File { get; set; }     // файл .ndjson/.csv/.txt
            public string? Payload { get; set; }     // текст прямо в форме (ndjson/csv/plain)
        }

        [HttpPost("bulk")]
        [Consumes("multipart/form-data", "application/x-ndjson", "application/json", "text/plain", "text/csv")]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Bulk(
            [FromQuery] string locale = "ru",
            [FromQuery(Name = "pos")] string defaultPos = "noun",
            [FromQuery] bool dryRun = false,
            [FromForm] BulkForm? form = null
        )
        {
            var guard = CheckAdmin(); if (guard != null) return guard;

            // 1) Чтение входа
            string text = string.Empty;
            string contentType = (Request.ContentType ?? string.Empty).ToLowerInvariant();

            if (form?.File is not null && form.File.Length > 0)
            {
                using var sr = new StreamReader(form.File.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                text = await sr.ReadToEndAsync();
                contentType = form.File.ContentType?.ToLowerInvariant() ?? contentType;
            }
            else if (!string.IsNullOrWhiteSpace(form?.Payload))
            {
                text = form!.Payload!;
            }
            else
            {
                // Сырой NDJSON/CSV/JSON — curl --data-binary и т.п.
                using var sr = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                text = await sr.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "empty_payload" });

            // 2) Детект формата и парсинг "сырья"
            var rows = new List<(string? word, string? pos, string? loc)>();
            bool looksJsonArray = text.TrimStart().StartsWith("[");
            bool looksJsonObject = text.TrimStart().StartsWith("{");

            if ((contentType.Contains("json") && looksJsonArray))
            {
                // JSON-массив объектов
                try
                {
                    var arr = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(text);
                    if (arr != null)
                        foreach (var o in arr)
                            rows.Add((
                                o?.GetValueOrDefault("word"),
                                o?.GetValueOrDefault("partOfSpeech"),
                                o?.GetValueOrDefault("locale")
                            ));
                }
                catch { return BadRequest(new { error = "invalid_json_array" }); }
            }
            else if (contentType.Contains("json") || looksJsonObject)
            {
                // NDJSON
                                foreach (var line in SplitLines(text))
                                    {
                                        if (string.IsNullOrWhiteSpace(line)) continue;
                                        try
                    {
                        var o = JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                                                if (o is null) continue; // безопасный скип пустых/битых строк
                        
                        o.TryGetValue("word", out var w);
                        o.TryGetValue("partOfSpeech", out var p);
                        o.TryGetValue("locale", out var l);
                        
                        rows.Add((w, p, l));
                                            }
                                        catch { /* skip */ }
                }
            }
            else if (contentType.Contains("csv"))
            {
                // CSV (auto delimiter, header optional)
                var lines = SplitLines(text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                if (lines.Count > 0)
                {
                    char delim = DetectDelimiter(lines[0]);
                    bool hasHeader = HeaderLooksLikeCsv(lines[0], delim);
                    foreach (var line in hasHeader ? lines.Skip(1) : lines)
                    {
                        var cells = SplitCsv(line, delim);
                        rows.Add((cells.ElementAtOrDefault(0), cells.ElementAtOrDefault(1), cells.ElementAtOrDefault(2)));
                    }
                }
            }
            else
            {
                // text/plain — по строке слово
                foreach (var line in SplitLines(text))
                    if (!string.IsNullOrWhiteSpace(line))
                        rows.Add((line, null, null));
            }

            if (rows.Count == 0)
                return BadRequest(new { error = "no_rows_parsed" });

            // 3) Нормализация и дедупликация в пачке
            var prepared = new List<(string word, string pos, string loc)>();
            int skippedTooShort = 0, skippedTooLong = 0, skippedInvalid = 0, skippedDuplicateInPayload = 0, skippedDuplicateInDb = 0;

            var seen = new HashSet<(string loc, string word)>(StringTupleComparer.Ordinal);
            foreach (var (w, p, l) in rows)
            {
                var (ok, nw, np, nl, reason) = Normalize(w, p ?? defaultPos, l ?? locale);
                if (!ok)
                {
                    switch (reason)
                    {
                        case "length_lt_1": skippedTooShort++; break;
                        case "length_gt_5": skippedTooLong++; break;
                        default: skippedInvalid++; break;
                    }
                    continue;
                }
                var key = (nl, nw);
                if (!seen.Add(key)) { skippedDuplicateInPayload++; continue; }
                prepared.Add((nw, np, nl));
            }

            if (prepared.Count == 0)
                return Ok(new
                {
                    added = 0,
                    skippedTooShort,
                    skippedTooLong,
                    skippedInvalid,
                    skippedDuplicateInPayload,
                    skippedDuplicateInDb = 0
                });

            // 4) Проверка дублей против БД (одним запросом)
            var locales = prepared.Select(x => x.loc).Distinct().ToArray();
            var existing = await _db.Lexemes
                .Where(l => locales.Contains(l.Locale))
                .Select(l => new { l.Locale, l.Word })
                .ToListAsync();

            var existingSet = new HashSet<(string loc, string word)>(
                existing.Select(e => (e.Locale, e.Word)),
                StringTupleComparer.Ordinal);

            var toInsert = prepared.Where(x => !existingSet.Contains((x.loc, x.word))).ToList();
            skippedDuplicateInDb = prepared.Count - toInsert.Count;

            if (dryRun)
            {
                return Ok(new
                {
                    added = toInsert.Count,
                    dryRun = true,
                    skippedTooShort,
                    skippedTooLong,
                    skippedInvalid,
                    skippedDuplicateInPayload,
                    skippedDuplicateInDb
                });
            }

            // 5) Вставка пачкой
            using var tx = await _db.Database.BeginTransactionAsync();
            await _db.Lexemes.AddRangeAsync(toInsert.Select(x => new Lexeme { Word = x.word, PartOfSpeech = x.pos, Locale = x.loc }));
            var added = await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                added,
                skippedTooShort,
                skippedTooLong,
                skippedInvalid,
                skippedDuplicateInPayload,
                skippedDuplicateInDb
            });
        }

        // ---------- helpers ----------

        private static (bool ok, string word, string pos, string loc, string reason) Normalize(string? word, string? pos, string? loc)
        {
            string w = (word ?? "").Trim().ToUpperInvariant();
            string p = string.IsNullOrWhiteSpace(pos) ? "noun" : pos!.Trim().ToLowerInvariant();
            string l = string.IsNullOrWhiteSpace(loc) ? "ru" : loc!.Trim().ToLowerInvariant();

            if (w.Length < 1) return (false, "", "", "", "length_lt_1");
            if (w.Length > 5) return (false, "", "", "", "length_gt_5");

            bool lettersOnly = w.All(ch => char.IsLetter(ch) || ch == '-' || ch == '’' || ch == '\'');
            if (!lettersOnly) return (false, "", "", "", "invalid_chars");

            return (true, w, p, l, "");
        }

        private static IEnumerable<string> SplitLines(string s)
            => s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.None);

        private static bool HeaderLooksLikeCsv(string line, char delim)
        {
            var h = SplitCsv(line, delim).Select(c => c.Trim().ToLowerInvariant()).ToArray();
            return h.Contains("word") || h.Contains("partofspeech") || h.Contains("locale");
        }

        private static char DetectDelimiter(string line)
        {
            int c = line.Count(ch => ch == ',');
            int s = line.Count(ch => ch == ';');
            int t = line.Count(ch => ch == '\t');
            if (t >= c && t >= s) return '\t';
            if (s >= c && s >= t) return ';';
            return ',';
        }

        private static readonly Regex CsvCellRx = new("\"([^\"]*)\"|([^\";,\t]+)", RegexOptions.Compiled);
        private static List<string> SplitCsv(string line, char delim)
        {
            var result = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '\"')
                {
                    int j = i + 1;
                    var sb = new StringBuilder();
                    while (j < line.Length)
                    {
                        if (line[j] == '\"')
                        {
                            if (j + 1 < line.Length && line[j + 1] == '\"') { sb.Append('\"'); j += 2; continue; }
                            j++; break;
                        }
                        sb.Append(line[j]); j++;
                    }
                    result.Add(sb.ToString());
                    while (j < line.Length && line[j] != delim) j++;
                    if (j < line.Length && line[j] == delim) j++;
                    i = j;
                }
                else
                {
                    int j = i;
                    while (j < line.Length && line[j] != delim) j++;
                    result.Add(line[i..j].Trim());
                    i = (j < line.Length && line[j] == delim) ? j + 1 : j;
                }
            }
            if (result.Count > 0) result[0] = result[0].Trim('\uFEFF'); // BOM
            return result;
        }

        private sealed class StringTupleComparer : IEqualityComparer<(string loc, string word)>
        {
            public static readonly StringTupleComparer Ordinal = new();
            public bool Equals((string loc, string word) x, (string loc, string word) y)
                => string.Equals(x.loc, y.loc, StringComparison.Ordinal) &&
                   string.Equals(x.word, y.word, StringComparison.Ordinal);
            public int GetHashCode((string loc, string word) obj)
                => HashCode.Combine(obj.loc, obj.word);
        }
    }
}
