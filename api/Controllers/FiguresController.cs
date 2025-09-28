// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Контроллер фигур, генерация слоя и логика спауна + утилиты сетки
// 📍 Путь: /src/TelegramBlock/Controllers/FiguresController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;
using TelegramBlock.Logic;

namespace TelegramBlock.Controllers
{
    [ApiController]
    [Route("game/session/{sessionId:guid}/figures")]
    public class FiguresController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly FigureSpawnService _spawn;

        public FiguresController(GameDbContext db)
        {
            _db = db;
            _spawn = new FigureSpawnService(db);
        }

        // Получить все фигуры сессии (для дебага/UI)
        [HttpGet]
        public IActionResult List(Guid sessionId)
        {
            var session = _db.GameSessions
                .Include(s => s.Figures)
                .FirstOrDefault(s => s.Id == sessionId);
            if (session == null) return NotFound("Session not found.");

            return Ok(session.Figures.Select(f => new
            {
                f.Id,
                f.Word,
                f.Length,
                f.HeadCoord,
                f.BlockCoords,
                f.IsFixed,
                f.Locale
            }));
        }

        // Сгенерировать новый слой снизу по правилам (поднимая все фигуры на 1)
        // Возвращает набор добавленных фигур и флаг gameOver, если слой нельзя добавить (переполнение 17-й строки)
        [HttpPost("spawn-layer")]
        public IActionResult SpawnLayer(Guid sessionId)
        {
            SpawnOutcome result;
                        try
            {
                result = _spawn.TrySpawnLayer(sessionId);
                            }
                        catch (KeyNotFoundException)
            {
                                return NotFound("Session not found.");
                            }

            if (result.GameOver)
            {
                return Conflict(new
                {
                    gameOver = true,
                    reason = "Board overflow: adding a new layer would exceed Y=17.",
                    height = 17
                });
            }

            return Ok(new
            {
                gameOver = false,
                added = result.AddedFigures.Select(f => new { f.Id, f.Word, f.Length, f.HeadCoord, f.BlockCoords }),
                totalFigures = result.TotalFigures
            });
        }
    }

    // ------------------------- Сервис спауна слоя --------------------------
    public class FigureSpawnService : ISpawnService
    {
        private readonly GameDbContext _db;
        private readonly Random _rnd = new Random();

        public FigureSpawnService(GameDbContext db)
        {
            _db = db;
        }

        public SpawnOutcome TrySpawnLayer(Guid sessionId)
        {
            var session = _db.GameSessions
                .Include(s => s.Figures)
                .FirstOrDefault(s => s.Id == sessionId);
            if (session == null) throw new KeyNotFoundException("Session not found.");

            // 1) Проверка переполнения: если есть блок на Y=17, новый слой невозможен (если не было сжигания до этого)
            if (session.Figures.SelectMany(f => f.BlockCoords).Any(c => GridMath.Parse(c).y == 17))
            {
                return new SpawnOutcome { GameOver = true };
            }

            // 2) Поднять все существующие фигуры на 1 по Y
            foreach (var fig in session.Figures)
            {
                var blocks = fig.BlockCoords.Select(c => GridMath.Parse(c)).Select(p => (p.x, y: p.y + 1)).ToArray();
                fig.BlockCoords = blocks.Select(GridMath.ToCoord).ToArray();
                var head = GridMath.Parse(fig.HeadCoord);
                fig.HeadCoord = GridMath.ToCoord((head.x, head.y + 1));
            }

            // 3) Сгенерировать набор фигуру(р) на Y=1: минимум 1 фигура, слой не должен быть полностью заполнен, без пересечений
            var occupied = new HashSet<int>(session.Figures
                .SelectMany(f => f.BlockCoords)
                .Where(c => GridMath.Parse(c).y == 1)
                .Select(c => GridMath.Parse(c).x));

            var added = new List<Figure>();

            // Цикл попыток набора: сначала гарантированно добавим 1 фигуру, затем опционально ещё (следуя запрету заполнения 11/11)
            AddOneFigureMandatory(session, occupied, added);
            // Опциональные добавления: с вероятностью, пока остаются места и не получится 11/11
            while (occupied.Count < 10 && _rnd.NextDouble() < 0.5) // 50% шанс добавить ещё фигуру, но оставляем минимум 1 пустую клетку
            {
                if (!TryAddAnother(session, occupied, added)) break;
            }

            _db.SaveChanges();

            // 4) После спауна — фиксация флагов IsFixed для фигур на опоре (Y=1 или есть опора под каждым сегментом)
            foreach (var f in session.Figures)
            {
                f.IsFixed = GridPhysics.HasFullSupport(session.Figures, f);
            }

            _db.SaveChanges();

            return new SpawnOutcome
            {
                GameOver = false,
                AddedFigures = added,
                TotalFigures = session.Figures.Count
            };
        }

        private void AddOneFigureMandatory(GameSession session, HashSet<int> occupied, List<Figure> added)
        {
            // Гарантированно добавляем одну фигуру подходящей длины
            for (int safety = 0; safety < 50; safety++)
            {
                var len = _rnd.Next(1, 6); // 1..5
                if (TryPlaceFigureInRow(session, y: 1, len, occupied, out var figure))
                {
                    _db.Figures.Add(figure);
                    session.Figures.Add(figure);
                    added.Add(figure);
                    MarkOccupied(figure, occupied);
                    return;
                }
            }
            // Если почему-то не удалось — бросаем исключение для отладки
            throw new InvalidOperationException("Failed to place a mandatory figure on the new layer.");
        }

        private bool TryAddAnother(GameSession session, HashSet<int> occupied, List<Figure> added)
        {
            // Не допускаем заполнение 11/11
            if (occupied.Count >= 10) return false; // оставим хотя бы 1 пустую клетку

            for (int safety = 0; safety < 50; safety++)
            {
                var len = _rnd.Next(1, 6);
                if (TryPlaceFigureInRow(session, y: 1, len, occupied, out var figure))
                {
                    _db.Figures.Add(figure);
                    session.Figures.Add(figure);
                    added.Add(figure);
                    MarkOccupied(figure, occupied);
                    return true;
                }
            }
            return false;
        }

        private static void MarkOccupied(Figure f, HashSet<int> occ)
        {
            foreach (var c in f.BlockCoords)
            {
                var p = GridMath.Parse(c);
                if (p.y == 1) occ.Add(p.x);
            }
        }

        private bool TryPlaceFigureInRow(GameSession session, int y, int len, HashSet<int> occupiedX, out Figure figure)
        {
            var freeSpans = GridMath.GetFreeSpansOnRow(session.Figures, y, occupiedX);
            // Выбираем случайный доступный промежуток, куда влезет длина len
            var candidates = freeSpans.Where(s => s.length >= len).ToList();
            if (!candidates.Any())
            {
                figure = null!;
                return false;
            }

            var span = candidates[_rnd.Next(candidates.Count)];
            var maxStart = span.start + span.length - len; // включительно
            var x0 = _rnd.Next(span.start, maxStart + 1);
            var blocks = Enumerable.Range(0, len).Select(i => (x: x0 + i, y)).ToArray();

            // Выбор слова подходящей длины из словаря текущей локали (может отсутствовать → используем заглушку "X" * len)
            var locale = session.Locale;
            var lexeme = _db.Lexemes
                .Where(l => l.Locale == locale && l.Word.Length == len)
                .OrderBy(l => Guid.NewGuid())
                .FirstOrDefault();

            var word = lexeme?.Word ?? new string('X', len);

            var head = GridMath.ToCoord(blocks[0]);
            var figureBlocks = blocks.Select(GridMath.ToCoord).ToArray();

            figure = new Figure
            {
                Id = Guid.NewGuid(),
                GameSessionId = session.Id,
                Length = len,
                Word = word,
                HeadCoord = head,
                BlockCoords = figureBlocks,
                IsFixed = y == 1, // у нижнего слоя есть опора
                Locale = locale,
                LexemeId = lexeme?.Id
            };

            return true;
        }
    }

    public class SpawnOutcome
    {
        public bool GameOver { get; set; }
        public List<Figure> AddedFigures { get; set; } = new();
        public int TotalFigures { get; set; }
    }

    // --------------------------- Физика и утилиты ---------------------------
    public static class GridMath
    {
        // X: 1..11 (A..K), Y: 1..17
        public static (int x, int y) Parse(string coord)
        {
            if (string.IsNullOrWhiteSpace(coord)) throw new ArgumentException("coord is empty");
            char col = coord[0];
            int x = (col - 'A') + 1;
            int y = int.Parse(coord.Substring(1));
            return (x, y);
        }

        public static string ToCoord((int x, int y) p)
        {
            return ((char)('A' + (p.x - 1))).ToString() + p.y.ToString();
        }

        public static IEnumerable<(int start, int length)> GetFreeSpansOnRow(IEnumerable<Figure> figures, int y, HashSet<int>? presetOccupied = null)
        {
            var occupied = new HashSet<int>(presetOccupied ?? Enumerable.Empty<int>());
            foreach (var c in figures.SelectMany(f => f.BlockCoords))
            {
                var p = Parse(c);
                if (p.y == y) occupied.Add(p.x);
            }

            var free = new List<(int start, int length)>();
            int x = 1;
            while (x <= 11)
            {
                while (x <= 11 && occupied.Contains(x)) x++;
                int start = x;
                while (x <= 11 && !occupied.Contains(x)) x++;
                int length = x - start;
                if (length > 0) free.Add((start, length));
            }
            return free;
        }
    }

    public static class GridPhysics
    {
        // Опора под каждым сегментом: либо Y==1, либо имеется блок другой фигуры (или той же?) на y-1 с тем же x
        public static bool HasFullSupport(IEnumerable<Figure> all, Figure f)
        {
            foreach (var c in f.BlockCoords)
            {
                var p = GridMath.Parse(c);
                if (p.y == 1) continue; // нижняя граница — опора
                bool supported = all
                    .Where(o => o.Id != f.Id)
                    .SelectMany(o => o.BlockCoords)
                    .Any(b =>
                    {
                        var q = GridMath.Parse(b);
                        return q.x == p.x && q.y == p.y - 1;
                    });
                if (!supported) return false;
            }
            return true;
        }
    }
}
