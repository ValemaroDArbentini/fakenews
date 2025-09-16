// 📄 Назначение: Логика перемещения/превью и оркестрации (добавлен ComputeFreeSteps + единый коммит хода)
// 📍 Путь: /src/TelegramBlock.Logic/GameLogicServices.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TelegramBlock.Controllers;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;

namespace TelegramBlock.Logic
{
    // --------------------------- Контракты ---------------------------
    public interface IMoveService
    {
        MoveResult Move(Guid sessionId, Guid figureId, MoveDirection direction);
        (int allowedSteps, string[] path) ComputeFreeSteps(Guid sessionId, Guid figureId, MoveDirection direction, int requestedSteps);
    }

    public interface IBurnAndDropService
    {
        ResolveResult ResolveAll(Guid sessionId);
    }

    public interface ISpawnService
    {
        SpawnOutcome TrySpawnLayer(Guid sessionId);
    }

    public interface ITurnOrchestrator
    {
        TurnResult ApplyAfterPlayerAction(Guid sessionId, OrchestratorPolicy policy);
    }

    public enum MoveDirection { Left = -1, Right = 1 }

    public record MoveResult(bool Success, string? Error, string[]? NewCoords);
    public record ResolveResult(int rowsBurned, int cascades, int scoreGained);
    public record TurnResult(bool gameOver, int rowsBurned, int cascades, int scoreGained, int totalFigures);

    public record OrchestratorPolicy(bool spawnIfNoBurn, int minFiguresThreshold);

    // ------------------------ Реализация Move ------------------------
    public class MoveService : IMoveService
    {
        private readonly GameDbContext _db;
        private readonly ILogger<MoveService> _logger;
        public MoveService(GameDbContext db, ILogger<MoveService> logger) { _db = db; _logger = logger; }

        public MoveResult Move(Guid sessionId, Guid figureId, MoveDirection direction)
        {
            var session = _db.GameSessions.Include(s => s.Figures).FirstOrDefault(s => s.Id == sessionId);
            if (session == null) return new(false, "Session not found.", null);
            var fig = session.Figures.FirstOrDefault(f => f.Id == figureId);
            if (fig == null) return new(false, "Figure not found.", null);

            int shift = (int)direction;

            var moved = fig.BlockCoords.Select(c => Grid.To((Grid.From(c).x + shift, Grid.From(c).y))).ToArray();
            if (moved.Any(p => Grid.From(p).x < 1 || Grid.From(p).x > 11))
                return new(false, "Out of bounds.", null);

            var occupied = session.Figures.Where(f => f.Id != fig.Id).SelectMany(f => f.BlockCoords).ToHashSet();
            if (moved.Any(occupied.Contains))
                return new(false, "Target occupied.", null);

            fig.BlockCoords = moved;
            fig.HeadCoord = moved.First();
            _db.SaveChanges();
            _logger.LogInformation("Move success: figure {FigureId} moved {Dir} in session {SessionId} → {Coords}", fig.Id, direction, sessionId, moved);
            return new(true, null, moved);
        }

        // Чистый расчёт: сколько шагов можно сделать и какие координаты пройдут по пути.
        public (int allowedSteps, string[] path) ComputeFreeSteps(Guid sessionId, Guid figureId, MoveDirection direction, int requestedSteps)
        {
            var session = _db.GameSessions.Include(s => s.Figures).First(x => x.Id == sessionId);
            var fig = session.Figures.First(x => x.Id == figureId);

            int stepSign = (int)direction;
            int maxReq = Math.Clamp(requestedSteps, 1, 11);

            // Готовим множество занятых клеток другими фигурами
            var others = session.Figures.Where(f => f.Id != fig.Id).SelectMany(f => f.BlockCoords).ToHashSet();

            // Текущее положение фигуры (локальная копия)
            var cur = fig.BlockCoords.Select(Grid.From).ToArray();
            var path = new List<string>();
            int performed = 0;

            for (int s = 0; s < maxReq; s++)
            {
                var candidate = cur.Select(p => (x: p.x + stepSign, y: p.y)).ToArray();
                // Стены
                if (candidate.Any(p => p.x < 1 || p.x > 11)) break;
                // Коллизии
                var candidateCoords = candidate.Select(Grid.To).ToArray();
                if (candidateCoords.Any(others.Contains)) break;

                // Шаг возможен — фиксируем и пишем head в path
                cur = candidate;
                performed++;
                path.Add(candidateCoords[0]); // путь головного блока (для шлейфа)
            }

            return (performed, path.ToArray());
        }
    }

