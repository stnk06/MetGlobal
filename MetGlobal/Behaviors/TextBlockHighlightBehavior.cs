using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MetGlobal.Behaviors
{
    /// <summary>
    /// Вспомогательный класс-поведение для подсветки части текста в TextBlock.
    /// </summary>
    public static class TextBlockHighlightBehavior
    {
        // Создаем присоединенное свойство, которое будет принимать текст для поиска
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.RegisterAttached(
                "SearchText",
                typeof(string),
                typeof(TextBlockHighlightBehavior),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public static string GetSearchText(DependencyObject obj)
        {
            return (string)obj.GetValue(SearchTextProperty);
        }

        public static void SetSearchText(DependencyObject obj, string value)
        {
            obj.SetValue(SearchTextProperty, value);
        }

        /// <summary>
        /// Этот метод вызывается каждый раз, когда меняется текст в строке поиска.
        /// </summary>
        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as TextBlock;
            if (textBlock == null) return;

            string searchText = (string)e.NewValue;
            string fullText = textBlock.Text;

            // Очищаем предыдущее форматирование
            textBlock.Inlines.Clear();

            // Если строка поиска пуста, просто показываем исходный текст
            if (string.IsNullOrWhiteSpace(searchText))
            {
                textBlock.Inlines.Add(new Run(fullText));
                return;
            }

            // Главная логика: разбиваем текст на части и подсвечиваем нужные
            int lastIndex = 0;
            int matchIndex;
            // ИЗМЕНЕНИЕ: Добавлен параметр StringComparison.OrdinalIgnoreCase для поиска без учета регистра
            while ((matchIndex = fullText.IndexOf(searchText, lastIndex, System.StringComparison.OrdinalIgnoreCase)) != -1)
            {
                // Добавляем текст до найденного совпадения
                if (matchIndex > lastIndex)
                {
                    textBlock.Inlines.Add(new Run(fullText.Substring(lastIndex, matchIndex - lastIndex)));
                }

                // Добавляем подсвеченный фрагмент
                var highlightedRun = new Run(fullText.Substring(matchIndex, searchText.Length))
                {
                    Background = new SolidColorBrush(Colors.Gold),
                    Foreground = new SolidColorBrush(Colors.Black)
                };
                textBlock.Inlines.Add(highlightedRun);

                lastIndex = matchIndex + searchText.Length;
            }

            // Добавляем оставшийся текст после последнего совпадения
            if (lastIndex < fullText.Length)
            {
                textBlock.Inlines.Add(new Run(fullText.Substring(lastIndex)));
            }
        }
    }
}
