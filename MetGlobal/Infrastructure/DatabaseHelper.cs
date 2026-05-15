using System.Configuration;
using System.Data.SqlClient;

namespace MetGlobal.Infrastructure
{
    /// <summary>
    /// Статический класс для управления подключением к базе данных.
    /// Он считывает строку подключения из файла App.config.
    /// </summary>
    public static class DatabaseHelper
    {
        // Хранит строку подключения, полученную из App.config
        private static readonly string connectionString;


        static DatabaseHelper()
        {
            connectionString = ConfigurationManager.ConnectionStrings["MetGlobalDB"].ConnectionString;
        }

        /// <summary>
        /// Создает и возвращает новый объект подключения к базе данных.
        /// </summary>
        /// <returns>Открываемый объект SqlConnection.</returns>
        public static SqlConnection GetConnection()
        {
            // Создаем новый экземпляр подключения с нашей строкой
            return new SqlConnection(connectionString);
        }
    }
}
