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
    public class CustomersViewModel : ViewModelBase
    {
        public ObservableCollection<Customer> Customers { get; set; }
        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set { _selectedCustomer = value; OnPropertyChanged(); }
        }
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public CustomersViewModel()
        {
            Customers = new ObservableCollection<Customer>();
            LoadCustomers();

            AddCommand = new RelayCommand(AddCustomer);
            EditCommand = new RelayCommand(EditCustomer, (o) => SelectedCustomer != null);
            DeleteCommand = new RelayCommand(DeleteCustomer, (o) => SelectedCustomer != null);
            SearchCommand = new RelayCommand(Search);

            // ИСПРАВЛЕНО: Добавлен 'null' в качестве аргумента для изображения графика
            ExportToPdfCommand = new RelayCommand(o => ExportService.ExportToPdf(Customers, "Отчет по клиентам", null));
            ExportToExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(Customers, "Клиенты"));
        }

        private void LoadCustomers(string filter = "")
        {
            Customers.Clear();
            string query = "SELECT CustomerID, CompanyName, ContactName, Phone, Email, LegalAddress FROM Customers";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query += " WHERE CompanyName LIKE @Filter OR ContactName LIKE @Filter";
            }
            query += " ORDER BY CompanyName";

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand(query, connection);
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        command.Parameters.AddWithValue("@Filter", $"%{filter}%");
                    }
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Customers.Add(new Customer
                            {
                                CustomerID = reader.GetInt32(0),
                                CompanyName = reader.GetString(1),
                                ContactName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Phone = reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                LegalAddress = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке клиентов: " + ex.Message);
            }
        }

        private void Search(object obj)
        {
            LoadCustomers(SearchText);
        }

        private void AddCustomer(object obj)
        {
            var newCustomer = new Customer();
            var editWindow = new EditCustomerWindow(newCustomer);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "INSERT INTO Customers (CompanyName, ContactName, Phone, Email, LegalAddress) VALUES (@CompName, @ContName, @Phone, @Email, @Address)";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CompName", newCustomer.CompanyName);
                        command.Parameters.AddWithValue("@ContName", (object)newCustomer.ContactName ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", newCustomer.Phone);
                        command.Parameters.AddWithValue("@Email", (object)newCustomer.Email ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Address", (object)newCustomer.LegalAddress ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Клиент успешно добавлен!");
                    }
                    LoadCustomers(SearchText);
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при добавлении клиента: " + ex.Message);
                }
            }
        }

        private void EditCustomer(object obj)
        {
            var customerToEdit = new Customer
            {
                CustomerID = SelectedCustomer.CustomerID,
                CompanyName = SelectedCustomer.CompanyName,
                ContactName = SelectedCustomer.ContactName,
                Phone = SelectedCustomer.Phone,
                Email = SelectedCustomer.Email,
                LegalAddress = SelectedCustomer.LegalAddress
            };

            var editWindow = new EditCustomerWindow(customerToEdit);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "UPDATE Customers SET CompanyName=@CompName, ContactName=@ContName, Phone=@Phone, Email=@Email, LegalAddress=@Address WHERE CustomerID=@ID";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", customerToEdit.CustomerID);
                        command.Parameters.AddWithValue("@CompName", customerToEdit.CompanyName);
                        command.Parameters.AddWithValue("@ContName", (object)customerToEdit.ContactName ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", customerToEdit.Phone);
                        command.Parameters.AddWithValue("@Email", (object)customerToEdit.Email ?? System.DBNull.Value);
                        command.Parameters.AddWithValue("@Address", (object)customerToEdit.LegalAddress ?? System.DBNull.Value);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Данные клиента успешно обновлены!");
                    }
                    LoadCustomers(SearchText);
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Ошибка при обновлении клиента: " + ex.Message);
                }
            }
        }

        private void DeleteCustomer(object obj)
        {
            if (DialogService.ShowConfirmation($"Вы уверены, что хотите удалить клиента '{SelectedCustomer.CompanyName}'?"))
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        string query = "DELETE FROM Customers WHERE CustomerID=@ID";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", SelectedCustomer.CustomerID);
                        connection.Open();
                        command.ExecuteNonQuery();
                        DialogService.ShowInfo("Клиент успешно удален!");
                    }
                    LoadCustomers(SearchText);
                }
                catch (SqlException ex)
                {
                    DialogService.ShowError("Не удалось удалить клиента. Возможно, он используется в заказах.\n\nОшибка: " + ex.Message);
                }
            }
        }
    }
}