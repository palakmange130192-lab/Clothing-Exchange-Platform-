using ClothExchange.Data;
using ClothExchange.Filters;
using ClothExchange.Models;
using ClothExchange.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers
{
    [LoginRequired]   // every action in this controller needs a logged-in user
    public class ExchangeController : Controller
    {
        private readonly DbHelper _db;

        public ExchangeController(DbHelper db)
        {
            _db = db;
        }

        // ---------- Show the "request exchange" page ----------
        // id = the cloth the user WANTS
        [HttpGet]
        public IActionResult Create(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;
            RequestViewModel model = new RequestViewModel();

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                // 1. the wanted cloth
                SqlCommand cmd = new SqlCommand(ClothingMapper.SelectSql + "WHERE c.Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Wanted = ClothingMapper.FromReader(reader);
                    }
                }

                if (model.Wanted == null || model.Wanted.OwnerId == userId || model.Wanted.Status != "Available")
                {
                    TempData["Error"] = "You cannot request this item.";
                    return RedirectToAction("Index", "Clothes");
                }

                // 2. the user's own available clothes (what they can offer)
                SqlCommand cmd2 = new SqlCommand(ClothingMapper.SelectSql +
                    "WHERE c.OwnerId = @OwnerId AND c.Status = 'Available'", con);
                cmd2.Parameters.AddWithValue("@OwnerId", userId);
                using (SqlDataReader reader2 = cmd2.ExecuteReader())
                {
                    while (reader2.Read())
                    {
                        model.MyClothes.Add(ClothingMapper.FromReader(reader2));
                    }
                }
            }

            return View(model);
        }

        // ---------- Save the request ----------
        [HttpPost]
        public IActionResult Create(int id, int offeredClothId, string message)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            try
            {
                using (SqlConnection con = _db.GetConnection())
                {
                    con.Open();

                    // the offered cloth must belong to this user and be available
                    SqlCommand check = new SqlCommand(
                        "SELECT COUNT(*) FROM Clothes WHERE Id = @Id AND OwnerId = @OwnerId AND Status = 'Available'", con);
                    check.Parameters.AddWithValue("@Id", offeredClothId);
                    check.Parameters.AddWithValue("@OwnerId", userId);

                    if ((int)check.ExecuteScalar() == 0)
                    {
                        TempData["Error"] = "Please choose one of your available clothes.";
                        return RedirectToAction("Create", new { id = id });
                    }

                    // the wanted cloth must still be available and not the user's own
                    SqlCommand checkWanted = new SqlCommand(
                        "SELECT COUNT(*) FROM Clothes WHERE Id = @Id AND OwnerId <> @UserId AND Status = 'Available'", con);
                    checkWanted.Parameters.AddWithValue("@Id", id);
                    checkWanted.Parameters.AddWithValue("@UserId", userId);

                    if ((int)checkWanted.ExecuteScalar() == 0)
                    {
                        TempData["Error"] = "This item is no longer available.";
                        return RedirectToAction("Index", "Clothes");
                    }

                    // do not allow the same pending request twice
                    SqlCommand checkDuplicate = new SqlCommand(
                        "SELECT COUNT(*) FROM ExchangeRequests WHERE RequesterId = @UserId " +
                        "AND RequestedClothId = @Wanted AND OfferedClothId = @Offered AND Status = 'Pending'", con);
                    checkDuplicate.Parameters.AddWithValue("@UserId", userId);
                    checkDuplicate.Parameters.AddWithValue("@Wanted", id);
                    checkDuplicate.Parameters.AddWithValue("@Offered", offeredClothId);

                    if ((int)checkDuplicate.ExecuteScalar() > 0)
                    {
                        TempData["Error"] = "You already sent this request.";
                        return RedirectToAction("MyRequests");
                    }

                    string sql = "INSERT INTO ExchangeRequests (RequesterId, RequestedClothId, OfferedClothId, Message) " +
                                 "VALUES (@RequesterId, @RequestedClothId, @OfferedClothId, @Message)";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@RequesterId", userId);
                    cmd.Parameters.AddWithValue("@RequestedClothId", id);
                    cmd.Parameters.AddWithValue("@OfferedClothId", offeredClothId);
                    cmd.Parameters.AddWithValue("@Message", (object)message ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Exchange request sent.";
                return RedirectToAction("MyRequests");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not send request: " + ex.Message;
                return RedirectToAction("Index", "Clothes");
            }
        }

        // ---------- Sent + received requests ----------
        public IActionResult MyRequests()
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;
            MyRequestsViewModel model = new MyRequestsViewModel();

            try
            {
                model.Sent = LoadRequests("r.RequesterId = @UserId", userId);      // I asked
                model.Received = LoadRequests("rc.OwnerId = @UserId", userId);     // someone asked me
            }
            catch (Exception ex)
            {
                // show the real error on the page instead of a blank/hanging page
                ViewBag.Error = "Could not load requests: " + ex.Message;
            }

            return View(model);
        }

        // ---------- Accept (transaction: 3 updates must all succeed) ----------
        [HttpPost]
        public IActionResult Accept(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlTransaction tran = con.BeginTransaction();

                try
                {
                    // find the request, but only if it is pending and the wanted cloth is mine
                    string findSql = "SELECT r.RequestedClothId, r.OfferedClothId FROM ExchangeRequests r " +
                                     "INNER JOIN Clothes c ON r.RequestedClothId = c.Id " +
                                     "WHERE r.Id = @Id AND c.OwnerId = @UserId AND r.Status = 'Pending'";
                    SqlCommand find = new SqlCommand(findSql, con, tran);
                    find.Parameters.AddWithValue("@Id", id);
                    find.Parameters.AddWithValue("@UserId", userId);

                    bool found = false;
                    int requestedId = 0;
                    int offeredId = 0;

                    using (SqlDataReader reader = find.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            found = true;
                            requestedId = reader.GetInt32(0);
                            offeredId = reader.GetInt32(1);
                        }
                    }

                    if (!found)
                    {
                        tran.Rollback();
                        TempData["Error"] = "Request not found.";
                        return RedirectToAction("MyRequests");
                    }

                    string updateSql =
                        // 1. mark this request accepted
                        "UPDATE ExchangeRequests SET Status = 'Accepted' WHERE Id = @Id; " +
                        // 2. both clothes are now exchanged
                        "UPDATE Clothes SET Status = 'Exchanged' WHERE Id IN (@A, @B); " +
                        // 3. other pending requests that use these clothes are rejected
                        "UPDATE ExchangeRequests SET Status = 'Rejected' " +
                        "WHERE Status = 'Pending' AND Id <> @Id " +
                        "AND (RequestedClothId IN (@A, @B) OR OfferedClothId IN (@A, @B));";

                    SqlCommand update = new SqlCommand(updateSql, con, tran);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@A", requestedId);
                    update.Parameters.AddWithValue("@B", offeredId);
                    update.ExecuteNonQuery();

                    tran.Commit();
                    TempData["Success"] = "Exchange accepted. Contact each other to swap the clothes.";
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["Error"] = "Could not accept: " + ex.Message;
                }
            }

            return RedirectToAction("MyRequests");
        }

        // ---------- Reject ----------
        [HttpPost]
        public IActionResult Reject(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            string sql = "UPDATE ExchangeRequests SET Status = 'Rejected' " +
                         "WHERE Id = @Id AND Status = 'Pending' " +
                         "AND RequestedClothId IN (SELECT Id FROM Clothes WHERE OwnerId = @UserId)";

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Request rejected.";
            return RedirectToAction("MyRequests");
        }

        // ---------- Cancel (the person who sent it can withdraw it) ----------
        [HttpPost]
        public IActionResult Cancel(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM ExchangeRequests WHERE Id = @Id AND RequesterId = @UserId AND Status = 'Pending'", con);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Request cancelled.";
            return RedirectToAction("MyRequests");
        }

        // ---------- helper: load requests with a fixed WHERE condition ----------
        private List<ExchangeRequest> LoadRequests(string whereCondition, int userId)
        {
            List<ExchangeRequest> list = new List<ExchangeRequest>();

            string sql = "SELECT r.Id, r.RequesterId, r.RequestedClothId, r.OfferedClothId, r.Message, r.Status, r.CreatedAt, " +
                         "u.FullName AS RequesterName, o.FullName AS OwnerName, " +
                         "rc.Title AS RequestedTitle, oc.Title AS OfferedTitle " +
                         "FROM ExchangeRequests r " +
                         "INNER JOIN Users u ON r.RequesterId = u.Id " +
                         "INNER JOIN Clothes rc ON r.RequestedClothId = rc.Id " +
                         "INNER JOIN Users o ON rc.OwnerId = o.Id " +
                         "INNER JOIN Clothes oc ON r.OfferedClothId = oc.Id " +
                         "WHERE " + whereCondition + " ORDER BY r.CreatedAt DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ExchangeRequest r = new ExchangeRequest();
                        r.Id = (int)reader["Id"];
                        r.RequesterId = (int)reader["RequesterId"];
                        r.RequestedClothId = (int)reader["RequestedClothId"];
                        r.OfferedClothId = (int)reader["OfferedClothId"];
                        r.Message = reader["Message"] == DBNull.Value ? null : reader["Message"].ToString();
                        r.Status = reader["Status"].ToString();
                        r.CreatedAt = (DateTime)reader["CreatedAt"];
                        r.RequesterName = reader["RequesterName"].ToString();
                        r.OwnerName = reader["OwnerName"].ToString();
                        r.RequestedTitle = reader["RequestedTitle"].ToString();
                        r.OfferedTitle = reader["OfferedTitle"].ToString();
                        list.Add(r);
                    }
                }
            }

            return list;
        }
    }
}
