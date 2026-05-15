using System.Windows;

namespace MetGlobal.Services
{
    /// <summary>
    /// Сервис для отображения стандартных системных диалоговых окон.
    /// </summary>
    public static class DialogService
    {
        /// <summary>
        /// Показывает информационное сообщение.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        public static void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Показывает сообщение об ошибке.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Показывает диалоговое окно для подтверждения действия.
        /// </summary>
        /// <param name="message">Текст вопроса.</param>
        /// <returns>True, если пользователь нажал "Да", иначе False.</returns>
        public static bool ShowConfirmation(string message)
        {
            return MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }
    }
}