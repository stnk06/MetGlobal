using System.Collections.Generic;

namespace MetGlobal.Services
{
    public static class TranslationHelper
    {
        private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>
        {
            { "ProductID", "ID Продукта" },
            { "ProductCode", "Артикул" },
            { "ProductName", "Наименование" },
            { "CategoryID", "ID Категории" },
            { "Unit", "Ед. изм." },
            { "Price", "Цена" },
            { "GOST", "ГОСТ" },
            { "Density", "Плотность" },
            { "CategoryName", "Категория" },
            { "SupplierName", "Поставщик" },
            { "CustomerID", "ID Клиента" },
            { "CompanyName", "Компания" },
            { "ContactName", "Контактное лицо" },
            { "Phone", "Телефон" },
            { "Email", "Email" },
            { "LegalAddress", "Юр. адрес" },
            { "OrderID", "№ Заказа" },
            { "OrderDate", "Дата заказа" },
            { "Status", "Статус" },
            { "TotalAmount", "Сумма" },
            { "SupplierID", "ID Поставщика" },
            { "Country", "Страна" },
            { "INN", "ИНН" },
            { "ContactPhone", "Контактный телефон" },
            { "Description", "Описание" },

            { "OrdersCount", "Кол-во заказов" },
            { "Percentage", "Доля (%)" },
            { "Month", "Месяц" },
            { "TotalQuantitySold", "Продано (шт.)" },
            { "TotalRevenue", "Выручка" },
            { "OrderDateFormatted", "Дата оформления" },
            { "CustomerName", "Клиент" },
            { "ProductsCount", "Позиций" },
            { "OrderTotal", "Сумма заказа" }
        };

        public static string GetHeader(string propertyName)
        {
            return Translations.TryGetValue(propertyName, out var header) ? header : propertyName;
        }
    }
}