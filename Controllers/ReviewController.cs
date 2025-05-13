using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GyanSagarNew.Model;

namespace GyanSagarNew.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : Controller
    {
        private readonly string _connectionString;

        public ReviewController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }

        [HttpGet("purchased/{userId}")]
        public IActionResult GetPurchasedBooks(int userId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            SELECT DISTINCT b.BookID, b.Title, b.Author, b.ImagePath
            FROM UserOrderBook uob
            INNER JOIN `Order` o ON uob.OrderID = o.OrderID
            INNER JOIN Books b ON uob.BookID = b.BookID
            WHERE uob.UserID = @UserID AND o.status = 'Successful'", conn);

                cmd.Parameters.AddWithValue("@UserID", userId);

                var reader = cmd.ExecuteReader();
                var books = new List<object>();

                while (reader.Read())
                {
                    books.Add(new
                    {
                        BookID = reader["BookID"],
                        Title = reader["Title"],
                        Author = reader["Author"],
                        ImagePath = reader["ImagePath"]
                    });
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }


        [HttpPost("submit")]
        public IActionResult SubmitReview([FromBody] AddReviewDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                // Check if review already exists
                var checkCmd = new MySqlCommand(@"
                SELECT Rating FROM BookReviews 
                WHERE UserID = @UserID AND BookID = @BookID", conn, transaction);

                checkCmd.Parameters.AddWithValue("@UserID", dto.UserId);
                checkCmd.Parameters.AddWithValue("@BookID", dto.BookId);

                var existingRatingObj = checkCmd.ExecuteScalar();
                bool reviewExists = existingRatingObj != null;

                int oldRating = reviewExists ? Convert.ToInt32(existingRatingObj) : 0;

                // Get current book rating data
                var selectCmd = new MySqlCommand(@"
                SELECT Rating, TotalRating FROM Books WHERE BookID = @BookID", conn, transaction);
                selectCmd.Parameters.AddWithValue("@BookID", dto.BookId);

                double currentAverage = 0;
                int totalRatings = 0;

                using (var reader = selectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        currentAverage = Convert.ToDouble(reader["Rating"]);
                        totalRatings = Convert.ToInt32(reader["TotalRating"]);
                    }
                }

                double newAverage;

                if (reviewExists)
                {
                    // Update existing review
                    var updateReviewCmd = new MySqlCommand(@"
                    UPDATE BookReviews 
                    SET Rating = @Rating, ReviewText = @Review, CreatedAt = NOW() 
                    WHERE UserID = @UserID AND BookID = @BookID", conn, transaction);

                    updateReviewCmd.Parameters.AddWithValue("@Rating", dto.Rating);
                    updateReviewCmd.Parameters.AddWithValue("@Review", dto.Review);
                    updateReviewCmd.Parameters.AddWithValue("@UserID", dto.UserId);
                    updateReviewCmd.Parameters.AddWithValue("@BookID", dto.BookId);
                    updateReviewCmd.ExecuteNonQuery();

                    // Adjust the average rating
                    double totalRatingSum = currentAverage * totalRatings;
                    totalRatingSum = totalRatingSum - oldRating + dto.Rating;
                    newAverage = Math.Round(totalRatingSum / totalRatings, 2);
                }
                else
                {
                    // Insert new review
                    var insertCmd = new MySqlCommand(@"
                INSERT INTO BookReviews (UserID, BookID, Rating, ReviewText, CreatedAt)
                VALUES (@UserID, @BookID, @Rating, @Review, NOW())", conn, transaction);

                    insertCmd.Parameters.AddWithValue("@UserID", dto.UserId);
                    insertCmd.Parameters.AddWithValue("@BookID", dto.BookId);
                    insertCmd.Parameters.AddWithValue("@Rating", dto.Rating);
                    insertCmd.Parameters.AddWithValue("@Review", dto.Review);
                    insertCmd.ExecuteNonQuery();

                    // Calculate new average
                    newAverage = Math.Round(((currentAverage * totalRatings) + dto.Rating) / (totalRatings + 1), 2);
                    totalRatings += 1;
                }

                // Update the book's average rating and total count
                var updateBookCmd = new MySqlCommand(@"
            UPDATE Books 
            SET Rating = @NewRating, TotalRating = @NewTotal 
            WHERE BookID = @BookID", conn, transaction);

                updateBookCmd.Parameters.AddWithValue("@NewRating", newAverage);
                updateBookCmd.Parameters.AddWithValue("@NewTotal", totalRatings);
                updateBookCmd.Parameters.AddWithValue("@BookID", dto.BookId);
                updateBookCmd.ExecuteNonQuery();

                transaction.Commit();
                return Ok("Review submitted and rating updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }



        [HttpGet("user/{userId}/book/{bookId}")]
        public IActionResult GetUserReview(int userId, int bookId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            SELECT Rating, ReviewText 
            FROM BookReviews 
            WHERE UserID = @UserID AND BookID = @BookID", conn);

                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@BookID", bookId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new
                    {
                        rating = Convert.ToInt32(reader["Rating"]),
                        reviewText = reader["ReviewText"].ToString()
                    });
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }



        [HttpGet("book/{id}/reviews")]
        public IActionResult GetBookReviews(int id)
        {
            var reviews = new List<ReviewDto>();

            // Open the MySQL connection
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Query to fetch the reviews for the book
            var cmd = new MySqlCommand("SELECT r.ReviewText, r.Rating, u.FullName FROM bookreviews r JOIN Users u ON r.UserID = u.ID WHERE r.BookID = @BookID", conn);
            cmd.Parameters.AddWithValue("@BookID", id);

            using var reader = cmd.ExecuteReader();

            while (reader.Read()) // If reviews are found
            {
                var review = new ReviewDto
                {
                    Review = reader["ReviewText"].ToString(),
                    Rating = Convert.ToInt32(reader["Rating"]),
                    Username = reader["Fullname"].ToString()
                };
                reviews.Add(review);
            }

            if (reviews.Count > 0)
            {
                return Ok(reviews); // Return reviews if found
            }

            return NotFound(new { message = "No reviews found for this book." }); // If no reviews found
        }



    }
}
