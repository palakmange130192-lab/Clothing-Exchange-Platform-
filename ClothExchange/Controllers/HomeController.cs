using ClothExchange.Data;
using ClothExchange.Models;
using ClothExchange.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbHelper _db;

        public HomeController(DbHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();

            try
            {
                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();

                    // 3 simple numbers for the stats bar
                    model.TotalUsers = (int)new SqlCommand("SELECT COUNT(*) FROM Users", con).ExecuteScalar();
                    model.TotalClothes = (int)new SqlCommand("SELECT COUNT(*) FROM Clothes", con).ExecuteScalar();
                    model.TotalExchanges = (int)new SqlCommand("SELECT COUNT(*) FROM ExchangeRequests WHERE Status = 'Accepted'", con).ExecuteScalar();

                    // latest 4 available clothes
                    string sql = ClothingMapper.SelectSql.Replace("SELECT", "SELECT TOP 4") +
                                 "WHERE c.Status = 'Available' ORDER BY c.CreatedAt DESC";
                    SqlCommand cmd = new SqlCommand(sql, con);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Latest.Add(ClothingMapper.FromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Could not connect to the database: " + ex.Message;
            }

            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
