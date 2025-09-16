// 📄 Назначение: Первичное заполнение словаря Lexemes
// 📍 
using System;
using System.Linq;
using System.Threading.Tasks;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;

namespace TelegramBlock.Entities
{
    public static class LexemeSeeder
    {
        public static async Task SeedAsync(GameDbContext db)
        {
            if (db.Lexemes.Any()) return; // уже заполнено — выходим

            // Базовые списки: ru/en, длины 1–5 (коротко, узнаваемо, без претензий на академичность)
            var ru = new (string Word, string Pos)[]
            {
                // len=1–2
                ("Я", "other"), ("А", "other"), ("ДА", "other"), ("НЕТ", "other"),
                // len=3
                ("КОТ","noun"), ("ГАЗ","noun"), ("УХО","noun"), ("СПИ","verb"),
                // len=4
                ("СЛОН","noun"), ("КОНЬ","noun"), ("ЖИВИ","verb"), ("БЕГИ","verb"),
                // len=5
                ("СПИРТ","noun"), ("МЕЧТА","noun"), ("ПИШИ","verb"), ("СМОТР","noun") 
            }
            .Where(x => x.Word.Length >= 1 && x.Word.Length <= 5) // на всякий
            .Select(x => new Lexeme { Word = x.Word.ToUpperInvariant(), PartOfSpeech = x.Pos, Locale = "ru" });

            var en = new (string Word, string Pos)[]
            {
                // len=1–2
                ("I","other"), ("A","other"), ("OK","other"), ("GO","verb"),
                // len=3
                ("CAT","noun"), ("DOG","noun"), ("RUN","verb"),
                // len=4
                ("WALK","verb"), ("BIRD","noun"),
                // len=5
                ("DREAM","noun"), ("SMILE","noun"), ("WRITE","verb") // 5–6 — фильтруем ниже
            }
            .Where(x => x.Word.Length >= 1 && x.Word.Length <= 5)
            .Select(x => new Lexeme { Word = x.Word.ToUpperInvariant(), PartOfSpeech = x.Pos, Locale = "en" });

            await db.Lexemes.AddRangeAsync(ru);
            await db.Lexemes.AddRangeAsync(en);
            await db.SaveChangesAsync();
        }
    }
}
