using ClothExchange.Data;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers;

// REST API (returns JSON, no views)
//   GET    /api/clothes        -> all available clothes
//   GET    /api/clothes/3      -> one item
//   POST   /api/clothes        -> add an item          (login required)
//   PUT    /api/clothes/3      -> update your item     (login required)
//   DELETE /api/clothes/3      -> delete your item     (login required)
[ApiController]
[Route("api/clothes")]
public class ClothesApiController : ControllerBase
{
    private readonly DbHelper _db;

    public ClothesApiController(DbHelper db)
    {
        _db = db;
    }

    // Id of the logged-in user (0 if nobody) - same Session as the website
    private int Me
    {
        get { return HttpContext.Session.GetInt32("UserId") ?? 0; }
    }

    // ---------- GET all ----------
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.GetClothes("WHERE c.Status = 'Available'"));   // 200 + JSON
    }

    // ---------- GET one ----------
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var item = _db.GetClothingById(id);
        if (item == null)
        {
            return NotFound();   // 404
        }
        return Ok(item);
    }

    // ---------- POST (create) ----------
    // [ApiController] automatically returns 400 if the [Required] rules in Clothing are broken
    [HttpPost]
    public IActionResult Create([FromBody] Clothing model)
    {
        if (Me == 0)
        {
            return Unauthorized();   // 401
        }

        int newId = _db.InsertAndGetId(
            "INSERT INTO Clothes (Title, Description, Size, Category, Condition, ImageUrl, OwnerId) " +
            "OUTPUT INSERTED.Id " +
            "VALUES (@t, @d, @s, @c, @cond, @img, @owner)",
            new SqlParameter("@t", model.Title),
            new SqlParameter("@d", model.Description ?? ""),
            new SqlParameter("@s", model.Size),
            new SqlParameter("@c", model.Category),
            new SqlParameter("@cond", model.Condition),
            new SqlParameter("@img", model.ImageUrl ?? ""),
            new SqlParameter("@owner", Me));

        return CreatedAtAction(nameof(GetById), new { id = newId }, _db.GetClothingById(newId));   // 201
    }

    // ---------- PUT (update) ----------
    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] Clothing model)
    {
        if (Me == 0)
        {
            return Unauthorized();   // 401
        }

        var existing = _db.GetClothingById(id);
        if (existing == null)
        {
            return NotFound();   // 404
        }
        if (existing.OwnerId != Me)
        {
            return Forbid();   // 403: not your item
        }

        _db.Execute(
            "UPDATE Clothes SET Title=@t, Description=@d, Size=@s, Category=@c, Condition=@cond, ImageUrl=@img " +
            "WHERE Id=@id AND OwnerId=@owner",
            new SqlParameter("@t", model.Title),
            new SqlParameter("@d", model.Description ?? ""),
            new SqlParameter("@s", model.Size),
            new SqlParameter("@c", model.Category),
            new SqlParameter("@cond", model.Condition),
            new SqlParameter("@img", model.ImageUrl ?? ""),
            new SqlParameter("@id", id),
            new SqlParameter("@owner", Me));

        return Ok(_db.GetClothingById(id));
    }

    // ---------- DELETE ----------
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        if (Me == 0)
        {
            return Unauthorized();   // 401
        }

        var existing = _db.GetClothingById(id);
        if (existing == null)
        {
            return NotFound();   // 404
        }
        if (existing.OwnerId != Me)
        {
            return Forbid();   // 403
        }

        _db.Execute("DELETE FROM ExchangeRequests WHERE RequestedItemId=@id OR OfferedItemId=@id",
            new SqlParameter("@id", id));
        _db.Execute("DELETE FROM Clothes WHERE Id=@id AND OwnerId=@owner",
            new SqlParameter("@id", id),
            new SqlParameter("@owner", Me));

        return NoContent();   // 204
    }
}
