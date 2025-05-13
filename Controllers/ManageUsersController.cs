using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace GyanSagarNew.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class ManageUsersController : Controller
    {
        private readonly string _connectionString;

        public ManageUsersController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }




        // Get all users
        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var users = new List<object>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var cmd = new MySqlCommand("SELECT Id, FullName, Email, Role FROM Users", conn);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new
                {
                    Id = reader["Id"],
                    FullName = reader["FullName"],
                    Email = reader["Email"],
                    Role = reader["Role"]
                });
            }

            return Ok(users);
        }

        // POST: api/User/update-role
        [HttpPost("update-role")]
        public IActionResult UpdateUserRole([FromBody] UpdateUserRoleDto dto)
        {
            if (string.IsNullOrEmpty(dto.Role) || dto.UserId <= 0)
            {
                return BadRequest("Invalid data.");
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var cmd = new MySqlCommand("UPDATE Users SET Role = @Role WHERE Id = @UserId", conn);
            cmd.Parameters.AddWithValue("@Role", dto.Role);
            cmd.Parameters.AddWithValue("@UserId", dto.UserId);

            int affected = cmd.ExecuteNonQuery();

            return affected > 0
                ? Ok("Role updated successfully.")
                : NotFound("User not found.");
        }


    }
    }
