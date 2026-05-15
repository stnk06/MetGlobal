using MetGlobal.Models;
using MetGlobal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MetGlobal.Views
{
    public partial class EditProductWindow : Window
    {
        // Списки для выпадающих меню
        public List<string> MaterialPrefixes { get; } = new List<string>
        {
            "ST-", // Сталь
            "AL-", // Алюминий
            "BR-", // Бронза
            "SS-", // Нержавейка
            "CU-", // Медь
            "TI-", // Титан
            "PL-", // Пластик
            "WD-"  // Дерево
        };

        public List<string> MeasurementUnits { get; } = new List<string>
        {
            "шт",
            "м",
            "кв. м",
            "куб. м",
            "кг",
            "т",
            "л",
            "компл."
        };

        // Временные поля для сборки артикула
        public string SelectedPrefix { get; set; }
        public string CodeSuffix { get; set; }

        public EditProductWindow(Product product, IEnumerable<Category> categories, IEnumerable<Supplier> suppliers)
        {
            InitializeComponent();

            // Инициализация данных
            var tempProduct = product;

            // Разбор существующего артикула (если есть) на Префикс и Суффикс
            if (!string.IsNullOrEmpty(tempProduct.ProductCode))
            {
                // Пытаемся найти известный префикс
                string foundPrefix = MaterialPrefixes.FirstOrDefault(p => tempProduct.ProductCode.StartsWith(p));
                if (foundPrefix != null)
                {
                    SelectedPrefix = foundPrefix;
                    CodeSuffix = tempProduct.ProductCode.Substring(foundPrefix.Length);
                }
                else
                {
                    // Если префикс нестандартный, ставим первый по умолчанию, а остальное в суффикс
                    SelectedPrefix = MaterialPrefixes[0];
                    CodeSuffix = tempProduct.ProductCode;
                }
            }
            else
            {
                SelectedPrefix = MaterialPrefixes[0];
                CodeSuffix = string.Empty;
            }

            // Инициализация ГОСТа (добавляем префикс, если пусто)
            if (string.IsNullOrWhiteSpace(tempProduct.GOST))
            {
                tempProduct.GOST = "ГОСТ ";
            }
            else if (!tempProduct.GOST.Trim().StartsWith("ГОСТ"))
            {
                tempProduct.GOST = "ГОСТ " + tempProduct.GOST;
            }

            DataContext = tempProduct;

            // Привязываем списки для ComboBox (Categories и Suppliers приходят извне, а локальные списки через this)
            CategoryComboBox.ItemsSource = categories;
            SupplierComboBox.ItemsSource = suppliers;

            // Привязка локальных списков
            PrefixComboBox.ItemsSource = MaterialPrefixes;
            PrefixComboBox.SelectedItem = SelectedPrefix;

            SuffixTextBox.Text = CodeSuffix;

            UnitComboBox.ItemsSource = MeasurementUnits;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var product = DataContext as Product;

            // 1. Сборка Артикула
            string prefix = PrefixComboBox.SelectedItem as string;
            string suffix = SuffixTextBox.Text;

            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(suffix))
            {
                DialogService.ShowError("Артикул должен состоять из типа (префикса) и номера.");
                return;
            }
            product.ProductCode = prefix + suffix;

            // 2. Валидация стандартных полей
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                DialogService.ShowError("Наименование продукта не может быть пустым.");
                return;
            }

            if (product.CategoryID == 0 || product.SupplierID == 0)
            {
                DialogService.ShowError("Необходимо выбрать категорию и поставщика.");
                return;
            }

            if (string.IsNullOrWhiteSpace(product.Unit))
            {
                DialogService.ShowError("Необходимо выбрать единицу измерения.");
                return;
            }

            // 3. Проверка ГОСТа (должен быть длиннее чем просто "ГОСТ ")
            if (product.GOST != null && product.GOST.Trim() == "ГОСТ")
            {
                // Если пользователь ничего не ввел кроме префикса, можно либо очистить, либо требовать ввод
                // В данном случае считаем это пустым полем
                product.GOST = null;
            }

            this.DialogResult = true;
        }

        // --- Логика защиты поля ГОСТ ---

        private void GostTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;

            // Запрещаем удаление префикса "ГОСТ " (длина 5 символов)
            // Если курсор стоит в начале или выделение затрагивает префикс
            if (textBox.SelectionStart < 5)
            {
                // Запрещаем Backspace если мы на границе
                if (e.Key == Key.Back && textBox.SelectionStart == 5 && textBox.SelectionLength == 0)
                {
                    e.Handled = true;
                    return;
                }

                // Запрещаем редактирование первых 5 символов
                // Разрешаем только стрелки
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Tab)
                {
                    return;
                }

                // Перемещаем курсор в конец префикса, если пытаются писать в начале
                if (textBox.SelectionLength == 0)
                {
                    textBox.CaretIndex = 5;
                }
                else if (textBox.SelectionStart < 5)
                {
                    // Если пытаются выделить и удалить префикс
                    textBox.SelectionStart = 5;
                    textBox.SelectionLength = 0;
                    e.Handled = true;
                }
            }

            // Запрет Backspace, если он сотрет часть префикса
            if (e.Key == Key.Back && textBox.CaretIndex <= 5 && textBox.SelectionLength == 0)
            {
                e.Handled = true;
            }
        }

        private void GostTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string prefix = "ГОСТ ";

            // Если вдруг префикс стерли (например, выделили всё и нажали Delete), восстанавливаем его
            if (!textBox.Text.StartsWith(prefix))
            {
                // Пытаемся сохранить то, что пользователь ввел, убрав обрывки префикса если есть
                string content = textBox.Text.Replace("ГОСТ", "").Replace("ГОС", "").Replace("ГО", "").Replace("Г", "").TrimStart();

                textBox.Text = prefix + content;
                textBox.CaretIndex = textBox.Text.Length; // Курсор в конец
            }
        }
    }
}