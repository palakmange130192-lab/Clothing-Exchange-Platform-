using System.Data;
using System.Security.Cryptography;
using System.Text;
using ClothExchange.Models;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Data;

// Data-access class. All ADO.NET code (SqlConnection / SqlCommand / SqlDataReader) lives here or in controllers.
public class DbHelper
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;
    private readonly string _dbName;

    // The connection string comes from appsettings.json (injected by DI)
    public DbHelper(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;

        var builder = new SqlConnectionStringBuilder(_connectionString);
        _dbName = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        _masterConnectionString = builder.ConnectionString;
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    // Creates the database + tables if they don't exist yet
    public void EnsureCreated()
    {
        using (var con = new SqlConnection(_masterConnectionString))
        {
            con.Open();
            var cmd = new SqlCommand("IF DB_ID('" + _dbName + "') IS NULL CREATE DATABASE [" + _dbName + "]", con);
            cmd.ExecuteNonQuery();
        }

        const string tablesSql = @"
IF OBJECT_ID('Users', 'U') IS NULL
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    City NVARCHAR(100) NOT NULL DEFAULT ''
);

IF OBJECT_ID('Clothes', 'U') IS NULL
CREATE TABLE Clothes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL DEFAULT '',
    Size NVARCHAR(20) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Condition NVARCHAR(20) NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL DEFAULT '',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Available',
    OwnerId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

IF OBJECT_ID('ExchangeRequests', 'U') IS NULL
CREATE TABLE ExchangeRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestedItemId INT NOT NULL FOREIGN KEY REFERENCES Clothes(Id),
    OfferedItemId INT NOT NULL FOREIGN KEY REFERENCES Clothes(Id),
    RequesterId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Message NVARCHAR(300) NOT NULL DEFAULT '',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);";

        using (var con = GetConnection())
        {
            con.Open();
            var cmd = new SqlCommand(tablesSql, con);
            cmd.ExecuteNonQuery();
        }
    }

    // ---------- small reusable helpers ----------

    // INSERT / UPDATE / DELETE
    public int Execute(string sql, params SqlParameter[] parameters)
    {
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }

    // INSERT ... OUTPUT INSERTED.Id  -> returns the new row's Id (used by the REST API)
    public int InsertAndGetId(string sql, params SqlParameter[] parameters)
    {
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddRange(parameters);
        return (int)cmd.ExecuteScalar()!;
    }

    // SELECT COUNT(*) ...
    public int Count(string sql, params SqlParameter[] parameters)
    {
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddRange(parameters);
        return (int)cmd.ExecuteScalar()!;
    }

    // Passwords are stored as a SHA256 hash, never as plain text
    public static string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    // ---------- Clothes queries ----------

    private const string ClothesSelect =
        "SELECT c.Id, c.Title, c.Description, c.Size, c.Category, c.Condition, c.ImageUrl, c.Status, " +
        "c.OwnerId, c.CreatedAt, u.FullName AS OwnerName " +
        "FROM Clothes c JOIN Users u ON c.OwnerId = u.Id ";

    // 'where' is something like "WHERE c.Status = 'Available'"
    public List<Clothing> GetClothes(string where = "", params SqlParameter[] parameters)
    {
        var list = new List<Clothing>();
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqlCommand(ClothesSelect + where + " ORDER BY c.CreatedAt DESC", con);
        cmd.Parameters.AddRange(parameters);
        using SqlDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Clothing
            {
                Id = (int)r["Id"],
                Title = r["Title"].ToString()!,
                Description = r["Description"].ToString(),
                Size = r["Size"].ToString()!,
                Category = r["Category"].ToString()!,
                Condition = r["Condition"].ToString()!,
                ImageUrl = r["ImageUrl"].ToString(),
                Status = r["Status"].ToString()!,
                OwnerId = (int)r["OwnerId"],
                OwnerName = r["OwnerName"].ToString()!,
                CreatedAt = (DateTime)r["CreatedAt"]
            });
        }
        return list;
    }

    public Clothing? GetClothingById(int id)
    {
        return GetClothes("WHERE c.Id = @id", new SqlParameter("@id", id)).FirstOrDefault();
    }

    // ---------- DISCONNECTED architecture: SqlDataAdapter + DataSet ----------
    // The adapter opens the connection, fills the DataSet, and closes the connection again.
    // After that we work with the data in memory (the DataSet) without any open connection.
    public List<Clothing> SearchClothes(string? search, string? category)
    {
        string sql = ClothesSelect +
            "WHERE c.Status = 'Available' " +
            "AND (@s = '' OR c.Title LIKE '%' + @s + '%' OR c.Description LIKE '%' + @s + '%') " +
            "AND (@cat = '' OR c.Category = @cat) " +
            "ORDER BY c.CreatedAt DESC";

        using var con = GetConnection();
        var adapter = new SqlDataAdapter(sql, con);
        adapter.SelectCommand.Parameters.AddWithValue("@s", search ?? "");
        adapter.SelectCommand.Parameters.AddWithValue("@cat", category ?? "");

        var ds = new DataSet();
        adapter.Fill(ds, "Clothes");          // DataSet -> DataTable "Clothes" -> DataRows

        var list = new List<Clothing>();
        foreach (DataRow row in ds.Tables["Clothes"]!.Rows)
        {
            list.Add(new Clothing
            {
                Id = (int)row["Id"],
                Title = row["Title"].ToString()!,
                Description = row["Description"].ToString(),
                Size = row["Size"].ToString()!,
                Category = row["Category"].ToString()!,
                Condition = row["Condition"].ToString()!,
                ImageUrl = row["ImageUrl"].ToString(),
                Status = row["Status"].ToString()!,
                OwnerId = (int)row["OwnerId"],
                OwnerName = row["OwnerName"].ToString()!,
                CreatedAt = (DateTime)row["CreatedAt"]
            });
        }
        return list;
    }

    // ---------- Exchange request queries ----------

    private const string RequestsSelect =
        "SELECT r.Id, r.RequestedItemId, r.OfferedItemId, r.RequesterId, r.Message, r.Status, r.CreatedAt, " +
        "rc.Title AS RequestedTitle, oc.Title AS OfferedTitle, ru.FullName AS RequesterName, ou.FullName AS OwnerName " +
        "FROM ExchangeRequests r " +
        "JOIN Clothes rc ON r.RequestedItemId = rc.Id " +
        "JOIN Clothes oc ON r.OfferedItemId = oc.Id " +
        "JOIN Users ru ON r.RequesterId = ru.Id " +
        "JOIN Users ou ON rc.OwnerId = ou.Id ";

    public List<ExchangeRequest> GetRequests(string where, params SqlParameter[] parameters)
    {
        var list = new List<ExchangeRequest>();
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqlCommand(RequestsSelect + where + " ORDER BY r.CreatedAt DESC", con);
        cmd.Parameters.AddRange(parameters);
        using SqlDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ExchangeRequest
            {
                Id = (int)r["Id"],
                RequestedItemId = (int)r["RequestedItemId"],
                OfferedItemId = (int)r["OfferedItemId"],
                RequesterId = (int)r["RequesterId"],
                Message = r["Message"].ToString()!,
                Status = r["Status"].ToString()!,
                CreatedAt = (DateTime)r["CreatedAt"],
                RequestedTitle = r["RequestedTitle"].ToString()!,
                OfferedTitle = r["OfferedTitle"].ToString()!,
                RequesterName = r["RequesterName"].ToString()!,
                OwnerName = r["OwnerName"].ToString()!
            });
        }
        return list;
    }
}
