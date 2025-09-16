// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Сущности доменной модели
// 📍 Путь: /src/TelegramBlock.Entities/GameEntities.cs

using System;
using System.Collections.Generic;
using static NpgsqlTypes.NpgsqlTsVector;

namespace TelegramBlock.Entities
{
    public class GameSession
    {
        public Guid Id { get; set; }
        public long TelegramUserId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int Score { get; set; }
        public bool IsWin { get; set; }
        public bool IsBusy { get; set; } = false;

        public string Locale { get; set; } = "ru"; // локаль Telegram-пользователя

        public ICollection<Figure> Figures { get; set; } = new List<Figure>();
        public ICollection<BroadcastedLine> BroadcastedLines { get; set; } = new List<BroadcastedLine>();
    }

    public class Figure
    {
        public Guid Id { get; set; }
        public Guid GameSessionId { get; set; }
        public int Length { get; set; } // от 1 до 5
        public string Word { get; set; } = null!; // слово-фигура: "КОТ", "СПИ"
        public string HeadCoord { get; set; } = null!; // например, "C7"
        public string[] BlockCoords { get; set; } = null!; // например, ["C7","D7","E7"]

        public bool IsFixed { get; set; } // зафиксирована (упала на опору)
        public string Locale { get; set; } = "ru"; // язык, на котором сгенерировано слово

        public int? LexemeId { get; set; } // опциональная ссылка на словарь
        public Lexeme? Lexeme { get; set; } // навигационное свойство
    }

    public class MoveAction
    {
        public Guid Id { get; set; }
        public Guid GameSessionId { get; set; }
        public DateTime PerformedAt { get; set; }

        public Guid FigureId { get; set; }
        public string FromCoord { get; set; } = null!;
        public string ToCoord { get; set; } = null!;
    }

    public class BurnEvent
    {
        public Guid Id { get; set; }
        public Guid GameSessionId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int RowY { get; set; }
        public int CascadeLevel { get; set; }
        public int ScoreAwarded { get; set; }
    }

    public class BroadcastedLine
    {
        public Guid Id { get; set; }
        public Guid GameSessionId { get; set; }
        public DateTime SentAt { get; set; }
        public string Phrase { get; set; } = null!; // например: "СПИРТ ГАЗ УХО"
        public int SourceRow { get; set; }
        public bool WasWinMoment { get; set; }
        public string Reaction { get; set; } = null!; // "они там совсем поехали?"
        public string Locale { get; set; } = "ru"; // язык фразы
        public string Direction { get; set; } = "ltr"; // ltr или rtl
        public bool IsSavedByUser { get; set; } = false; // сохранён пользователем
        public bool IsViewed { get; set; } = false; // просмотрен (но не обязательно сохранён)
        public string? Tags { get; set; } // техническое поле, можно использовать для категорий, сессий, отладочных пометок set; } = "ltr"; // ltr или rtl
    }
}

