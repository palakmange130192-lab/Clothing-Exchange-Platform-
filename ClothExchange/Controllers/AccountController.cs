using ClothExchange.Data;
using ClothExchange.Helpers;
using ClothExchange.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbHelper _db;

        public AccountController(DbHelper db)
        {
            _db = db;
        }

        // ---------- Register ----------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();

                    // 1. is the email already used?
                    SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", con);
                    check.Parameters.AddWithValue("@Email", model.Email);
                    int count = (int)check.ExecuteScalar();

                    if (count > 0)
                    {
                        ModelState.AddModelError("Email", "This email is already registered.");
                        return View(model);
                    }

                    // 2. insert the new user
                    string sql = "INSERT INTO Users (FullName, Email, PasswordHash, City) " +
                                 "VALUES (@FullName, @Email, @PasswordHash, @City)";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hash(model.Password));
                    cmd.Parameters.AddWithValue("@City", model.City);
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Account created. Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not register: " + ex.Message);
                return View(model);
            }
        }

        // ---------- Login ----------
        [HttpGet]
        public IActionResult Login()
        {
            LoginViewModel model = new LoginViewModel();
            model.Email = Request.Cookies["LastEmail"];   // cookie remembers the last email
            return View(model);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();

                    string sql = "SELECT Id, FullName FROM Users WHERE Email = @Email AND PasswordHash = @PasswordHash";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hash(model.Password));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Session: stored on the server, lives until logout / timeout
                            HttpContext.Session.SetInt32("UserId", (int)reader["Id"]);
                            HttpContext.Session.SetString("UserName", reader["FullName"].ToString());

                            // Cookie: stored in the browser for 7 days
                            CookieOptions options = new CookieOptions();
                            options.Expires = DateTimeOffset.Now.AddDays(7);
                            Response.Cookies.Append("LastEmail", model.Email, options);

                            return RedirectToAction("Index", "Clothes");
                        }
                    }
                }

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not login: " + ex.Message);
                return View(model);
            }
        }

        // ---------- Logout ----------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
