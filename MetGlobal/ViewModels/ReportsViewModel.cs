using MetGlobal.Infrastructure;
using MetGlobal.Models;
using MetGlobal.Services;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;

namespace MetGlobal.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        public ObservableCollection<CategorySalesReport> CategoryReports { get; set; }
        public ObservableCollection<MonthlySalesReport> MonthlyReports { get; set; }
        public ObservableCollection<CustomerSalesReport> CustomerReports { get; set; }
        public ObservableCollection<SupplierProductReport> SupplierReports { get; set; }
        public ObservableCollection<DailyOrdersReport> DailyReports { get; set; }

        public SeriesCollection CategorySeries { get; set; }
        public SeriesCollection MonthlySeries { get; set; }
        public string[] MonthlyLabels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public ICommand RefreshCommand { get; }

        public ICommand ExportCategoryPdfCommand { get; }
        public ICommand ExportMonthlyPdfCommand { get; }
        public ICommand ExportCustomerPdfCommand { get; }
        public ICommand ExportSupplierPdfCommand { get; }
        public ICommand ExportDailyPdfCommand { get; }

        public ICommand ExportCategoryExcelCommand { get; }
        public ICommand ExportMonthlyExcelCommand { get; }
        public ICommand ExportCustomerExcelCommand { get; }
        public ICommand ExportSupplierExcelCommand { get; }
        public ICommand ExportDailyExcelCommand { get; }

        public ReportsViewModel()
        {
            CategoryReports = new ObservableCollection<CategorySalesReport>();
            MonthlyReports = new ObservableCollection<MonthlySalesReport>();
            CustomerReports = new ObservableCollection<CustomerSalesReport>();
            SupplierReports = new ObservableCollection<SupplierProductReport>();
            DailyReports = new ObservableCollection<DailyOrdersReport>();

            CategorySeries = new SeriesCollection();
            MonthlySeries = new SeriesCollection();
            YFormatter = value => value.ToString("N0");

            RefreshCommand = new RelayCommand(o => LoadAllReports());

            // РУСИФИЦИРОВАННЫЕ ЗАГОЛОВКИ И ПАРАМЕТРЫ ДЛЯ PDF
            // ИСПРАВЛЕНИЕ: Используем RenderControlToImage вместо прямого кастинга
            ExportCategoryPdfCommand = new RelayCommand(chart =>
                ExportService.ExportToPdf(CategoryReports, "Продажи по категориям", ExportService.RenderControlToImage(chart as FrameworkElement), null, "", new[] { "TotalAmount" }));

            ExportMonthlyPdfCommand = new RelayCommand(chart =>
                ExportService.ExportToPdf(MonthlyReports, "Динамика продаж", ExportService.RenderControlToImage(chart as FrameworkElement), null, "", new[] { "TotalAmount", "OrdersCount" }));

            ExportCustomerPdfCommand = new RelayCommand(o =>
                ExportService.ExportToPdf(CustomerReports, "Рейтинг клиентов", null, null, "", new[] { "TotalAmount", "OrdersCount" }));

            ExportSupplierPdfCommand = new RelayCommand(o =>
                ExportService.ExportToPdf(SupplierReports, "Продукция по Поставщикам", null,
                    x => x.SupplierName, "Поставщик:", new[] { "TotalRevenue", "TotalQuantitySold" }));

            ExportDailyPdfCommand = new RelayCommand(o =>
                ExportService.ExportToPdf(DailyReports, "Ежедневный журнал продаж", null,
                    x => x.OrderDateFormatted, "Дата:", new[] { "OrderTotal" }));

            // Команды Excel (заголовки листов)
            ExportCategoryExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(CategoryReports, "Категории"));
            ExportMonthlyExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(MonthlyReports, "Месяцы"));
            ExportCustomerExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(CustomerReports, "Клиенты"));
            ExportSupplierExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(SupplierReports, "Поставщики"));
            ExportDailyExcelCommand = new RelayCommand(o => ExportService.ExportToExcel(DailyReports, "Журнал"));

            LoadAllReports();
        }

        private async void LoadAllReports()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        LoadCategoryReport(connection);
                        LoadMonthlyReport(connection);
                        LoadCustomerReport(connection);
                        LoadSupplierReport(connection);
                        LoadDailyReport(connection);
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        DialogService.ShowError("Ошибка загрузки отчетов: " + ex.Message));
                }
            });
        }

        private void LoadCategoryReport(SqlConnection conn)
        {
            var list = new System.Collections.Generic.List<CategorySalesReport>();
            decimal globalTotal = 0;

            var cmd = new SqlCommand(@"
                SELECT 
                    c.CategoryName, 
                    SUM(od.Quantity * od.UnitPrice) as Total 
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                JOIN Categories c ON p.CategoryID = c.CategoryID
                JOIN Orders o ON od.OrderID = o.OrderID
                WHERE o.Status != 'Отменен'
                GROUP BY c.CategoryName
                ORDER BY Total DESC", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var amount = reader.GetDecimal(1);
                    globalTotal += amount;
                    list.Add(new CategorySalesReport
                    {
                        CategoryName = reader.GetString(0),
                        TotalAmount = amount
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CategoryReports.Clear();
                CategorySeries.Clear();
                foreach (var item in list)
                {
                    item.Percentage = globalTotal > 0 ? (double)(item.TotalAmount / globalTotal) : 0;
                    CategoryReports.Add(item);

                    if (item.Percentage > 0.02)
                    {
                        CategorySeries.Add(new PieSeries
                        {
                            Title = item.CategoryName,
                            Values = new ChartValues<decimal> { item.TotalAmount },
                            DataLabels = true,
                            LabelPoint = point => $"{point.Y:N0} ({point.Participation:P0})"
                        });
                    }
                }
            });
        }

        private void LoadMonthlyReport(SqlConnection conn)
        {
            var list = new System.Collections.Generic.List<MonthlySalesReport>();
            var cmd = new SqlCommand(@"
                SELECT 
                    FORMAT(OrderDate, 'yyyy-MM') as MonthStr,
                    COUNT(OrderID) as Cnt,
                    SUM(TotalAmount) as Total
                FROM Orders
                WHERE Status != 'Отменен'
                GROUP BY FORMAT(OrderDate, 'yyyy-MM')
                ORDER BY MonthStr", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string rawDate = reader.GetString(0);
                    DateTime dt = DateTime.ParseExact(rawDate, "yyyy-MM", CultureInfo.InvariantCulture);

                    list.Add(new MonthlySalesReport
                    {
                        RawSortableDate = rawDate,
                        Month = dt.ToString("MMMM yyyy", new CultureInfo("ru-RU")),
                        OrdersCount = reader.GetInt32(1),
                        TotalAmount = reader.GetDecimal(2)
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MonthlyReports.Clear();
                MonthlySeries.Clear();
                foreach (var item in list) MonthlyReports.Add(item);

                MonthlyLabels = list.Select(x => x.Month).ToArray();
                MonthlySeries.Add(new ColumnSeries
                {
                    Title = "Продажи",
                    Values = new ChartValues<decimal>(list.Select(x => x.TotalAmount))
                });
                OnPropertyChanged(nameof(MonthlyLabels));
            });
        }

        private void LoadCustomerReport(SqlConnection conn)
        {
            var list = new System.Collections.Generic.List<CustomerSalesReport>();
            var cmd = new SqlCommand(@"
                SELECT TOP 20
                    c.CompanyName,
                    COUNT(o.OrderID) as Cnt,
                    SUM(o.TotalAmount) as Total
                FROM Orders o
                JOIN Customers c ON o.CustomerID = c.CustomerID
                WHERE o.Status != 'Отменен'
                GROUP BY c.CompanyName
                ORDER BY Total DESC", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new CustomerSalesReport
                    {
                        CompanyName = reader.GetString(0),
                        OrdersCount = reader.GetInt32(1),
                        TotalAmount = reader.GetDecimal(2)
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CustomerReports.Clear();
                foreach (var item in list) CustomerReports.Add(item);
            });
        }

        private void LoadSupplierReport(SqlConnection conn)
        {
            var list = new System.Collections.Generic.List<SupplierProductReport>();
            var cmd = new SqlCommand(@"
                SELECT 
                    s.SupplierName,
                    p.ProductName,
                    p.ProductCode,
                    p.Unit,
                    SUM(od.Quantity) as TotalQty,
                    SUM(od.Quantity * od.UnitPrice) as Revenue
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                JOIN Suppliers s ON p.SupplierID = s.SupplierID
                JOIN Orders o ON od.OrderID = o.OrderID
                WHERE o.Status = 'Отгружен'
                GROUP BY s.SupplierName, p.ProductName, p.ProductCode, p.Unit
                ORDER BY s.SupplierName, Revenue DESC", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new SupplierProductReport
                    {
                        SupplierName = reader.GetString(0),
                        ProductName = reader.GetString(1),
                        ProductCode = reader.GetString(2),
                        Unit = reader.GetString(3),
                        TotalQuantitySold = reader.GetDecimal(4),
                        TotalRevenue = reader.GetDecimal(5)
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                SupplierReports.Clear();
                foreach (var item in list) SupplierReports.Add(item);
            });
        }

        private void LoadDailyReport(SqlConnection conn)
        {
            var list = new System.Collections.Generic.List<DailyOrdersReport>();
            var cmd = new SqlCommand(@"
                SELECT 
                    o.OrderDate,
                    o.OrderID,
                    c.CompanyName,
                    o.Status,
                    (SELECT COUNT(*) FROM OrderDetails WHERE OrderID = o.OrderID) as ProdCount,
                    o.TotalAmount
                FROM Orders o
                JOIN Customers c ON o.CustomerID = c.CustomerID
                ORDER BY o.OrderDate DESC", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var date = reader.GetDateTime(0);
                    list.Add(new DailyOrdersReport
                    {
                        OrderDateFormatted = date.ToString("d MMMM yyyy, dddd", new CultureInfo("ru-RU")),
                        OrderID = reader.GetInt32(1),
                        CustomerName = reader.GetString(2),
                        Status = reader.GetString(3),
                        ProductsCount = reader.GetInt32(4),
                        OrderTotal = reader.GetDecimal(5)
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DailyReports.Clear();
                foreach (var item in list) DailyReports.Add(item);
            });
        }
    }
}