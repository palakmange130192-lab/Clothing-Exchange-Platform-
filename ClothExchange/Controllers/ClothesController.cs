using System.Data;
using ClothExchange.Data;
using ClothExchange.Filters;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers
{
    public class ClothesController : Controller
    {
        private readonly DbHelper _db;
        private readonly IWebHostEnvironment _env;   // gives us the path of the wwwroot folder

        public ClothesController(DbHelper db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ---------- Browse + search (DISCONNECTED: DataAdapter + DataSet) ----------
        public IActionResult Index(string search, string category)
        {
            List<Clothing> list = new List<Clothing>();

            try
            {
                string sql = ClothingMapper.SelectSql +
                    "WHERE c.Status = 'Available' " +
                    "AND (@Search = '' OR c.Title LIKE '%' + @Search + '%' OR c.Description LIKE '%' + @Search + '%') " +
                    "AND (@Category = '' OR c.Category = @Category) " +
                    "ORDER BY c.CreatedAt DESC";

                using (SqlConnection con = _db.GetConnection())
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@Search", search ?? "");
                    adapter.SelectCommand.Parameters.AddWithValue("@Category", category ?? "");

                    DataSet ds = new DataSet();
                    adapter.Fill(ds, "Clothes");   // opens the connection, fills the DataSet, closes it

                    foreach (DataRow row in ds.Tables["Clothes"].Rows)
                    {
                        list.Add(ClothingMapper.FromRow(row));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not load clothes: " + ex.Message;
            }

            ViewBag.Search = search;
            ViewBag.Category = category;
            return View(list);
        }

        // ---------- Details (CONNECTED: SqlDataReader) ----------
        public IActionResult Details(int id)
        {
            Clothing item;

            try
            {
                item = GetClothing(id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not load this item: " + ex.Message;
                return RedirectToAction("Index");
            }

            if (item == null)
            {
                return NotFound();
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.IsOwner = (userId == item.OwnerId);
            return View(item);
        }

        // ---------- My clothes ----------
        [LoginRequired]
        public IActionResult MyClothes()
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;
            List<Clothing> list = new List<Clothing>();

            try
            {
                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(ClothingMapper.SelectSql +
                        "WHERE c.OwnerId = @OwnerId ORDER BY c.CreatedAt DESC", con);
                    cmd.Parameters.AddWithValue("@OwnerId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ClothingMapper.FromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Could not load your clothes: " + ex.Message;
            }

            return View(list);
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
        public IActionResult Create(Clothing model, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "Please choose a photo.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId").Value;

                // save the file in wwwroot/uploads and keep its path in the database
                model.ImageUrl = SavePhoto(photo);

                string sql = "INSERT INTO Clothes (OwnerId, Title, Description, Category, Size, Condition, ImageUrl) " +
                             "VALUES (@OwnerId, @Title, @Description, @Category, @Size, @Condition, @ImageUrl)";

                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@OwnerId", userId);
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Description", model.Description);
                    cmd.Parameters.AddWithValue("@Category", model.Category);
                    cmd.Parameters.AddWithValue("@Size", model.Size);
                    cmd.Parameters.AddWithValue("@Condition", model.Condition);
                    cmd.Parameters.AddWithValue("@ImageUrl", (object)model.ImageUrl ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Your clothing was added.";
                return RedirectToAction("MyClothes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not save: " + ex.Message);
                return View(model);
            }
        }

        // ---------- Edit ----------
        [LoginRequired]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            Clothing item = GetClothing(id);
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            if (item == null || item.OwnerId != userId)
            {
                TempData["Error"] = "You can only edit your own clothes.";
                return RedirectToAction("MyClothes");
            }

            return View(item);
        }

        [LoginRequired]
        [HttpPost]
        public IActionResult Edit(Clothing model, IFormFile photo)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId").Value;

                // only if a new photo was chosen: save it, remember the old one to delete
                string oldPhoto = null;
                if (photo != null && photo.Length > 0)
                {
                    oldPhoto = model.ImageUrl;
                    model.ImageUrl = SavePhoto(photo);
                }

                // "AND OwnerId = @OwnerId" makes sure nobody edits someone else's item
                string sql = "UPDATE Clothes SET Title = @Title, Description = @Description, Category = @Category, " +
                             "Size = @Size, Condition = @Condition, ImageUrl = @ImageUrl " +
                             "WHERE Id = @Id AND OwnerId = @OwnerId";

                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@OwnerId", userId);
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Description", model.Description);
                    cmd.Parameters.AddWithValue("@Category", model.Category);
                    cmd.Parameters.AddWithValue("@Size", model.Size);
                    cmd.Parameters.AddWithValue("@Condition", model.Condition);
                    cmd.Parameters.AddWithValue("@ImageUrl", (object)model.ImageUrl ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                DeletePhoto(oldPhoto);

                TempData["Success"] = "Clothing updated.";
                return RedirectToAction("MyClothes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not update: " + ex.Message);
                return View(model);
            }
        }

        // ---------- Delete (uses a transaction: both deletes happen or none) ----------
        [LoginRequired]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            Clothing item = GetClothing(id);   // we need its photo path
            string photoToDelete = null;

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlTransaction tran = con.BeginTransaction();

                try
                {
                    // first remove requests that use this cloth
                    SqlCommand deleteRequests = new SqlCommand(
                        "DELETE FROM ExchangeRequests WHERE RequestedClothId = @Id OR OfferedClothId = @Id", con, tran);
                    deleteRequests.Parameters.AddWithValue("@Id", id);
                    deleteRequests.ExecuteNonQuery();

                    // then remove the cloth itself
                    SqlCommand deleteCloth = new SqlCommand(
                        "DELETE FROM Clothes WHERE Id = @Id AND OwnerId = @OwnerId", con, tran);
                    deleteCloth.Parameters.AddWithValue("@Id", id);
                    deleteCloth.Parameters.AddWithValue("@OwnerId", userId);
                    deleteCloth.ExecuteNonQuery();

                    tran.Commit();
                    TempData["Success"] = "Clothing deleted.";

                    if (item != null && item.OwnerId == userId)
                    {
                        photoToDelete = item.ImageUrl;
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["Error"] = "Could not delete: " + ex.Message;
                }
            }

            DeletePhoto(photoToDelete);
            return RedirectToAction("MyClothes");
        }

        // ---------- helper: save an uploaded photo, returns its path ----------
        private string SavePhoto(IFormFile photo)
        {
            string[] allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            string extension = Path.GetExtension(photo.FileName).ToLower();

            if (!allowed.Contains(extension))
            {
                throw new Exception("Only JPG, PNG, WEBP or GIF photos are allowed.");
            }

            if (photo.Length > 5 * 1024 * 1024)
            {
                throw new Exception("The photo must be smaller than 5 MB.");
            }

            string folder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);

            // random file name so two users can upload "photo.jpg" without clashing
            string fileName = Guid.NewGuid().ToString() + extension;

            using (FileStream stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return "/uploads/" + fileName;
        }

        // ---------- helper: delete a photo file we saved earlier ----------
        private void DeletePhoto(string photoPath)
        {
            // only files in our uploads folder, never sample links like https://...
            if (string.IsNullOrEmpty(photoPath) || !photoPath.StartsWith("/uploads/"))
            {
                return;
            }

            string fullPath = Path.Combine(_env.WebRootPath, "uploads", Path.GetFileName(photoPath));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // ---------- helper: get one clothing by id ----------
        private Clothing GetClothing(int id)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(ClothingMapper.SelectSql + "WHERE c.Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ClothingMapper.FromReader(reader);
                    }
                }
            }

            return null;
        }
    }
}