    // -------------------- Реализация Burn & Drop --------------------
    public class BurnAndDropService : IBurnAndDropService
    {
        private readonly GameDbContext _db;
        private readonly ILogger<BurnAndDropService> _logger;
        public BurnAndDropService(GameDbContext db, ILogger<BurnAndDropService> logger) { _db = db; _logger = logger; }

        private void RegisterBurn(GameSession s, int rowY, int cascade, int award)
        {
            s.Score += award;
            _db.BurnEvents.Add(new BurnEvent
            {
                Id = Guid.NewGuid(),
                GameSessionId = s.Id,
                OccurredAt = DateTime.UtcNow,
                RowY = rowY,
                CascadeLevel = cascade,
                ScoreAwarded = award
            });
        }

        private static bool DropUntilSupported(GameSession session)
        {
            bool movedAny = false;
            while (true)
            {
                var occ = new HashSet<string>(session.Figures.SelectMany(f => f.BlockCoords));
                var fallers = new List<Figure>();
                foreach (var f in session.Figures)
                {
                    bool can = true;
                    foreach (var c in f.BlockCoords)
                    {
                        var p = Grid.From(c);
                        if (p.y <= 1) { can = false; break; }
                        var below = Grid.To((p.x, p.y - 1));
                        if (occ.Contains(below)) { can = false; break; }
                    }
                    if (can) fallers.Add(f);
                }
                if (fallers.Count == 0) break;
                foreach (var f in fallers)
                    for (int i = 0; i < f.BlockCoords.Length; i++)
                    {
                        var p = Grid.From(f.BlockCoords[i]);
                        f.BlockCoords[i] = Grid.To((p.x, p.y - 1));
                    }
                movedAny = true;
            }
            foreach (var f in session.Figures)
            {
                f.HeadCoord = f.BlockCoords.First();
                f.IsFixed = GridPhysics.HasFullSupport(session.Figures, f);
            }
            return movedAny;
        }

        public ResolveResult ResolveAll(Guid sessionId)
        {
            var session = _db.GameSessions.Include(s => s.Figures).First(x => x.Id == sessionId);
            int totalRows = 0, cascade = 0, gained = 0;
            while (true)
            {
                bool fell = DropUntilSupported(session);
                _db.SaveChanges();

                var full = Enumerable.Range(1, 17).Where(y => Grid.IsRowFull(session.Figures, y)).ToList();
                if (full.Count == 0)
                {
                    if (!fell) break; else continue;
                }
                cascade++;
                totalRows += full.Count;
                int award = ScoreMath.ScoreFor(full.Count, cascade);
                gained += award;
                foreach (var y in full) RegisterBurn(session, y, cascade, award / full.Count);

                foreach (var f in session.Figures)
                {
                    var blocks = f.BlockCoords.Select(Grid.From).ToList();
                    blocks.RemoveAll(b => full.Contains(b.y));
                    int dropBy(int y) => full.Count(r => r < y);
                    f.BlockCoords = blocks.Select(b => Grid.To((b.x, b.y - dropBy(b.y)))).ToArray();
                    f.HeadCoord = f.BlockCoords.FirstOrDefault() ?? f.HeadCoord;
                }
                session.Figures = session.Figures.Where(f => f.BlockCoords.Length > 0).ToList();
                _db.SaveChanges();
            }
            return new ResolveResult(totalRows, cascade, gained);
        }
    }

    // ------------------------- Оркестратор хода -------------------------
    public class TurnOrchestrator : ITurnOrchestrator
    {
        private readonly IBurnAndDropService _resolver;
        private readonly ISpawnService _spawner;
        private readonly GameDbContext _db;
        private readonly ILogger<TurnOrchestrator> _logger;
        public TurnOrchestrator(IBurnAndDropService resolver, ISpawnService spawner, GameDbContext db, ILogger<TurnOrchestrator> logger)
        { _resolver = resolver; _spawner = spawner; _db = db; _logger = logger; }

