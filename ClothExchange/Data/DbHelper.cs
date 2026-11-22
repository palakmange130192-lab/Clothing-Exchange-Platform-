using Microsoft.Data.SqlClient;

namespace ClothExchange.Data
{
    // Gives every controller a SqlConnection. It is registered in Program.cs
    // and arrives in the controller constructor (Dependency Injection).
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
