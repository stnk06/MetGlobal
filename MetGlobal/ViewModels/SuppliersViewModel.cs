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
    public class SuppliersViewModel : ViewModelBase
    {
        public ObservableCollection<Supplier> Suppliers { get; set; }
        private Supplier _selectedSupplier;
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set { _selectedSupplier = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public SuppliersViewModel()
        {
            Suppliers = new ObservableCollection<Supplier>();
            LoadSuppliers();

            AddCommand = new RelayCommand(AddSupplier);
            EditCommand = new RelayCommand(EditSupplier, p => SelectedSupplier != null);
            DeleteCommand = new RelayCommand(DeleteSupplier, p => SelectedSupplier != null);
        }

        private void LoadSuppliers()
        {
            Suppliers.Clear();
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand("SELECT SupplierID, SupplierName, Country, INN, ContactPhone FROM Suppliers ORDER BY SupplierName", connection);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Suppliers.Add(new Supplier
                            {
                                SupplierID = reader.GetInt32(0),
                                SupplierName = reader.GetString(1),
                                Country = reader.GetString(2),
                                INN = reader.IsDBNull(3) ? null : reader.GetString(3),
                                ContactPhone = reader.IsDBNull(4) ? null : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке поставщиков: " + ex.Message);
            }
        }

        private void AddSupplier(object obj)
        {
            var newSupplier = new Supplier();
            var editWindow = new EditSupplierWindow(newSupplier);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("INSERT INTO Suppliers (SupplierName, Country, INN, ContactPhone) VALUES (@Name, @Country, @INN, @Phone)", connection);
                        command.Parameters.AddWithValue("@Name", newSupplier.SupplierName);
                        command.Parameters.AddWithValue("@Country", newSupplier.Country);
                        command.Parameters.AddWithValue("@INN", (object)newSupplier.INN ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", (object)newSupplier.ContactPhone ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Поставщик успешно добавлен!");
                    }
                    LoadSuppliers();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при добавлении поставщика: " + ex.Message);
                }
            }
        }

        private void EditSupplier(object obj)
        {
            var supplierToEdit = new Supplier
            {
                SupplierID = SelectedSupplier.SupplierID,
                SupplierName = SelectedSupplier.SupplierName,
                Country = SelectedSupplier.Country,
                INN = SelectedSupplier.INN,
                ContactPhone = SelectedSupplier.ContactPhone
            };
            var editWindow = new EditSupplierWindow(supplierToEdit);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("UPDATE Suppliers SET SupplierName = @Name, Country = @Country, INN = @INN, ContactPhone = @Phone WHERE SupplierID = @ID", connection);
                        command.Parameters.AddWithValue("@ID", supplierToEdit.SupplierID);
                        command.Parameters.AddWithValue("@Name", supplierToEdit.SupplierName);
                        command.Parameters.AddWithValue("@Country", supplierToEdit.Country);
                        command.Parameters.AddWithValue("@INN", (object)supplierToEdit.INN ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", (object)supplierToEdit.ContactPhone ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Поставщик успешно обновлен!");
                    }
                    LoadSuppliers();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при обновлении поставщика: " + ex.Message);
                }
            }
        }

        private void DeleteSupplier(object obj)
        {
            if (DialogService.ShowConfirmation($"Вы уверены, что хотите удалить поставщика '{SelectedSupplier.SupplierName}'?"))
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        var command = new SqlCommand("DELETE FROM Suppliers WHERE SupplierID = @ID", connection);
                        command.Parameters.AddWithValue("@ID", SelectedSupplier.SupplierID);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Поставщик успешно удален!");
                    }
                    LoadSuppliers();
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Не удалось удалить поставщика. Возможно, он используется в других таблицах.\n\nОшибка: " + ex.Message);
                }
            }
        }
    }
}