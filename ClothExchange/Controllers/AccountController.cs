using ClothExchange.Data;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers;

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
            // Is the email already used?
            int exists = _db.Count("SELECT COUNT(*) FROM Users WHERE Email = @e",
                new SqlParameter("@e", model.Email));
            if (exists > 0)
            {
                ModelState.AddModelError("Email", "This email is already registered");
                return View(model);
            }

            _db.Execute("INSERT INTO Users (FullName, Email, PasswordHash, City) VALUES (@n, @e, @p, @c)",
                new SqlParameter("@n", model.FullName),
                new SqlParameter("@e", model.Email),
                new SqlParameter("@p", DbHelper.HashPassword(model.Password)),
                new SqlParameter("@c", model.City ?? ""));

            TempData["Msg"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            // Exception handling: show the problem on the page instead of crashing
            ModelState.AddModelError("", "Could not register: " + ex.Message);
            return View(model);
        }
    }

    // ---------- Login ----------
    [HttpGet]
    public IActionResult Login()
    {
        // Cookie: the browser remembers the last email used (see below)
        var model = new LoginViewModel();
        model.Email = Request.Cookies["LastEmail"] ?? "";
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
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT Id, FullName FROM Users WHERE Email = @e AND PasswordHash = @p", con);
            cmd.Parameters.AddWithValue("@e", model.Email);
            cmd.Parameters.AddWithValue("@p", DbHelper.HashPassword(model.Password));

            using SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // Session: stored on the SERVER, remembers who is logged in
                HttpContext.Session.SetInt32("UserId", reader.GetInt32(0));
                HttpContext.Session.SetString("UserName", reader.GetString(1));

                // Cookie: stored in the BROWSER for 7 days, remembers the last email
                var options = new CookieOptions();
                options.Expires = DateTimeOffset.Now.AddDays(7);
                Response.Cookies.Append("LastEmail", model.Email, options);

                return RedirectToAction("Index", "Clothes");
            }

            ModelState.AddModelError("", "Invalid email or password");
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
