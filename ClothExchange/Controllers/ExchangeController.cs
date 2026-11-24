using ClothExchange.Data;
using ClothExchange.Filters;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers;

[LoginRequired]   // every action in this controller needs a logged-in user
public class ExchangeController : Controller
{
    private readonly DbHelper _db;

    public ExchangeController(DbHelper db)
    {
        _db = db;
    }

    private int Me
    {
        get { return HttpContext.Session.GetInt32("UserId") ?? 0; }
    }

    // ---------- Step 1: show the form (pick which of MY items to offer) ----------
    [HttpGet]
    public IActionResult Propose(int id)
    {
        var target = _db.GetClothingById(id);
        if (target == null || target.OwnerId == Me || target.Status != "Available")
        {
            TempData["Error"] = "You cannot request this item.";
            return RedirectToAction("Index", "Clothes");
        }

        var vm = new ExchangeRequestViewModel
        {
            Target = target,
            MyItems = _db.GetClothes("WHERE c.OwnerId = @me AND c.Status = 'Available'",
                new SqlParameter("@me", Me))
        };
        return View(vm);
    }

    // ---------- Step 2: save the request ----------
    [HttpPost]
    public IActionResult Propose(int requestedItemId, int offeredItemId, string? message)
    {
        var target = _db.GetClothingById(requestedItemId);
        var mine = _db.GetClothingById(offeredItemId);

        bool valid = target != null && mine != null
                     && target.OwnerId != Me && mine.OwnerId == Me
                     && target.Status == "Available" && mine.Status == "Available";
        if (!valid)
        {
            TempData["Error"] = "Invalid exchange request.";
            return RedirectToAction("Index", "Clothes");
        }

        try
        {
            _db.Execute(
                "INSERT INTO ExchangeRequests (RequestedItemId, OfferedItemId, RequesterId, Message) " +
                "VALUES (@req, @off, @me, @msg)",
                new SqlParameter("@req", requestedItemId),
                new SqlParameter("@off", offeredItemId),
                new SqlParameter("@me", Me),
                new SqlParameter("@msg", message ?? ""));

            TempData["Msg"] = "Exchange request sent!";
            return RedirectToAction("Sent");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not send the request: " + ex.Message;
            return RedirectToAction("Index", "Clothes");
        }
    }

    // ---------- Requests other people sent for MY items ----------
    public IActionResult Incoming()
    {
        var list = _db.GetRequests("WHERE rc.OwnerId = @me", new SqlParameter("@me", Me));
        return View(list);
    }

    // ---------- Requests I sent ----------
    public IActionResult Sent()
    {
        var list = _db.GetRequests("WHERE r.RequesterId = @me", new SqlParameter("@me", Me));
        return View(list);
    }

    // ---------- Accept: swap happens here ----------
    [HttpPost]
    public IActionResult Accept(int id)
    {
        using var con = _db.GetConnection();
        con.Open();
        using var tx = con.BeginTransaction();   // all steps succeed together or none do

        try
        {
            int requestedId = 0, offeredId = 0;

            // 1. Find the request (must be Pending and for one of MY items)
            using (var cmd = new SqlCommand(
                "SELECT r.RequestedItemId, r.OfferedItemId FROM ExchangeRequests r " +
                "JOIN Clothes c ON c.Id = r.RequestedItemId " +
                "WHERE r.Id = @id AND r.Status = 'Pending' AND c.OwnerId = @me", con, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@me", Me);
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    requestedId = reader.GetInt32(0);
                    offeredId = reader.GetInt32(1);
                }
            }

            if (requestedId == 0)
            {
                tx.Rollback();
                TempData["Error"] = "Request not found.";
                return RedirectToAction("Incoming");
            }

            // 2. Both items must still be Available
            using (var check = new SqlCommand(
                "SELECT COUNT(*) FROM Clothes WHERE Id IN (@a, @b) AND Status = 'Available'", con, tx))
            {
                check.Parameters.AddWithValue("@a", requestedId);
                check.Parameters.AddWithValue("@b", offeredId);
                if ((int)check.ExecuteScalar()! != 2)
                {
                    tx.Rollback();
                    TempData["Error"] = "One of the items is no longer available.";
                    return RedirectToAction("Incoming");
                }
            }

            // 3. Mark request Accepted, both items Exchanged, reject all other pending requests for those items
            Run(con, tx, "UPDATE ExchangeRequests SET Status = 'Accepted' WHERE Id = @id",
                new SqlParameter("@id", id));
            Run(con, tx, "UPDATE Clothes SET Status = 'Exchanged' WHERE Id IN (@a, @b)",
                new SqlParameter("@a", requestedId), new SqlParameter("@b", offeredId));
            Run(con, tx,
                "UPDATE ExchangeRequests SET Status = 'Rejected' " +
                "WHERE Status = 'Pending' AND Id <> @id " +
                "AND (RequestedItemId IN (@a, @b) OR OfferedItemId IN (@a, @b))",
                new SqlParameter("@id", id), new SqlParameter("@a", requestedId), new SqlParameter("@b", offeredId));

            tx.Commit();
            TempData["Msg"] = "Exchange accepted! Both items are now marked as Exchanged.";
        }
        catch (Exception ex)
        {
            tx.Rollback();   // something failed: undo everything
            TempData["Error"] = "Could not accept the request: " + ex.Message;
        }

        return RedirectToAction("Incoming");
    }

    // ---------- Reject ----------
    [HttpPost]
    public IActionResult Reject(int id)
    {
        _db.Execute(
            "UPDATE ExchangeRequests SET Status = 'Rejected' " +
            "WHERE Id = @id AND Status = 'Pending' " +
            "AND RequestedItemId IN (SELECT Id FROM Clothes WHERE OwnerId = @me)",
            new SqlParameter("@id", id),
            new SqlParameter("@me", Me));
        TempData["Msg"] = "Request rejected.";
        return RedirectToAction("Incoming");
    }

    // ---------- Cancel (by the person who sent it) ----------
    [HttpPost]
    public IActionResult Cancel(int id)
    {
        _db.Execute("DELETE FROM ExchangeRequests WHERE Id = @id AND RequesterId = @me AND Status = 'Pending'",
            new SqlParameter("@id", id),
            new SqlParameter("@me", Me));
        TempData["Msg"] = "Request cancelled.";
        return RedirectToAction("Sent");
    }

    // helper used inside the transaction
    private static void Run(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] ps)
    {
        using var cmd = new SqlCommand(sql, con, tx);
        cmd.Parameters.AddRange(ps);
        cmd.ExecuteNonQuery();
    }
}
