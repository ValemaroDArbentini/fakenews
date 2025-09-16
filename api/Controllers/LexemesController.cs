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

        public LexemesController(GameDbContext db, ILogger<LexemesController> log, IConfiguration cfg)
        { _db = db; _log = log; _adminToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN"); }

        private bool IsAdmin() =>
            !string.IsNullOrEmpty(_adminToken) &&
            Request.Headers.TryGetValue("X-Admin-Token", out var h) &&
            string.Equals(h.ToString(), _adminToken, StringComparison.Ordinal);

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] int? len, [FromQuery] string? q,
                                             [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _db.Lexemes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(locale)) query = query.Where(x => x.Locale == locale);
            if (len is >= 1 and <= 5) query = query.Where(x => x.Word.Length == len);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = q.Trim().ToUpper().Replace('*', '%');
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

        public record UpsertDto(string Word, string PartOfSpeech, string Locale);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] UpsertDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var word = (dto.Word ?? "").Trim().ToUpperInvariant();
            if (word.Length is < 1 or > 5) return BadRequest(new { error = "length_must_be_1_5" });
            if (await _db.Lexemes.AnyAsync(x => x.Locale == dto.Locale && x.Word == word))
                return Conflict(new { error = "duplicate" });

            var lex = new Lexeme { Word = word, PartOfSpeech = dto.PartOfSpeech, Locale = dto.Locale };
            _db.Lexemes.Add(lex);
            await _db.SaveChangesAsync();
            return Ok(new { id = lex.Id });
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> Bulk([FromQuery] string locale = "ru", [FromQuery] string pos = "noun")
        {
            if (!IsAdmin()) return Unauthorized();
            using var reader = new StreamReader(Request.Body);
            var text = await reader.ReadToEndAsync();

            var added = 0; var skipped = 0;
            void TryAdd(string word, string? part, string? loc)
            {
                var w = (word ?? "").Trim().ToUpperInvariant();
                var p = string.IsNullOrWhiteSpace(part) ? pos : part!;
                var l = string.IsNullOrWhiteSpace(loc) ? locale : loc!;
                if (w.Length is < 1 or > 5) { skipped++; return; }
                if (_db.Lexemes.Any(x => x.Locale == l && x.Word == w)) { skipped++; return; }
                _db.Lexemes.Add(new Lexeme { Word = w, PartOfSpeech = p, Locale = l });
                added++;
            }

            if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(line);
                        TryAdd(obj?["word"] ?? "", obj!.GetValueOrDefault("partOfSpeech"), obj!.GetValueOrDefault("locale"));
                    }
                    catch { skipped++; }
                }
            }
            else if (Request.ContentType?.Contains("csv", StringComparison.OrdinalIgnoreCase) == true)
            {
                foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(0))
                {
                    var cells = line.Split(',', StringSplitOptions.TrimEntries);
                    if (cells.Length == 0) { skipped++; continue; }
                    TryAdd(cells.ElementAtOrDefault(0) ?? "", cells.ElementAtOrDefault(1), cells.ElementAtOrDefault(2));
                }
            }
            else // text/plain — по строке слово
            {
                foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    TryAdd(line, null, null);
            }

            await _db.SaveChangesAsync();
            return Ok(new { added, skipped });
        }
    }
}
