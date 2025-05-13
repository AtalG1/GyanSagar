using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace GyanSagarNew.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class BookmarkController : Controller
    {
        private readonly string _connectionString;

        public BookmarkController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }


        [HttpPost("add")]
        public IActionResult AddToBookmark([FromBody] AddToBookmarkDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Bookmark WHERE UserID = @UserID AND BookID = @BookID", conn);
                checkCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                checkCmd.Parameters.AddWithValue("@BookID", dto.BookID);

                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (exists)
                {
                    return Conflict("Book already bookmarked.");
                }

                var insertCmd = new MySqlCommand("INSERT INTO Bookmark (UserID, BookID) VALUES (@UserID, @BookID)", conn);
                insertCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                insertCmd.Parameters.AddWithValue("@BookID", dto.BookID);
                insertCmd.ExecuteNonQuery();

                return Ok("Book bookmarked successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }




        [HttpGet("view/{userId}")]
        public IActionResult ViewBookmark(int userId)
        {
            try
            {
                var bookmarks = new List<ViewBookmarkDto>();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
                    SELECT b.BookID, b.Title, b.Author, b.Price, b.ImagePath
                    FROM Bookmark bm
                    JOIN Books b ON bm.BookID = b.BookID
                    WHERE bm.UserID = @UserID", conn);

                cmd.Parameters.AddWithValue("@UserID", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bookmarks.Add(new ViewBookmarkDto
                    {
                        BookID = Convert.ToInt32(reader["BookID"]),
                        Title = reader["Title"].ToString(),
                        Author = reader["Author"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        ImagePath = reader["ImagePath"].ToString()
                    });
                }

                return Ok(bookmarks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }


        [HttpDelete("delete/{bookId}")]
        public IActionResult DeleteBookmark(int bookId, [FromQuery] int userId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand("DELETE FROM Bookmark WHERE UserID = @UserID AND BookID = @BookID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@BookID", bookId);

                var affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows > 0)
                {
                    return Ok("Bookmark removed.");
                }

                return NotFound("Bookmark not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }



    }
}
