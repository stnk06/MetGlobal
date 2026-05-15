using System.Linq;
using System.Text.RegularExpressions;

namespace MetGlobal.Services
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Проверяет, что строка содержит только буквы, пробелы или дефисы.
        /// </summary>
        public static bool IsTextOnly(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return true; // Пустое значение допустимо, проверка на обязательность - отдельно
            return input.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '-');
        }

        /// <summary>
        /// Проверяет, является ли строка корректным ИНН (10 или 12 цифр).
        /// </summary>
        public static bool IsValidInn(string inn)
        {
            if (string.IsNullOrWhiteSpace(inn)) return true;
            return Regex.IsMatch(inn, @"^(\d{10}|\d{12})$");
        }

        /// <summary>
        /// Проверяет, является ли строка корректным номером телефона.
        /// </summary>
        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false; // Телефон обязателен
            // Удаляем все нецифровые символы для проверки
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
            // Проверяем, что номер начинается с 7 или 8 и содержит 11 цифр
            return digitsOnly.Length == 11 && (digitsOnly.StartsWith("7") || digitsOnly.StartsWith("8"));
        }

        /// <summary>
        /// Проверяет, является ли строка корректным Email.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}