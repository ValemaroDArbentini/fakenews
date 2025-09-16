// Назначение: Эндпоинты игрового процесса. ЕДИНЫЙ префикс /game (без дубля /api/game)
// Путь: C:\\DL_Projects\\FakeNews\\api\\Controllers\\GameController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;
using TelegramBlock.Logic;

namespace TelegramBlock.Controllers
{
    [ApiController]
    [Route("game")] // единый префикс
    public class GameController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<GameController> _logger;
        private readonly IMoveService _moveService;
        private readonly ITurnOrchestrator _orchestrator;

        public GameController(GameDbContext db, ILogger<GameController> logger, IMoveService moveService, ITurnOrchestrator orchestrator)
        {
            _db = db; _logger = logger; _moveService = moveService; _orchestrator = orchestrator;
        }

        // ------------------------------ SESSION ------------------------------
        [HttpPost("session")]
        [Produces("application/json")]
        public IActionResult CreateSession()
        {
            var s = new GameSession
            {
                Id = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow,
                Score = 0,
                IsWin = false,
                Locale = "ru",
                IsBusy = false
            };
            _db.GameSessions.Add(s);
            _db.SaveChanges();
            return Ok(new { sessionId = s.Id, locale = s.Locale, startTime = s.StartedAt });
        }

        [HttpGet("session/{id:guid}")]
        [Produces("application/json")]
        public IActionResult GetState([FromRoute] Guid id)
        {
            var s = _db.GameSessions
                .Include(x => x.Figures)
                .Include(x => x.BroadcastedLines)
                .FirstOrDefault(x => x.Id == id);
            if (s == null) return NotFound();

            return Ok(new
            {
                s.Id,
                s.StartedAt,
                s.EndedAt,
                s.Score,
                s.IsWin,
                s.Locale,
                s.IsBusy,
                figures = s.Figures.Select(f => new { f.Id, f.Word, f.HeadCoord, f.BlockCoords, f.IsFixed, f.Locale, length = f.Word?.Length ?? 0 }),
                broadcast = s.BroadcastedLines
            });
        }
        // ------------------------------ MOVE -------------------------------
        public sealed class MoveDto
        {
            public Guid FigureId { get; set; }
            public string Direction { get; set; } = string.Empty; // "left"|"right"
            public int Steps { get; set; } = 1;                   // >=1
            public bool Preview { get; set; } = false;            // true=только расчёт
        }

        [HttpPost("session/{id:guid}/move")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult Move([FromRoute] Guid id, [FromBody] MoveDto dto)
        {
            if (!Request.HasJsonContentType())
                return BadRequest(new { error = "Content-Type must be application/json" });

            if (dto.FigureId == Guid.Empty)
                return BadRequest(new { error = "figureId is required" });

            if (!Enum.TryParse<MoveDirection>(dto.Direction, true, out var dir))
                return BadRequest(new { error = "direction must be 'left' or 'right'" });

            var session = _db.GameSessions.FirstOrDefault(x => x.Id == id);
            if (session == null) return NotFound();
            if (session.EndedAt != null)
                return Conflict(new { error = "Session ended" });

            if (dto.Preview)
            {
                var result = _moveService.ComputeFreeSteps(id, dto.FigureId, dir, dto.Steps);
                var head = _db.Figures.Where(f => f.GameSessionId == id && f.Id == dto.FigureId).Select(f => f.HeadCoord).FirstOrDefault();
                var path = new List<string>(result.path);
                if (!string.IsNullOrEmpty(head) && path.Count == 0)
                {
                    var p = GridMath.Parse(head);
                    for (int i = 1; i <= result.allowedSteps; i++)
                        path.Add(GridMath.ToCoord((p.x + (dir == MoveDirection.Left ? -i : i), p.y)));
                }
                return Ok(new { allowedSteps = result.allowedSteps, path, wouldBlock = result.allowedSteps < dto.Steps });
            }

            int performed = 0;
            for (int i = 0; i < Math.Max(1, dto.Steps); i++)
            {
                var res = _moveService.Move(id, dto.FigureId, dir);
                if (!res.Success)
                {
                    if (string.Equals(res.Error, "Figure not found.", StringComparison.OrdinalIgnoreCase))
                        return NotFound(new { error = "figure_not_found" });
                    if (string.Equals(res.Error, "Out of bounds.", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(res.Error, "Target occupied.", StringComparison.OrdinalIgnoreCase))
                        break; // прекращаем оставшиеся шаги
                    return BadRequest(new { error = res.Error });
                }
                performed++;
            }

            var summary = _orchestrator.ApplyAfterPlayerAction(id, new OrchestratorPolicy(spawnIfNoBurn: true, minFiguresThreshold: 4));
            return Ok(new
            {
                success = true,
                performedSteps = performed,
                summary.rowsBurned,
                summary.cascades,
                summary.scoreGained,
                totalFigures = _db.Figures.Count(f => f.GameSessionId == id),
                gameOver = _db.GameSessions.Where(s => s.Id == id).Select(s => s.EndedAt != null).FirstOrDefault()
            });
        }

        // ------------------------------ END -------------------------------
        [HttpPost("session/{id:guid}/end")]
        public IActionResult End([FromRoute] Guid id, [FromQuery] bool win = false)
        {
            var session = _db.GameSessions.FirstOrDefault(x => x.Id == id);
            if (session == null) return NotFound();
            if (session.EndedAt != null) return Conflict(new { error = "already_ended" });

            session.EndedAt = DateTime.UtcNow;
            session.IsWin = win;
            _db.SaveChanges();
            return Ok(new { ok = true });
        }
    }

    internal static class HttpRequestJsonExtensions
    {
        public static bool HasJsonContentType(this HttpRequest req)
            => !string.IsNullOrEmpty(req.ContentType) && req.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}