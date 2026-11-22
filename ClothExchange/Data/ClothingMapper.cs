using System.Data;
using ClothExchange.Models;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Data
{
    // Converts database rows into Clothing objects.
    // FromReader is used with SqlDataReader (connected),
    // FromRow is used with DataSet/DataTable (disconnected).
    public static class ClothingMapper
    {
        public const string SelectSql =
            "SELECT c.Id, c.OwnerId, c.Title, c.Description, c.Category, c.Size, c.Condition, " +
            "c.ImageUrl, c.Status, c.CreatedAt, u.FullName AS OwnerName, u.City AS OwnerCity " +
            "FROM Clothes c INNER JOIN Users u ON c.OwnerId = u.Id ";

        public static Clothing FromReader(SqlDataReader r)
        {
            Clothing c = new Clothing();
            c.Id = (int)r["Id"];
            c.OwnerId = (int)r["OwnerId"];
            c.Title = r["Title"].ToString();
            c.Description = r["Description"].ToString();
            c.Category = r["Category"].ToString();
            c.Size = r["Size"].ToString();
            c.Condition = r["Condition"].ToString();
            c.ImageUrl = r["ImageUrl"] == DBNull.Value ? null : r["ImageUrl"].ToString();
            c.Status = r["Status"].ToString();
            c.CreatedAt = (DateTime)r["CreatedAt"];
            c.OwnerName = r["OwnerName"].ToString();
            c.OwnerCity = r["OwnerCity"].ToString();
            return c;
        }

        public static Clothing FromRow(DataRow r)
        {
            Clothing c = new Clothing();
            c.Id = (int)r["Id"];
            c.OwnerId = (int)r["OwnerId"];
            c.Title = r["Title"].ToString();
            c.Description = r["Description"].ToString();
            c.Category = r["Category"].ToString();
            c.Size = r["Size"].ToString();
            c.Condition = r["Condition"].ToString();
            c.ImageUrl = r["ImageUrl"] == DBNull.Value ? null : r["ImageUrl"].ToString();
            c.Status = r["Status"].ToString();
            c.CreatedAt = (DateTime)r["CreatedAt"];
            c.OwnerName = r["OwnerName"].ToString();
            c.OwnerCity = r["OwnerCity"].ToString();
            return c;
        }
    }
}
