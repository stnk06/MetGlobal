using MetGlobal.Infrastructure;
using MetGlobal.Models;
using MetGlobal.Services;
using MetGlobal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MetGlobal.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        public ObservableCollection<Order> Orders { get; set; }
        public ObservableCollection<OrderDetail> SelectedOrderDetails { get; set; }
        public ObservableCollection<string> Statuses { get; set; }

        private Order _selectedOrder;
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                LoadOrderDetails();
            }
        }

        public string SelectedStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand AddOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public OrdersViewModel()
        {
            Orders = new ObservableCollection<Order>();
            SelectedOrderDetails = new ObservableCollection<OrderDetail>();
            Statuses = new ObservableCollection<string> { "Все статусы", "Новый", "В обработке", "Отгружен", "Отменен" };
            SelectedStatus = "Все статусы";

            LoadOrders();

            SearchCommand = new RelayCommand(p => LoadOrders());
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            AddOrderCommand = new RelayCommand(AddOrder);
            EditOrderCommand = new RelayCommand(EditOrder, p => SelectedOrder != null);

            // ИСПРАВЛЕНО: Добавлен 'null' в качестве аргумента для изображения графика
            ExportToPdfCommand = new RelayCommand(o => ExportService.ExportToPdf(Orders, "Отчет по заказам", null));
            ExportToExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(Orders, "Заказы"));
        }

        private void AddOrder(object obj)
        {
            var newOrder = new Order { OrderDate = DateTime.Now, Status = "Новый" };
            var editWindow = new EditOrderWindow(newOrder);
            if (editWindow.ShowDialog() == true)
            {
                var vm = editWindow.DataContext as EditOrderViewModel;
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        string orderQuery = "INSERT INTO Orders (CustomerID, OrderDate, Status, TotalAmount) VALUES (@CustomerID, @OrderDate, @Status, @TotalAmount); SELECT SCOPE_IDENTITY();";
                        var orderCmd = new SqlCommand(orderQuery, connection, transaction);
                        orderCmd.Parameters.AddWithValue("@CustomerID", vm.CurrentOrder.CustomerID);
                        orderCmd.Parameters.AddWithValue("@OrderDate", vm.CurrentOrder.OrderDate);
                        orderCmd.Parameters.AddWithValue("@Status", vm.CurrentOrder.Status);
                        orderCmd.Parameters.AddWithValue("@TotalAmount", vm.CurrentOrder.TotalAmount);
                        int newOrderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                        foreach (var detail in vm.OrderDetails)
                        {
                            string detailQuery = "INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice);";
                            var detailCmd = new SqlCommand(detailQuery, connection, transaction);
                            detailCmd.Parameters.AddWithValue("@OrderID", newOrderId);
                            detailCmd.Parameters.AddWithValue("@ProductID", detail.ProductID);
                            detailCmd.Parameters.AddWithValue("@Quantity", detail.Quantity);
                            detailCmd.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice);
                            detailCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        DialogService.ShowInfo("Новый заказ успешно создан!");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        DialogService.ShowError("Произошла ошибка при создании заказа: " + ex.Message);
                    }
                }
                LoadOrders();
            }
        }

        private void EditOrder(object obj)
        {
            var orderToEdit = new Order
            {
                OrderID = SelectedOrder.OrderID,
                CustomerID = SelectedOrder.CustomerID,
                OrderDate = SelectedOrder.OrderDate,
                Status = SelectedOrder.Status,
                TotalAmount = SelectedOrder.TotalAmount
            };
            var editWindow = new EditOrderWindow(orderToEdit);
            if (editWindow.ShowDialog() == true)
            {
                var vm = editWindow.DataContext as EditOrderViewModel;
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        string orderQuery = "UPDATE Orders SET CustomerID = @CustomerID, OrderDate = @OrderDate, Status = @Status, TotalAmount = @TotalAmount WHERE OrderID = @OrderID;";
                        var orderCmd = new SqlCommand(orderQuery, connection, transaction);
                        orderCmd.Parameters.AddWithValue("@OrderID", vm.CurrentOrder.OrderID);
                        orderCmd.Parameters.AddWithValue("@CustomerID", vm.CurrentOrder.CustomerID);
                        orderCmd.Parameters.AddWithValue("@OrderDate", vm.CurrentOrder.OrderDate);
                        orderCmd.Parameters.AddWithValue("@Status", vm.CurrentOrder.Status);
                        orderCmd.Parameters.AddWithValue("@TotalAmount", vm.CurrentOrder.TotalAmount);
                        orderCmd.ExecuteNonQuery();

                        var deleteCmd = new SqlCommand("DELETE FROM OrderDetails WHERE OrderID = @OrderID", connection, transaction);
                        deleteCmd.Parameters.AddWithValue("@OrderID", vm.CurrentOrder.OrderID);
                        deleteCmd.ExecuteNonQuery();

                        foreach (var detail in vm.OrderDetails)
                        {
                            string detailQuery = "INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice);";
                            var detailCmd = new SqlCommand(detailQuery, connection, transaction);
                            detailCmd.Parameters.AddWithValue("@OrderID", vm.CurrentOrder.OrderID);
                            detailCmd.Parameters.AddWithValue("@ProductID", detail.ProductID);
                            detailCmd.Parameters.AddWithValue("@Quantity", detail.Quantity);
                            detailCmd.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice);
                            detailCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        DialogService.ShowInfo("Заказ успешно обновлен!");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        DialogService.ShowError("Произошла ошибка при обновлении заказа: " + ex.Message);
                    }
                }
                LoadOrders();
            }
        }

        private void ResetFilters(object obj)
        {
            SelectedStatus = "Все статусы";
            StartDate = null;
            EndDate = null;
            OnPropertyChanged(nameof(SelectedStatus));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            LoadOrders();
        }

        private void LoadOrders()
        {
            Orders.Clear();
            var query = @"SELECT o.OrderID, o.OrderDate, o.Status, o.TotalAmount, c.CustomerID, c.CompanyName 
                          FROM Orders o
                          JOIN Customers c ON o.CustomerID = c.CustomerID";

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Все статусы")
            {
                conditions.Add("o.Status = @Status");
                parameters.Add(new SqlParameter("@Status", SelectedStatus));
            }
            if (StartDate.HasValue)
            {
                conditions.Add("o.OrderDate >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", StartDate.Value));
            }
            if (EndDate.HasValue)
            {
                conditions.Add("o.OrderDate <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", EndDate.Value.Date.AddDays(1).AddTicks(-1)));
            }

            if (conditions.Any())
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }
            query += " ORDER BY o.OrderDate DESC";

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddRange(parameters.ToArray());
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Orders.Add(new Order
                            {
                                OrderID = reader.GetInt32(0),
                                OrderDate = reader.GetDateTime(1),
                                Status = reader.GetString(2),
                                TotalAmount = reader.GetDecimal(3),
                                CustomerID = reader.GetInt32(4),
                                CompanyName = reader.GetString(5)
                            });
                        }
                    }
                }
                OnPropertyChanged(nameof(Orders));
                if (SelectedOrder == null)
                {
                    SelectedOrder = Orders.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке заказов: " + ex.Message);
            }
        }

        private void LoadOrderDetails()
        {
            SelectedOrderDetails.Clear();
            if (SelectedOrder == null)
            {
                return;
            }

            var query = @"SELECT od.OrderDetailID, od.Quantity, od.UnitPrice, p.ProductID, p.ProductName, p.ProductCode
                          FROM OrderDetails od
                          JOIN Products p ON od.ProductID = p.ProductID
                          WHERE od.OrderID = @OrderID";

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderID", SelectedOrder.OrderID);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SelectedOrderDetails.Add(new OrderDetail
                            {
                                OrderDetailID = reader.GetInt32(0),
                                Quantity = reader.GetDecimal(1),
                                UnitPrice = reader.GetDecimal(2),
                                ProductID = reader.GetInt32(3),
                                ProductName = reader.GetString(4),
                                ProductCode = reader.GetString(5),
                                OrderID = SelectedOrder.OrderID
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                DialogService.ShowError("Ошибка при загрузке деталей заказа: " + ex.Message);
            }
        }
    }
}