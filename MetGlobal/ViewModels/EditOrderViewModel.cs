using MetGlobal.Infrastructure;
using MetGlobal.Models;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class EditOrderViewModel : ViewModelBase
    {
        // Основные данные
        public Order CurrentOrder { get; set; }
        public ObservableCollection<OrderDetail> OrderDetails { get; set; }

        // Списки для ComboBox'ов
        public ObservableCollection<Customer> AllCustomers { get; set; }
        public ObservableCollection<Product> AllProducts { get; set; }
        public ObservableCollection<string> AllStatuses { get; set; }

        // Свойства для добавления новой позиции в заказ
        private Product _selectedProductToAdd;
        public Product SelectedProductToAdd
        {
            get => _selectedProductToAdd;
            set { _selectedProductToAdd = value; OnPropertyChanged(); }
        }

        private decimal _quantityToAdd = 1;
        public decimal QuantityToAdd
        {
            get => _quantityToAdd;
            set { _quantityToAdd = value; OnPropertyChanged(); }
        }

        // Команды
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }

        public EditOrderViewModel(Order order)
        {
            CurrentOrder = order;
            LoadAuxiliaryData();

            OrderDetails = new ObservableCollection<OrderDetail>();
            if (CurrentOrder.OrderID != 0) // Если это редактирование существующего заказа
            {
                LoadOrderDetails();
            }

            OrderDetails.CollectionChanged += (s, e) => RecalculateTotal();

            AddDetailCommand = new RelayCommand(AddDetail, CanAddDetail);
            RemoveDetailCommand = new RelayCommand(RemoveDetail, CanRemoveDetail);
        }

        private void LoadAuxiliaryData()
        {
            AllCustomers = new ObservableCollection<Customer>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                var cmd = new SqlCommand("SELECT CustomerID, CompanyName FROM Customers ORDER BY CompanyName", conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        AllCustomers.Add(new Customer { CustomerID = reader.GetInt32(0), CompanyName = reader.GetString(1) });
                }
            }

            AllProducts = new ObservableCollection<Product>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                var cmd = new SqlCommand("SELECT ProductID, ProductName, Price FROM Products ORDER BY ProductName", conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        AllProducts.Add(new Product { ProductID = reader.GetInt32(0), ProductName = reader.GetString(1), Price = reader.GetDecimal(2) });
                }
            }

            AllStatuses = new ObservableCollection<string> { "Новый", "В обработке", "Отгружен", "Отменен" };
        }

        private void LoadOrderDetails()
        {
            string query = @"SELECT od.ProductID, p.ProductName, od.Quantity, od.UnitPrice 
                             FROM OrderDetails od
                             JOIN Products p ON od.ProductID = p.ProductID
                             WHERE od.OrderID = @OrderID";
            using (var connection = DatabaseHelper.GetConnection())
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@OrderID", CurrentOrder.OrderID);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        OrderDetails.Add(new OrderDetail
                        {
                            OrderID = CurrentOrder.OrderID,
                            ProductID = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Quantity = reader.GetDecimal(2),
                            UnitPrice = reader.GetDecimal(3)
                        });
                    }
                }
            }
        }

        private void RecalculateTotal()
        {
            CurrentOrder.TotalAmount = OrderDetails.Sum(d => d.Amount);
            OnPropertyChanged(nameof(CurrentOrder));
        }

        private bool CanAddDetail(object obj) => SelectedProductToAdd != null && QuantityToAdd > 0;
        private void AddDetail(object obj)
        {
            var existingDetail = OrderDetails.FirstOrDefault(d => d.ProductID == SelectedProductToAdd.ProductID);
            if (existingDetail != null)
            {
                existingDetail.Quantity += QuantityToAdd;
                // Нужно вручную вызвать событие для обновления UI, т.к. сам объект не меняется, а его свойство
                var index = OrderDetails.IndexOf(existingDetail);
                OrderDetails[index] = existingDetail;
            }
            else
            {
                OrderDetails.Add(new OrderDetail
                {
                    ProductID = SelectedProductToAdd.ProductID,
                    ProductName = SelectedProductToAdd.ProductName,
                    Quantity = QuantityToAdd,
                    UnitPrice = SelectedProductToAdd.Price
                });
            }
            RecalculateTotal();
        }

        private bool CanRemoveDetail(object parameter) => parameter is OrderDetail;
        private void RemoveDetail(object parameter)
        {
            if (parameter is OrderDetail detail)
            {
                OrderDetails.Remove(detail);
                RecalculateTotal();
            }
        }
    }
}
