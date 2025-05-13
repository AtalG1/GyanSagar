using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GyanSagarNew.Model;

namespace GyanSagarNew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public BooksController(IConfiguration config, IWebHostEnvironment env)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
            _env = env;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddBook([FromForm] AddBookDto dto)
        {
            try
            {
                string? imagePath = null;

                if (dto.BookImage != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(dto.BookImage.FileName);
                    var fullPath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await dto.BookImage.CopyToAsync(stream);

                    imagePath = $"/uploads/{fileName}";
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
                    INSERT INTO Books 
                    (Title, Author, Genre, ISBN, Price, Language, Format, Publisher, StockAvailability, CreatedAt, ImagePath)
                    VALUES 
                    (@Title, @Author, @Genre, @ISBN, @Price, @Language, @Format, @Publisher, @StockAvailability, @CreatedAt, @ImagePath)", conn);

                cmd.Parameters.AddWithValue("@Title", dto.Title);
                cmd.Parameters.AddWithValue("@Author", dto.Author);
                cmd.Parameters.AddWithValue("@Genre", dto.Genre);
                cmd.Parameters.AddWithValue("@ISBN", dto.ISBN);
                cmd.Parameters.AddWithValue("@Price", dto.Price);
                cmd.Parameters.AddWithValue("@Language", dto.Language);
                cmd.Parameters.AddWithValue("@Format", dto.Format);
                cmd.Parameters.AddWithValue("@Publisher", dto.Publisher);
                cmd.Parameters.AddWithValue("@StockAvailability", dto.StockAvailability);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@ImagePath", imagePath ?? "");

                cmd.ExecuteNonQuery();
                return Redirect("/AdminBook.html");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }



        [HttpGet("all-details")]
        public IActionResult GetAllBookDetails(
                string? search = null,
                string? genre = null,
                string? language = null,
                decimal? maxPrice = null,
                bool? onSale = null,
                string? sortBy = null,
                int page = 1,
                int pageSize = 12 // default page size
            )
        {
            var books = new List<ViewBookDto>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var baseQuery = "FROM Books WHERE 1=1";
            var cmd = new MySqlCommand();
            cmd.Connection = conn;

            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery += " AND (LOWER(Title) LIKE @search OR LOWER(Author) LIKE @search OR LOWER(Genre) LIKE @search OR LOWER(Language) LIKE @search)";
                cmd.Parameters.AddWithValue("@search", $"%{search.ToLower()}%");
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                baseQuery += " AND Genre = @genre";
                cmd.Parameters.AddWithValue("@genre", genre);
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                baseQuery += " AND Language = @language";
                cmd.Parameters.AddWithValue("@language", language);
            }

            if (maxPrice.HasValue)
            {
                baseQuery += " AND Price <= @maxPrice";
                cmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);
            }

            if (onSale.HasValue)
            {
                baseQuery += " AND IsOnSale = @onSale";
                cmd.Parameters.AddWithValue("@onSale", onSale.Value);
            }

            // Get total count for pagination metadata
            cmd.CommandText = $"SELECT COUNT(*) {baseQuery}";
            var totalCount = Convert.ToInt32(cmd.ExecuteScalar());

            // Now get paginated data
            string sortClause = sortBy switch
            {
                "low-to-high" => "ORDER BY Price ASC",
                "high-to-low" => "ORDER BY Price DESC",
                _ => ""
            };

            int offset = (page - 1) * pageSize;
            cmd.CommandText = $"SELECT * {baseQuery} {sortClause} LIMIT @limit OFFSET @offset";
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                books.Add(new ViewBookDto
                {
                    BookID = Convert.ToInt32(reader["BookID"]),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    Genre = reader["Genre"].ToString(),
                    Price = Convert.ToDecimal(reader["Price"]),
                    Language = reader["Language"].ToString(),
                    Format = reader["Format"].ToString(),
                    Publisher = reader["Publisher"].ToString(),
                    StockAvailability = Convert.ToInt32(reader["StockAvailability"]),
                    Rating = Convert.ToSingle(reader["Rating"]),
                    TotalRating = Convert.ToInt32(reader["TotalRating"]),
                    TotalPurchase = Convert.ToInt32(reader["TotalPurchase"]),
                    IsOnSale = Convert.ToBoolean(reader["IsOnSale"]),
                    SaleDiscount = Convert.ToInt32(reader["SaleDiscount"]),
                    ImagePath = reader["ImagePath"].ToString()
                });
            }

            return Ok(new
            {
                totalCount,
                books
            });
        }



        /// GEt one book

        [HttpGet("book/{id}")]
        public IActionResult GetBookById(int id)
        {
            var book = new ViewBookDto();

            // Open the MySQL connection
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Query to fetch the book by its ID
            var cmd = new MySqlCommand("SELECT * FROM Books WHERE BookID = @BookID", conn);
            cmd.Parameters.AddWithValue("@BookID", id);

            using var reader = cmd.ExecuteReader();

            if (reader.Read()) // If a book is found
            {
                book = new ViewBookDto
                {
                    BookID = Convert.ToInt32(reader["BookID"]),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    Genre = reader["Genre"].ToString(),
                    ISBN = reader["ISBN"].ToString(),
                    Price = Convert.ToDecimal(reader["Price"]),
                    Language = reader["Language"].ToString(),
                    Format = reader["Format"].ToString(),
                    Publisher = reader["Publisher"].ToString(),
                    StockAvailability = Convert.ToInt32(reader["StockAvailability"]),
                    Rating = Convert.ToSingle(reader["Rating"]),
                    TotalRating = Convert.ToInt32(reader["TotalRating"]),
                    TotalPurchase = Convert.ToInt32(reader["TotalPurchase"]),
                    IsOnSale = Convert.ToBoolean(reader["IsOnSale"]),
                    SaleDiscount = Convert.ToInt32(reader["SaleDiscount"]),
                    ImagePath = reader["ImagePath"].ToString()
                };

                return Ok(book);

                // Return the book details if found
            }

            return NotFound(new { message = "Book not found." }); // Return NotFound if no book is found
        }



        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromForm] UpdateBookDto dto)
        {
            try
            {
                string? imagePath = null;

                if (dto.BookImage != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(dto.BookImage.FileName);
                    var fullPath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await dto.BookImage.CopyToAsync(stream);

                    imagePath = $"/uploads/{fileName}";
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            UPDATE Books SET
                Title = @Title,
                Author = @Author,
                Genre = @Genre,
                ISBN = @ISBN,
                Price = @Price,
                Language = @Language,
                Format = @Format,
                Publisher = @Publisher,
                StockAvailability = @StockAvailability,
                IsOnSale = @IsOnSale,
                SaleDiscount = @SaleDiscount,
                ImagePath = IF(@ImagePath = '', ImagePath, @ImagePath)
            WHERE BookID = @BookID", conn);

                cmd.Parameters.AddWithValue("@BookID", id);
                cmd.Parameters.AddWithValue("@Title", dto.Title);
                cmd.Parameters.AddWithValue("@Author", dto.Author);
                cmd.Parameters.AddWithValue("@Genre", dto.Genre);
                cmd.Parameters.AddWithValue("@ISBN", dto.ISBN);
                cmd.Parameters.AddWithValue("@Price", dto.Price);
                cmd.Parameters.AddWithValue("@Language", dto.Language);
                cmd.Parameters.AddWithValue("@Format", dto.Format);
                cmd.Parameters.AddWithValue("@Publisher", dto.Publisher);
                cmd.Parameters.AddWithValue("@StockAvailability", dto.StockAvailability);
                cmd.Parameters.AddWithValue("@IsOnSale", dto.IsOnSale);
                cmd.Parameters.AddWithValue("@SaleDiscount", dto.SaleDiscount);
                cmd.Parameters.AddWithValue("@ImagePath", imagePath ?? "");

                int result = cmd.ExecuteNonQuery();
                return result > 0 ? Ok("Book updated.") : NotFound("Book not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }



        [HttpDelete("delete/{id}")]
        public IActionResult DeleteBook(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand("DELETE FROM Books WHERE BookID = @BookID", conn);
                cmd.Parameters.AddWithValue("@BookID", id);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Book deleted successfully." });
                }
                else
                {
                    return NotFound(new { error = "Book not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error: " + ex.Message });
            }
        }





    }
}
