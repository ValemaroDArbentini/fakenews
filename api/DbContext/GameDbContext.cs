// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Контекст базы данных EF Core
// 📍 Путь: C:/DL_Projects/TelegramBlock/DbContext/GameDbContext.cs

using Microsoft.EntityFrameworkCore;
using TelegramBlock.Entities;

namespace TelegramBlock.DbContext
{
    public class GameDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<GameSession> GameSessions => Set<GameSession>();
        public DbSet<Figure> Figures => Set<Figure>();
        public DbSet<MoveAction> MoveActions => Set<MoveAction>();
        public DbSet<BurnEvent> BurnEvents => Set<BurnEvent>();
        public DbSet<BroadcastedLine> BroadcastedLines => Set<BroadcastedLine>();
        public DbSet<Lexeme> Lexemes => Set<Lexeme>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Конфигурация Lexeme.Word как уникального в пределах Locale
            modelBuilder.Entity<Lexeme>()
                .HasIndex(l => new { l.Word, l.Locale })
                .IsUnique();

            // Figure.Word не индексируем, т.к. он может быть "грязным"
            modelBuilder.Entity<Figure>()
                .HasOne(f => f.Lexeme)
                .WithMany(l => l.Figures)
                .HasForeignKey(f => f.LexemeId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}
