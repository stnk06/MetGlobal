using MetGlobal.Infrastructure;
using MetGlobal.Models;
using MetGlobal.Services;
using MetGlobal.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class ProductsViewModel : ViewModelBase
    {
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Category> FilterCategories { get; private set; }
        public ObservableCollection<Supplier> AllSuppliers { get; private set; }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        private Category _selectedFilterCategory;
        public Category SelectedFilterCategory
        {
            get => _selectedFilterCategory;
            set
            {
                _selectedFilterCategory = value;
                OnPropertyChanged();
                LoadProducts();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public ProductsViewModel()
        {
            Products = new ObservableCollection<Product>();
            FilterCategories = new ObservableCollection<Category>();
            AllSuppliers = new ObservableCollection<Supplier>();

            LoadData();

            AddCommand = new RelayCommand(AddProduct);
            EditCommand = new RelayCommand(EditProduct, p => SelectedProduct != null);
            DeleteCommand = new RelayCommand(DeleteProduct, p => SelectedProduct != null);
            SearchCommand = new RelayCommand(p => LoadProducts());

            // ИСПРАВЛЕНО: Добавлен 'null' в качестве аргумента для изображения графика
            ExportToPdfCommand = new RelayCommand(o => ExportService.ExportToPdf(Products, "Отчет по продукции", null));
            ExportToExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(Products, "Продукция"));
        }

        private void LoadData()
        {
            try
            {
                FilterCategories.Clear();
                FilterCategories.Add(new Category { CategoryID = 0, CategoryName = "Все категории" });
                string catQuery = "SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName";
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var catCmd = new SqlCommand(catQuery, conn);
                    using (var reader = catCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FilterCategories.Add(new Category { CategoryID = reader.GetInt32(0), CategoryName = reader.GetString(1) });
                        }
                    }

                    AllSuppliers.Clear();
                    string supQuery = "SELECT SupplierID, SupplierName FROM Suppliers ORDER BY SupplierName";
                    var supCmd = new SqlCommand(supQuery, conn);
                    using (var reader = supCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AllSuppliers.Add(new Supplier { SupplierID = reader.GetInt32(0), SupplierName = reader.GetString(1) });
                        }
                    }
                }
                LoadProducts();
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка доступа к базе данных: " + ex.Message);
            }
        }

        private void LoadProducts()
        {
            Products.Clear();
            var query = @"SELECT 
                            p.ProductID, p.ProductCode, p.ProductName, p.Unit, p.Price, p.GOST, p.Density,
                            c.CategoryID, c.CategoryName,
                            s.SupplierID, s.SupplierName
                          FROM Products p
                          JOIN Categories c ON p.CategoryID = c.CategoryID
                          JOIN Suppliers s ON p.SupplierID = s.SupplierID";

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (SelectedFilterCategory != null && SelectedFilterCategory.CategoryID != 0)
            {
                conditions.Add("p.CategoryID = @CategoryID");
                parameters.Add(new SqlParameter("@CategoryID", SelectedFilterCategory.CategoryID));
            }

            if (conditions.Any())
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }
            query += " ORDER BY p.ProductName";

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand(query, connection);
                    if (parameters.Any()) command.Parameters.AddRange(parameters.ToArray());
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Products.Add(new Product
                            {
                                ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                                ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                GOST = reader.IsDBNull(reader.GetOrdinal("GOST")) ? null : reader.GetString(reader.GetOrdinal("GOST")),
                                Density = reader.IsDBNull(reader.GetOrdinal("Density")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Density")),
                                CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                SupplierID = reader.GetInt32(reader.GetOrdinal("SupplierID")),
                                SupplierName = reader.GetString(reader.GetOrdinal("SupplierName"))
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке продуктов: " + ex.Message);
            }
        }

        private void AddProduct(object obj)
        {
            var newProduct = new Product();
            var editWindow = new EditProductWindow(newProduct, FilterCategories.Where(c => c.CategoryID != 0), AllSuppliers);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "INSERT INTO Products (ProductCode, ProductName, CategoryID, SupplierID, Unit, Price, GOST, Density) VALUES (@Code, @Name, @CatID, @SupID, @Unit, @Price, @GOST, @Density)";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@Code", newProduct.ProductCode);
                        command.Parameters.AddWithValue("@Name", newProduct.ProductName);
                        command.Parameters.AddWithValue("@CatID", newProduct.CategoryID);
                        command.Parameters.AddWithValue("@SupID", newProduct.SupplierID);
                        command.Parameters.AddWithValue("@Unit", newProduct.Unit);
                        command.Parameters.AddWithValue("@Price", newProduct.Price);
                        command.Parameters.AddWithValue("@GOST", (object)newProduct.GOST ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Density", (object)newProduct.Density ?? System.DBNull.Value);

                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Продукт успешно добавлен!");
                    }
                    LoadProducts();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при добавлении продукта: " + ex.Message);
                }
            }
        }

        private void EditProduct(object obj)
        {
            var productToEdit = new Product
            {
                ProductID = SelectedProduct.ProductID,
                ProductCode = SelectedProduct.ProductCode,
                ProductName = SelectedProduct.ProductName,
                CategoryID = SelectedProduct.CategoryID,
                SupplierID = SelectedProduct.SupplierID,
                Unit = SelectedProduct.Unit,
                Price = SelectedProduct.Price,
                GOST = SelectedProduct.GOST,
                Density = SelectedProduct.Density
            };

            var editWindow = new EditProductWindow(productToEdit, FilterCategories.Where(c => c.CategoryID != 0), AllSuppliers);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "UPDATE Products SET ProductCode = @Code, ProductName = @Name, CategoryID = @CatID, SupplierID = @SupID, Unit = @Unit, Price = @Price, GOST = @GOST, Density = @Density WHERE ProductID = @ID";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", productToEdit.ProductID);
                        command.Parameters.AddWithValue("@Code", productToEdit.ProductCode);
                        command.Parameters.AddWithValue("@Name", productToEdit.ProductName);
                        command.Parameters.AddWithValue("@CatID", productToEdit.CategoryID);
                        command.Parameters.AddWithValue("@SupID", productToEdit.SupplierID);
                        command.Parameters.AddWithValue("@Unit", productToEdit.Unit);
                        command.Parameters.AddWithValue("@Price", productToEdit.Price);
                        command.Parameters.AddWithValue("@GOST", (object)productToEdit.GOST ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Density", (object)productToEdit.Density ?? System.DBNull.Value);

                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Продукт успешно обновлен!");
                    }
                    LoadProducts();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при обновлении продукта: " + ex.Message);
                }
            }
        }

        private void DeleteProduct(object obj)
        {
            if (DialogService.ShowConfirmation($"Вы уверены, что хотите удалить '{SelectedProduct.ProductName}'?"))
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "DELETE FROM Products WHERE ProductID = @ID";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", SelectedProduct.ProductID);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Продукт успешно удален!");
                    }
                    LoadProducts();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Не удалось удалить продукт. Возможно, он используется в заказах.\n\nОшибка: " + ex.Message);
                }
            }
        }
    }
}