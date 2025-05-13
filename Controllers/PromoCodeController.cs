using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace GyanSagarNew.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PromoCodeController : Controller
    {
        private readonly string _connectionString;

        public PromoCodeController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }


        // Create
        [HttpPost("create")]
        public IActionResult Create([FromBody] PromoCode code)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            var cmd = new MySqlCommand(@"INSERT INTO PromoCode (Code, DiscountPercentage, IsActive, ExpiryDate)
                                     VALUES (@Code, @DiscountPercentage, @IsActive, @ExpiryDate)", conn);
            cmd.Parameters.AddWithValue("@Code", code.Code);
            cmd.Parameters.AddWithValue("@DiscountPercentage", code.DiscountPercentage);
            cmd.Parameters.AddWithValue("@IsActive", code.IsActive);
            cmd.Parameters.AddWithValue("@ExpiryDate", code.ExpiryDate);
            cmd.ExecuteNonQuery();

            return Ok(new { Message = "Promo code created." });
        }

        // Read all
        [HttpGet]
        public IActionResult GetAll()
        {
            var promoCodes = new List<PromoCode>();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            var cmd = new MySqlCommand("SELECT * FROM PromoCode", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                promoCodes.Add(new PromoCode
                {
                    PromoCodeID = Convert.ToInt32(reader["PromoCodeID"]),
                    Code = reader["Code"].ToString(),
                    DiscountPercentage = Convert.ToDecimal(reader["DiscountPercentage"]),
                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"])
                });
            }

            return Ok(promoCodes);
        }

        // Read by ID
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            var cmd = new MySqlCommand("SELECT * FROM PromoCode WHERE PromoCodeID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var promo = new PromoCode
                {
                    PromoCodeID = Convert.ToInt32(reader["PromoCodeID"]),
                    Code = reader["Code"].ToString(),
                    DiscountPercentage = Convert.ToDecimal(reader["DiscountPercentage"]),
                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"])
                };
                return Ok(promo);
            }

            return NotFound();
        }

        // Update
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] PromoCode code)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            var cmd = new MySqlCommand(@"UPDATE PromoCode 
                                     SET Code = @Code, DiscountPercentage = @DiscountPercentage, 
                                         IsActive = @IsActive, ExpiryDate = @ExpiryDate 
                                     WHERE PromoCodeID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@Code", code.Code);
            cmd.Parameters.AddWithValue("@DiscountPercentage", code.DiscountPercentage);
            cmd.Parameters.AddWithValue("@IsActive", code.IsActive);
            cmd.Parameters.AddWithValue("@ExpiryDate", code.ExpiryDate);
            int rows = cmd.ExecuteNonQuery();

            return rows > 0 ? Ok("Promo code updated.") : NotFound();
        }

        // Delete
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            var cmd = new MySqlCommand("DELETE FROM PromoCode WHERE PromoCodeID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            int rows = cmd.ExecuteNonQuery();

            return rows > 0 ? Ok("Promo code deleted.") : NotFound();
        }
    }
}
