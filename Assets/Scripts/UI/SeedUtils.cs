using System;

namespace Diploma.UI
{
    /// <summary>
    /// Утилиты для работы с seed.
    /// </summary>
    public static class SeedUtils
    {
        /// <summary>
        /// Преобразует строку в числовой seed.
        /// Если строка пустая — возвращает 12345.
        /// </summary>
        public static int StringToSeed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 12345;

            // Пробуем распарсить как число
            if (int.TryParse(text, out int numericSeed))
                return numericSeed;

            // Если не число — хешируем строку
            return text.GetHashCode();
        }

        /// <summary>
        /// Преобразует числовой seed в строку.
        /// </summary>
        public static string SeedToString(int seed)
        {
            return seed.ToString();
        }

        /// <summary>
        /// Генерирует случайный seed.
        /// </summary>
        public static int GenerateRandomSeed()
        {
            return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
    }
}
