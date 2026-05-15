using MetGlobal.Infrastructure;
using MetGlobal.Models;
using MetGlobal.Services;
using MetGlobal.Views;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class CategoriesViewModel : ViewModelBase
    {
        public ObservableCollection<Category> Categories { get; set; }
        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public CategoriesViewModel()
        {
            Categories = new ObservableCollection<Category>();
            LoadCategories();

            AddCommand = new RelayCommand(AddCategory);
            EditCommand = new RelayCommand(EditCategory, p => SelectedCategory != null);
            DeleteCommand = new RelayCommand(DeleteCategory, p => SelectedCategory != null);
        }

        private void LoadCategories()
        {
            Categories.Clear();
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand("SELECT CategoryID, CategoryName, Description FROM Categories ORDER BY CategoryName", connection);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Categories.Add(new Category
                            {
                                CategoryID = reader.GetInt32(0),
                                CategoryName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке категорий: " + ex.Message);
            }
        }

        private void AddCategory(object obj)
        {
            var newCategory = new Category();
            var editWindow = new EditCategoryWindow(newCategory);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("INSERT INTO Categories (CategoryName, Description) VALUES (@Name, @Desc)", connection);
                        command.Parameters.AddWithValue("@Name", newCategory.CategoryName);
                        command.Parameters.AddWithValue("@Desc", (object)newCategory.Description ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Категория успешно добавлена!");
                    }
                    LoadCategories();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при добавлении категории: " + ex.Message);
                }
            }
        }

        private void EditCategory(object obj)
        {
            var categoryToEdit = new Category
            {
                CategoryID = SelectedCategory.CategoryID,
                CategoryName = SelectedCategory.CategoryName,
                Description = SelectedCategory.Description
            };

            var editWindow = new EditCategoryWindow(categoryToEdit);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("UPDATE Categories SET CategoryName = @Name, Description = @Desc WHERE CategoryID = @ID", connection);
                        command.Parameters.AddWithValue("@ID", categoryToEdit.CategoryID);
                        command.Parameters.AddWithValue("@Name", categoryToEdit.CategoryName);
                        command.Parameters.AddWithValue("@Desc", (object)categoryToEdit.Description ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Категория успешно обновлена!");
                    }
                    LoadCategories();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при обновлении категории: " + ex.Message);
                }
            }
        }

        private void DeleteCategory(object obj)
        {
            if (DialogService.ShowConfirmation($"Вы уверены, что хотите удалить категорию '{SelectedCategory.CategoryName}'?"))
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("DELETE FROM Categories WHERE CategoryID = @ID", connection);
                        command.Parameters.AddWithValue("@ID", SelectedCategory.CategoryID);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Категория успешно удалена!");
                    }
                    LoadCategories();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Не удалось удалить категорию. Возможно, она используется в других таблицах.\n\nОшибка: " + ex.Message);
                }
            }
        }
    }
}