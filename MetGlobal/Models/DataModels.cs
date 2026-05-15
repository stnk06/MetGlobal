using System;

namespace MetGlobal.Models
{
    /// <summary>
    /// Модель для таблицы Категорий (Categories).
    /// </summary>
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Модель для таблицы Поставщиков (Suppliers).
    /// </summary>
    public class Supplier
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string Country { get; set; }
        public string INN { get; set; }
        public string ContactPhone { get; set; }
    }

    /// <summary>
    /// Модель для таблицы Продукции (Products).
    /// </summary>
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int CategoryID { get; set; }
        public int SupplierID { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public string GOST { get; set; }
        public decimal? Density { get; set; } // Nullable, так как может быть NULL в БД

        // Дополнительные свойства для удобного отображения в DataGrid
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }
    }

    /// <summary>
    /// Модель для таблицы Клиентов (Customers).
    /// </summary>
    public class Customer
    {
        public int CustomerID { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string LegalAddress { get; set; }
    }

    /// <summary>
    /// Модель для таблицы Заказов (Orders).
    /// </summary>
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }

        // Дополнительное свойство для удобного отображения в DataGrid
        public string CompanyName { get; set; }
    }

    /// <summary>
    /// Модель для таблицы Состава заказов (OrderDetails).
    /// </summary>
    public class OrderDetail
    {
        public int OrderDetailID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Дополнительные свойства для удобного отображения
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal Amount => Quantity * UnitPrice; // Вычисляемое свойство суммы
    }
}
