using ClothExchange.Data;
using ClothExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ClothExchange.Controllers
{
    // REST API (returns JSON, no views)
    //   GET /api/clothes       -> all available clothes
    //   GET /api/clothes/3     -> one clothing by id
    [ApiController]
    [Route("api/clothes")]
    public class ClothesApiController : ControllerBase
    {
        private readonly DbHelper _db;

        public ClothesApiController(DbHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Clothing> list = new List<Clothing>();

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(ClothingMapper.SelectSql +
                    "WHERE c.Status = 'Available' ORDER BY c.CreatedAt DESC", con);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ClothingMapper.FromReader(reader));
                    }
                }
            }

            return Ok(list);   // 200 + JSON
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
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
                        return Ok(ClothingMapper.FromReader(reader));
                    }
                }
            }

            return NotFound();   // 404
        }
    }
}
