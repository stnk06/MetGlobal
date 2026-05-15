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
    public partial class EditSupplierWindow : Window
    {
        public List<string> SupplierTypes { get; } = new List<string>
        {
            "", "ООО", "АО", "ПАО", "ИП", "ЗАО", "ОАО", "Ltd", "Inc", "Corp", "GmbH" 
        };

        public List<string> Countries { get; } = new List<string>
        {
            "Россия", "Беларусь", "Казахстан", "Армения", "Азербайджан",
            "Кыргызстан", "Молдова", "Таджикистан", "Узбекистан", "Туркменистан",
            "Польша", "Чехия", "Словакия", "Венгрия", "Румыния", "Болгария", "Сербия",
            "Индия", "Китай"
        };

        public List<string> PhoneMasks { get; } = new List<string>
        {
            "+7 (XXX) XXX-XX-XX",
            "+7 (XXXX) XX-XX-XX"
        };

        private bool _isFormatting = false;

        public EditSupplierWindow(Supplier supplier)
        {
            InitializeComponent();

            string fullName = supplier.SupplierName ?? "";
            string foundType = SupplierTypes.FirstOrDefault(t => !string.IsNullOrEmpty(t) && fullName.StartsWith(t + " "));

            if (foundType != null)
            {
                TypeComboBox.SelectedItem = foundType;
                NameTextBox.Text = fullName.Substring(foundType.Length).Trim();
            }
            else
            {
                TypeComboBox.SelectedItem = "";
                NameTextBox.Text = fullName;
            }

            PhoneMaskComboBox.SelectedIndex = 0;

            DataContext = supplier;

            // Привязка источников данных
            TypeComboBox.ItemsSource = SupplierTypes;
            CountryComboBox.ItemsSource = Countries;
            PhoneMaskComboBox.ItemsSource = PhoneMasks;
        }


        private void BlockLatinInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[a-zA-Z]");
        }

        private void BlockNonDigits(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }


        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            var textBox = sender as TextBox;

            bool isFourDigitArea = PhoneMaskComboBox.SelectedIndex == 1;
            string digits = GetDigits(textBox.Text);

            if (string.IsNullOrEmpty(digits)) return;

            if (digits.StartsWith("8")) digits = "7" + digits.Substring(1);
            if (!digits.StartsWith("7")) digits = "7" + digits;

            if (digits.Length > 11) digits = digits.Substring(0, 11);

            string formatted = "+7";
            if (digits.Length > 1)
            {
                int areaLen = isFourDigitArea ? 4 : 3;
                int taking = System.Math.Min(areaLen, digits.Length - 1);
                formatted += " (" + digits.Substring(1, taking);
            }

            int group1Start = isFourDigitArea ? 5 : 4;
            if (digits.Length > group1Start)
            {
                formatted += ") ";
                int group1Len = isFourDigitArea ? 2 : 3;
                int taking = System.Math.Min(group1Len, digits.Length - group1Start);
                formatted += digits.Substring(group1Start, taking);
            }

            int group2Start = isFourDigitArea ? 7 : 7;
            if (digits.Length > group2Start)
            {
                formatted += "-";
                int taking = System.Math.Min(2, digits.Length - group2Start);
                formatted += digits.Substring(group2Start, taking);
            }

            int group3Start = group2Start + 2;
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
            var supplier = DataContext as Supplier;

            string type = TypeComboBox.SelectedItem as string;
            string name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                DialogService.ShowError("Введите название поставщика.");
                return;
            }
            supplier.SupplierName = !string.IsNullOrEmpty(type) ? $"{type} {name}" : name;

            if (string.IsNullOrEmpty(supplier.Country))
            {
                DialogService.ShowError("Выберите страну.");
                return;
            }

            if (!string.IsNullOrEmpty(supplier.INN))
            {
                if (supplier.INN.Length != 10)
                {
                    DialogService.ShowError("ИНН должен содержать 10 цифр.");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(supplier.ContactPhone))
            {
                string digits = GetDigits(supplier.ContactPhone);
                if (digits.Length < 11)
                {
                    DialogService.ShowError("Номер телефона не полный.");
                    return;
                }
            }

            DialogResult = true;
        }
    }
}