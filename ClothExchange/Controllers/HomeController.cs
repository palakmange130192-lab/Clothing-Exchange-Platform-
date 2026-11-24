using ClothExchange.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClothExchange.Controllers;

public class HomeController : Controller
{
    private readonly DbHelper _db;

    public HomeController(DbHelper db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        // Show the 4 newest available items on the home page
        var latest = _db.GetClothes("WHERE c.Status = 'Available'").Take(4).ToList();
        return View(latest);
    }
}
