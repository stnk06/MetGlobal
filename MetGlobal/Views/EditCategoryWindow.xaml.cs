using MetGlobal.Models;
using MetGlobal.Services;
using System.Windows;

namespace MetGlobal.Views
{
    public partial class EditCategoryWindow : Window
    {
        public EditCategoryWindow(Category category)
        {
            InitializeComponent();
            DataContext = category;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var category = DataContext as Category;
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                DialogService.ShowError("Название категории не может быть пустым.");
                return;
            }
            if (!ValidationHelper.IsTextOnly(category.CategoryName))
            {
                DialogService.ShowError("Название категории может содержать только буквы.");
                return;
            }

            this.DialogResult = true;
        }
    }
}