using ClothExchange.Data;
using ClothExchange.Filters;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers;

public class ClothesController : Controller
{
    private readonly DbHelper _db;
    private readonly IWebHostEnvironment _env;

    public ClothesController(DbHelper db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Id of the logged-in user (0 if nobody)
    private int Me
    {
        get { return HttpContext.Session.GetInt32("UserId") ?? 0; }
    }

    // ---------- Browse + Search (public) ----------
    // Uses the DISCONNECTED approach: SqlDataAdapter + DataSet (see DbHelper.SearchClothes)
    public IActionResult Index(string? search, string? category)
    {
        List<Clothing> items = new List<Clothing>();

        try
        {
            items = _db.SearchClothes(search, category);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load clothes: " + ex.Message;
        }

        ViewBag.Search = search;
        ViewBag.Category = category;
        return View(items);
    }

    // ---------- Details (public) ----------
    public IActionResult Details(int id)
    {
        var item = _db.GetClothingById(id);
        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }

    // ---------- My items ----------
    [LoginRequired]
    public IActionResult MyItems()
    {
        var items = _db.GetClothes("WHERE c.OwnerId = @me", new SqlParameter("@me", Me));
        return View(items);
    }

    // ---------- Create ----------
    [LoginRequired]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Clothing());
    }

    [LoginRequired]
    [HttpPost]
    public IActionResult Create(Clothing model, IFormFile? photo)
    {
        // a photo is required when adding a new item
        if (photo == null || photo.Length == 0)
        {
            ModelState.AddModelError("ImageUrl", "Please choose a photo");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            model.ImageUrl = SavePhoto(photo!);   // save the file, keep its path

            _db.Execute(
                "INSERT INTO Clothes (Title, Description, Size, Category, Condition, ImageUrl, OwnerId) " +
                "VALUES (@t, @d, @s, @c, @cond, @img, @owner)",
                new SqlParameter("@t", model.Title),
                new SqlParameter("@d", model.Description ?? ""),
                new SqlParameter("@s", model.Size),
                new SqlParameter("@c", model.Category),
                new SqlParameter("@cond", model.Condition),
                new SqlParameter("@img", model.ImageUrl ?? ""),
                new SqlParameter("@owner", Me));

            TempData["Msg"] = "Item added successfully!";
            return RedirectToAction("MyItems");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Could not save the item: " + ex.Message);
            return View(model);
        }
    }

    // ---------- Edit ----------
    [LoginRequired]
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var item = _db.GetClothingById(id);
        if (item == null || item.OwnerId != Me)
        {
            return RedirectToAction("MyItems");
        }
        return View(item);
    }

    [LoginRequired]
    [HttpPost]
    public IActionResult Edit(Clothing model, IFormFile? photo)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // if a new photo was chosen, save it (otherwise the old path stays in model.ImageUrl)
            if (photo != null && photo.Length > 0)
            {
                model.ImageUrl = SavePhoto(photo);
            }

            // "AND OwnerId = @owner" makes sure users can only edit their own items
            _db.Execute(
                "UPDATE Clothes SET Title=@t, Description=@d, Size=@s, Category=@c, Condition=@cond, ImageUrl=@img " +
                "WHERE Id=@id AND OwnerId=@owner",
                new SqlParameter("@t", model.Title),
                new SqlParameter("@d", model.Description ?? ""),
                new SqlParameter("@s", model.Size),
                new SqlParameter("@c", model.Category),
                new SqlParameter("@cond", model.Condition),
                new SqlParameter("@img", model.ImageUrl ?? ""),
                new SqlParameter("@id", model.Id),
                new SqlParameter("@owner", Me));

            TempData["Msg"] = "Item updated successfully!";
            return RedirectToAction("MyItems");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Could not update the item: " + ex.Message);
            return View(model);
        }
    }

    // ---------- Delete ----------
    [LoginRequired]
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            var item = _db.GetClothingById(id);
            if (item != null && item.OwnerId == Me)
            {
                // First remove exchange requests that use this item, then the item itself
                _db.Execute("DELETE FROM ExchangeRequests WHERE RequestedItemId=@id OR OfferedItemId=@id",
                    new SqlParameter("@id", id));
                _db.Execute("DELETE FROM Clothes WHERE Id=@id AND OwnerId=@owner",
                    new SqlParameter("@id", id),
                    new SqlParameter("@owner", Me));
                TempData["Msg"] = "Item deleted.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not delete the item: " + ex.Message;
        }
        return RedirectToAction("MyItems");
    }

    // ---------- Photo upload helper ----------
    // Saves the uploaded photo in wwwroot/uploads and returns its path
    private string SavePhoto(IFormFile photo)
    {
        string extension = Path.GetExtension(photo.FileName).ToLower();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
        {
            throw new Exception("Only JPG, PNG or WEBP photos are allowed.");
        }

        string folder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);

        string fileName = Guid.NewGuid().ToString() + extension;   // random name
        using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
        {
            photo.CopyTo(stream);
        }
        return "/uploads/" + fileName;
    }
}