        public TurnResult ApplyAfterPlayerAction(Guid sessionId, OrchestratorPolicy policy)
        {
            var resolve = _resolver.ResolveAll(sessionId);
            _logger.LogInformation("Turn resolve: rows={Rows} cascades={Cascades} gained={Gained} (session {SessionId})", resolve.rowsBurned, resolve.cascades, resolve.scoreGained, sessionId);

            var session = _db.GameSessions.Include(s => s.Figures).First(x => x.Id == sessionId);

            bool spawned = false;
            if (policy.spawnIfNoBurn && resolve.rowsBurned == 0)
            {
                var spawn = _spawner.TrySpawnLayer(sessionId);
                _logger.LogInformation("Spawn after no-burn: gameOver={GameOver} added={Added} (session {SessionId})", spawn.GameOver, spawn.AddedFigures?.Count, sessionId);
                if (spawn.GameOver) return new(true, resolve.rowsBurned, resolve.cascades, resolve.scoreGained, session.Figures.Count);
                spawned = true;
            }
            if (session.Figures.Count < policy.minFiguresThreshold)
            {
                var spawn2 = _spawner.TrySpawnLayer(sessionId);
                _logger.LogInformation("Spawn to satisfy min figures: gameOver={GameOver} total={Total} (session {SessionId})", spawn2.GameOver, session.Figures.Count, sessionId);
                if (spawn2.GameOver) return new(true, resolve.rowsBurned, resolve.cascades, resolve.scoreGained, session.Figures.Count);
                spawned = true;
            }
            if (spawned)
            {
                var settle = _resolver.ResolveAll(sessionId);
                _logger.LogInformation("Post-spawn settle: rows={Rows} cascades={Cascades} gained={Gained} (session {SessionId})", settle.rowsBurned, settle.cascades, settle.scoreGained, sessionId);
                resolve = new ResolveResult(resolve.rowsBurned + settle.rowsBurned, resolve.cascades + settle.cascades, resolve.scoreGained + settle.scoreGained);
            }

            foreach (var f in session.Figures) f.IsFixed = GridPhysics.HasFullSupport(session.Figures, f);
            _db.SaveChanges();
            _logger.LogInformation("Turn end (session {SessionId}): figures={Figures}", sessionId, session.Figures.Count);
            return new(false, resolve.rowsBurned, resolve.cascades, resolve.scoreGained, session.Figures.Count);
        }
    }

    // --------------------------- Утилиты/Математика ---------------------------
    public static class Grid
    {
        public static (int x, int y) From(string coord)
        { char col = coord[0]; int x = (col - 'A') + 1; int y = int.Parse(coord[1..]); return (x, y); }
        public static string To((int x, int y) p) => ((char)('A' + (p.x - 1))).ToString() + p.y.ToString();

        public static class SessionLocks
        {
            private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
            public static async Task<IDisposable> Acquire(Guid sessionId)
            { var sem = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1)); await sem.WaitAsync(); return new Releaser(sessionId, sem); }
            private sealed class Releaser : IDisposable
            { private readonly SemaphoreSlim _sem; public Releaser(Guid id, SemaphoreSlim sem) { _sem = sem; } public void Dispose() { _sem.Release(); } }
        }

        public static bool IsRowFull(IEnumerable<Figure> figs, int y)
        {
            var occ = new HashSet<int>();
            foreach (var c in figs.SelectMany(f => f.BlockCoords)) { var p = From(c); if (p.y == y) occ.Add(p.x); }
            return occ.Count == 11;
        }
    }

    public static class ScoreMath
    {
        public static int ScoreFor(int rowsInBatch, int cascadeLevel)
        {
            int baseRows = rowsInBatch * 100 + Math.Max(0, rowsInBatch - 1) * 20;
            int combo = (cascadeLevel > 1) ? 50 * (cascadeLevel - 1) : 0;
            return baseRows + combo;
        }
    }

    public static class GridPhysics
    {
        public static bool HasFullSupport(IEnumerable<Figure> all, Figure f)
        {
            foreach (var c in f.BlockCoords)
            {
                var p = Grid.From(c);
                if (p.y == 1) continue;
                bool supported = all.Where(o => o.Id != f.Id).SelectMany(o => o.BlockCoords)
                    .Any(b => { var q = Grid.From(b); return q.x == p.x && q.y == p.y - 1; });
                if (!supported) return false;
            }
            return true;
        }
    }
}
