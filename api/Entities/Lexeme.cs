// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Словарь допустимых слов
// 📍 Путь: /src/TelegramBlock.Entities/Lexeme.cs

using System;
using System.Collections.Generic;

namespace TelegramBlock.Entities
{
    public class Lexeme
    {
        public int Id { get; set; }
        public string Word { get; set; } = null!; // само слово: "КОТ"
        public string PartOfSpeech { get; set; } = null!; // "noun" или "verb"
        public string Locale { get; set; } = "ru"; // язык: "ru", "en", "ar" и т.п.

        public int Length => Word.Length; // вычисляемое поле

        public ICollection<Figure> Figures { get; set; } = new List<Figure>();
    }
}
