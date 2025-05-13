using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Net.Mail;
using System.Net;

namespace GyanSagarNew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuyController : Controller
    {
        private readonly string _connectionString;
        private readonly string _gmailUser;
        private readonly string _gmailAppPassword;

        public BuyController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
            _gmailUser = config["GmailCredentials:GmailUser"];
            _gmailAppPassword = config["GmailCredentials:GmailAppPassword"];
        }

        [HttpPost("checkout")]
        public IActionResult Checkout([FromBody] CheckoutDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Fetch user email
                string userEmail = null;
                var emailCmd = new MySqlCommand("SELECT Email FROM Users WHERE Id = @UserID", conn);
                emailCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                using (var reader = emailCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userEmail = reader["Email"].ToString();
                    }
                    else
                    {
                        return BadRequest("User not found.");
                    }
                }


                // Step 1: Get cart items with SaleDiscount
                var cartItems = new List<ViewCartItemDto>();
                var cmd = new MySqlCommand(@"
            SELECT c.BookID, b.Title, b.Price, b.SaleDiscount, c.Quantity
            FROM Cart c
            JOIN Books b ON c.BookID = b.BookID
            WHERE c.UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", dto.UserID);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var originalPrice = Convert.ToDecimal(reader["Price"]);
                        var saleDiscount = reader["SaleDiscount"] != DBNull.Value ? Convert.ToDecimal(reader["SaleDiscount"]) : 0;
                        decimal finalPrice = originalPrice;

                        if (saleDiscount > 0)
                        {
                            finalPrice = originalPrice * (1 - saleDiscount / 100); // Apply sale discount
                        }

                        cartItems.Add(new ViewCartItemDto
                        {
                            BookID = Convert.ToInt32(reader["BookID"]),
                            Title = reader["Title"].ToString(),
                            Price = finalPrice,
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });
                    }
                }

                if (cartItems.Count == 0)
                    return BadRequest("Cart is empty.");

                decimal totalAmount = cartItems.Sum(item => item.Price * item.Quantity);
                decimal discountAmount = 0;
                decimal finalAmount = totalAmount;

                // Step 2: Apply promo code
                int? promoCodeID = null;
                if (!string.IsNullOrEmpty(dto.PromoCode))
                {
                    var promoCmd = new MySqlCommand(@"
                SELECT PromoCodeID, DiscountPercentage 
                FROM PromoCode 
                WHERE Code = @Code AND IsActive = 1 AND ExpiryDate >= NOW()", conn);
                    promoCmd.Parameters.AddWithValue("@Code", dto.PromoCode);

                    using (var promoReader = promoCmd.ExecuteReader())
                    {
                        if (promoReader.Read())
                        {
                            promoCodeID = Convert.ToInt32(promoReader["PromoCodeID"]);
                            decimal promoDiscount = Convert.ToDecimal(promoReader["DiscountPercentage"]);
                            decimal promoAmount = totalAmount * promoDiscount / 100;
                            discountAmount += promoAmount;
                            finalAmount -= promoAmount;
                        }
                        else
                        {
                            return BadRequest("Invalid or expired promo code.");
                        }
                    }
                }

                // Step 3: 5% discount if 5+ books
                int totalQuantity = cartItems.Sum(item => item.Quantity);
                if (totalQuantity >= 5)
                {
                    decimal qtyDiscount = totalAmount * 0.05m;
                    discountAmount += qtyDiscount;
                    finalAmount -= qtyDiscount;
                }

                // Step 4: 10% loyalty discount if 10+ orders
                var orderCountCmd = new MySqlCommand(@"
                SELECT COUNT(*) 
                FROM userorder uo
                JOIN `Order` o ON uo.OrderID = o.OrderID
                WHERE uo.UserID = @UserID AND o.Status = 'successful'
                ", conn);
                orderCountCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                int orderCount = Convert.ToInt32(orderCountCmd.ExecuteScalar());

                if (orderCount >= 10)
                {
                    decimal loyaltyDiscount = totalAmount * 0.10m;
                    discountAmount += loyaltyDiscount;
                    finalAmount -= loyaltyDiscount;
                }

                // Step 5: Generate a random verification code
                string verificationCode = GenerateVerificationCode();

                // Step 6: Insert order with verification code
                var orderCmd = new MySqlCommand(@"
            INSERT INTO `Order` (OrderDate, TotalAmount, DiscountPercentage, DiscountAmount, PromoCodeID, VerificationCode)
            VALUES (NOW(), @TotalAmount, @DiscountPercentage, @DiscountAmount, @PromoCodeID, @VerificationCode)", conn);
                orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                orderCmd.Parameters.AddWithValue("@DiscountPercentage", discountAmount / totalAmount * 100);
                orderCmd.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                orderCmd.Parameters.AddWithValue("@PromoCodeID", promoCodeID.HasValue ? promoCodeID : DBNull.Value);
                orderCmd.Parameters.AddWithValue("@VerificationCode", verificationCode);
                orderCmd.ExecuteNonQuery();

                int orderID = (int)orderCmd.LastInsertedId;

                // Step 7: Link user to order
                var linkCmd = new MySqlCommand("INSERT INTO userorder (UserID, OrderID) VALUES (@UserID, @OrderID)", conn);
                linkCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                linkCmd.Parameters.AddWithValue("@OrderID", orderID);
                linkCmd.ExecuteNonQuery();

                // Step 8: Insert each book into userorderbook
                foreach (var item in cartItems)
                {
                    var bookCmd = new MySqlCommand(@"
                INSERT INTO userorderbook (UserID, OrderID, BookID, Quantity)
                VALUES (@UserID, @OrderID, @BookID, @Quantity)", conn);
                    bookCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                    bookCmd.Parameters.AddWithValue("@OrderID", orderID);
                    bookCmd.Parameters.AddWithValue("@BookID", item.BookID);
                    bookCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    bookCmd.ExecuteNonQuery();
                }

                // Step 9: Clear the cart
                var clearCmd = new MySqlCommand("DELETE FROM Cart WHERE UserID = @UserID", conn);
                clearCmd.Parameters.AddWithValue("@UserID", dto.UserID);
                clearCmd.ExecuteNonQuery();

                bool emailSent = SendOrderVerificationEmail(userEmail, verificationCode);
                if (!emailSent)
                {
                    return StatusCode(500, "Failed to send verification email.");
                }


                // Step 10: Return response
                return Ok(new
                {
                    OrderID = orderID,
                    FinalAmount = finalAmount,
                    DiscountAmount = discountAmount,
                    PromoCodeApplied = dto.PromoCode,
                    StackableDiscountApplied = totalQuantity >= 5,
                    AdditionalDiscountApplied = orderCount >= 10,
                    VerificationCode = verificationCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }


        // Helper method to generate a random verification code
        private string GenerateVerificationCode(int length = 6)
        {
            var random = new Random();
            var verificationCode = "";

            for (int i = 0; i < length; i++)
            {
                verificationCode += random.Next(0, 10).ToString();
            }

            return verificationCode;
        }

        private bool SendOrderVerificationEmail(string toEmail, string code)
        {
            try
            {
                var subject = "Your Order Verification Code";
                var body = $"Thank you for your order! Your verification code is: {code}";

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_gmailUser, _gmailAppPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_gmailUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email error: " + ex.Message);
                return false;
            }
        }
    }
}
