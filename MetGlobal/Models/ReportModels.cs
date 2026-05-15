using System;

namespace MetGlobal.Models
{
    public class CustomerSalesReport
    {
        public string CompanyName { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CategorySalesReport
    {
        public string CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }

    public class MonthlySalesReport
    {
        public string Month { get; set; }
        public string RawSortableDate { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SupplierProductReport
    {
        public string SupplierName { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Unit { get; set; }
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyOrdersReport
    {
        public string OrderDateFormatted { get; set; }
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public int ProductsCount { get; set; }
        public decimal OrderTotal { get; set; }
    }
}