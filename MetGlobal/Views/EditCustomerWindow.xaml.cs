using MetGlobal.Models;
using MetGlobal.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MetGlobal.Views
{
    public partial class EditCustomerWindow : Window
    {
        public List<string> CompanyTypes { get; } = new List<string>
        {
            "", "ООО", "АО", "ПАО", "ИП", "ЗАО", "ОАО", "ГК", "ФГУП", "МУП", "НКО", "Фонд"
        };

        public List<string> PhoneMasks { get; } = new List<string>
        {
            "+7 (XXX) XXX-XX-XX",
            "+7 (XXXX) XX-XX-XX"
        };

        private bool _isFormatting = false;

        public EditCustomerWindow(Customer customer)
        {
            InitializeComponent();

            // Инициализация названия компании
            string fullCompName = customer.CompanyName ?? "";
            string foundType = CompanyTypes.FirstOrDefault(t => !string.IsNullOrEmpty(t) && fullCompName.StartsWith(t + " "));

            if (foundType != null)
            {
                CompTypeComboBox.SelectedItem = foundType;
                CompNameTextBox.Text = fullCompName.Substring(foundType.Length).Trim();
            }
            else
            {
                CompTypeComboBox.SelectedItem = "";
                CompNameTextBox.Text = fullCompName;
            }

            // Инициализация маски телефона
            PhoneMaskComboBox.SelectedIndex = 0;

            DataContext = customer;

            CompTypeComboBox.ItemsSource = CompanyTypes;
            PhoneMaskComboBox.ItemsSource = PhoneMasks;
        }

        // -----------------------------------------------------------
        // 1. БЛОКИРОВКА ВВОДА (GATEKEEPERS)
        // -----------------------------------------------------------

        // Для Названия компании: Блокируем латиницу
        private void BlockLatinInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[a-zA-Z]");
        }

        // Для Телефона: Блокируем ВСЁ, кроме цифр
        private void BlockNonDigits(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры (0-9)
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // Для Контактного лица: Строгая валидация + Ограничение длины
        private void ContactName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // А. Разрешаем только: Кириллицу, Пробел, Точку
            if (!Regex.IsMatch(e.Text, @"^[а-яА-ЯёЁ\s\.]$"))
            {
                e.Handled = true;
                return;
            }

            // Б. Проверка на завершенность формата (Фамилия И.О.)
            string currentText = textBox.Text;

            // Паттерн: [Слово][пробел][Буква].[Буква].
            // Пример: "Иванов И.И."
            string fullPattern = @"^[А-ЯЁ][а-яё]+\s[А-ЯЁ]\.[А-ЯЁ]\.$";

            // Если текст уже соответствует полному формату И мы пытаемся писать в конце строки
            if (Regex.IsMatch(currentText, fullPattern) && textBox.CaretIndex == currentText.Length)
            {
                e.Handled = true; // Блокируем ввод
            }
        }

        // -----------------------------------------------------------
        // 2. АВТО-ФОРМАТИРОВАНИЕ (BEAUTIFIERS)
        // -----------------------------------------------------------

        private void ContactNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            var textBox = sender as TextBox;
            if (textBox == null) return;

            int caretIndex = textBox.CaretIndex;
            string text = textBox.Text;

            if (string.IsNullOrEmpty(text)) return;

            string newText = text;
            bool changed = false;

            // 1. Первая буква фамилии -> Заглавная
            if (newText.Length > 0 && char.IsLower(newText[0]))
            {
                newText = char.ToUpper(newText[0]) + newText.Substring(1);
                changed = true;
            }

            int spaceIndex = newText.IndexOf(' ');
            if (spaceIndex != -1 && spaceIndex < newText.Length - 1)
            {
                // 2. Буква Имени -> Заглавная
                char nameChar = newText[spaceIndex + 1];
                if (char.IsLower(nameChar))
                {
                    char[] chars = newText.ToCharArray();
                    chars[spaceIndex + 1] = char.ToUpper(nameChar);
                    newText = new string(chars);
                    changed = true;
                }

                // 3. Авто-точка после имени
                // Если после буквы имени ничего нет или идет сразу буква отчества (без точки)
                if (newText.Length == spaceIndex + 2)
                {
                    newText += ".";
                    changed = true;
                    caretIndex++;
                }
                else if (newText.Length > spaceIndex + 2 && newText[spaceIndex + 2] != '.')
                {
                    // Вставка забытой точки
                    newText = newText.Insert(spaceIndex + 2, ".");
                    changed = true;
                    caretIndex++;
                }

                // 4. Буква Отчества -> Заглавная
                int firstDotIndex = newText.IndexOf('.', spaceIndex);
                if (firstDotIndex != -1 && firstDotIndex < newText.Length - 1)
                {
                    char patrChar = newText[firstDotIndex + 1];
                    if (char.IsLower(patrChar))
                    {
                        char[] chars = newText.ToCharArray();
                        chars[firstDotIndex + 1] = char.ToUpper(patrChar);
                        newText = new string(chars);
                        changed = true;
                    }

                    // 5. Авто-точка после отчества (Финал)
                    if (newText.Length == firstDotIndex + 2)
                    {
                        newText += ".";
                        changed = true;
                        caretIndex++;
                    }
                }
            }

            if (changed)
            {
                _isFormatting = true;
                textBox.Text = newText;
                textBox.CaretIndex = System.Math.Min(caretIndex, textBox.Text.Length);
                _isFormatting = false;
            }
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            var textBox = sender as TextBox;

            bool isFourDigitArea = PhoneMaskComboBox.SelectedIndex == 1;
            string digits = GetDigits(textBox.Text);

            if (string.IsNullOrEmpty(digits)) return;

            // Нормализация (меняем 8 на 7 или добавляем 7)
            if (digits.StartsWith("8")) digits = "7" + digits.Substring(1);
            if (!digits.StartsWith("7")) digits = "7" + digits;

            // Ограничение длины (всегда 11 цифр)
            if (digits.Length > 11) digits = digits.Substring(0, 11);

            // Форматирование
            string formatted = "+7";
            if (digits.Length > 1)
            {
                // (XXX) или (XXXX)
                int areaLen = isFourDigitArea ? 4 : 3;
                int taking = System.Math.Min(areaLen, digits.Length - 1);
                formatted += " (" + digits.Substring(1, taking);
            }

            int group1Start = isFourDigitArea ? 5 : 4; // Индекс начала первой группы цифр

            if (digits.Length > group1Start)
            {
                formatted += ") ";
                // Длина первой группы: 2 цифры (если код 4) или 3 цифры (если код 3)
                int group1Len = isFourDigitArea ? 2 : 3;
                int taking = System.Math.Min(group1Len, digits.Length - group1Start);
                formatted += digits.Substring(group1Start, taking);
            }

            int group2Start = isFourDigitArea ? 7 : 7; // Всегда начинается с 7-го индекса цифр (1+4+2 или 1+3+3)

            if (digits.Length > group2Start)
            {
                formatted += "-";
                int taking = System.Math.Min(2, digits.Length - group2Start);
                formatted += digits.Substring(group2Start, taking);
            }

            int group3Start = group2Start + 2; // 9
            if (digits.Length > group3Start)
            {
                formatted += "-";
                formatted += digits.Substring(group3Start, digits.Length - group3Start);
            }

            _isFormatting = true;
            textBox.Text = formatted;
            textBox.CaretIndex = textBox.Text.Length;
            _isFormatting = false;
        }

        private void PhoneMaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PhoneTextBox_TextChanged(PhoneTextBox, null);
        }

        private string GetDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return new string(input.Where(char.IsDigit).ToArray());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var customer = DataContext as Customer;

            string type = CompTypeComboBox.SelectedItem as string;
            string name = CompNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                DialogService.ShowError("Введите название компании.");
                return;
            }
            customer.CompanyName = !string.IsNullOrEmpty(type) ? $"{type} {name}" : name;

            // Проверка телефона
            string digits = GetDigits(customer.Phone);
            if (digits.Length < 11)
            {
                DialogService.ShowError("Номер телефона не полный (нужно 11 цифр).");
                return;
            }

            // Проверка Контактного лица
            if (string.IsNullOrWhiteSpace(customer.ContactName))
            {
                DialogService.ShowError("Контактное лицо обязательно.");
                return;
            }

            // Финальная проверка формата
            if (!Regex.IsMatch(customer.ContactName.Trim(), @"^[А-ЯЁ][а-яё]+\s[А-ЯЁ]\.[А-ЯЁ]\.$"))
            {
                if (!DialogService.ShowConfirmation("Формат контакта не идеален (Фамилия И.О.). Сохранить?")) return;
            }

            if (!string.IsNullOrEmpty(customer.Email) && !ValidationHelper.IsValidEmail(customer.Email))
            {
                DialogService.ShowError("Некорректный Email.");
                return;
            }

            DialogResult = true;
        }
    }
}