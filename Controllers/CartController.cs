using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace GyanSagarNew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly string _connectionString;

        public CartController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }

        [HttpPost("add")]
        public IActionResult AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Check if the item is already in the cart
                var checkCmd = new MySqlCommand("SELECT Quantity FROM Cart WHERE UserID = @UserID AND BookID = @BookID", conn);
                checkCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                checkCmd.Parameters.AddWithValue("@BookID", dto.BookID);

                var existingQuantity = checkCmd.ExecuteScalar();

                if (existingQuantity != null)
                {
                    // Update quantity (+1)
                    var newQuantity = Convert.ToInt32(existingQuantity) + 1;
                    var updateCmd = new MySqlCommand("UPDATE Cart SET Quantity = @Quantity WHERE UserID = @UserID AND BookID = @BookID", conn);
                    updateCmd.Parameters.AddWithValue("@Quantity", newQuantity);
                    updateCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                    updateCmd.Parameters.AddWithValue("@BookID", dto.BookID);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // Insert new item with Quantity = 1
                    var insertCmd = new MySqlCommand("INSERT INTO Cart (UserID, BookID, Quantity) VALUES (@UserID, @BookID, 1)", conn);
                    insertCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                    insertCmd.Parameters.AddWithValue("@BookID", dto.BookID);
                    insertCmd.ExecuteNonQuery();
                }

                return Ok("Item added to cart successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }




        [HttpGet("view/{userId}")]
        public IActionResult ViewCart(int userId)
        {
            try
            {
                var cartItems = new List<ViewCartItemDto>();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            SELECT c.BookID, b.Title, b.Price, b.Author, b.ImagePath, c.Quantity, 
                   b.SaleDiscount, b.IsOnSale
            FROM Cart c
            JOIN Books b ON c.BookID = b.BookID
            WHERE c.UserID = @UserID;", conn);

                cmd.Parameters.AddWithValue("@UserID", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cartItems.Add(new ViewCartItemDto
                    {
                        BookID = Convert.ToInt32(reader["BookID"]),
                        Title = reader["Title"].ToString(),
                        Author = reader["Author"].ToString(),
                        ImagePath = reader["ImagePath"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        // Handle nullable fields for SaleDiscount and IsOnSale
                        SaleDiscount = reader["SaleDiscount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SaleDiscount"]),
                        IsOnSale = reader["IsOnSale"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsOnSale"])
                    });
                }

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }







        [HttpPut("update")]
        public IActionResult UpdateCartItem([FromBody] UpdateCartDto updateCartItem)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            UPDATE Cart 
            SET Quantity = @Quantity
            WHERE UserID = @UserID AND BookID = @BookID;", conn);

                cmd.Parameters.AddWithValue("@Quantity", updateCartItem.Quantity);
                cmd.Parameters.AddWithValue("@UserID", updateCartItem.UserID);
                cmd.Parameters.AddWithValue("@BookID", updateCartItem.BookID);

                var affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows > 0)
                {
                    return Ok(new { message = "Quantity updated successfully" });
                }
                return BadRequest(new { message = "Failed to update quantity" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }





        [HttpDelete("delete/{bookId}")]
        public IActionResult DeleteCartItem(int bookId, [FromQuery] int userId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            DELETE FROM Cart 
            WHERE UserID = @UserID AND BookID = @BookID;", conn);

                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@BookID", bookId);

                var affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows > 0)
                {
                    return Ok(new { message = "Item removed from cart" });
                }
                return BadRequest(new { message = "Failed to remove item from cart" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }




        [HttpGet("validate")]
        public IActionResult ValidatePromo([FromQuery] string code)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var cmd = new MySqlCommand(@"SELECT DiscountPercentage FROM PromoCode 
                                     WHERE Code = @Code AND IsActive = 1 AND ExpiryDate > NOW()", conn);
            cmd.Parameters.AddWithValue("@Code", code);

            var result = cmd.ExecuteScalar();
            if (result != null)
            {
                return Ok(new { discountPercentage = Convert.ToDecimal(result) });
            }
            else
            {
                return BadRequest("Invalid or expired promo code.");
            }

        }



        }

}
