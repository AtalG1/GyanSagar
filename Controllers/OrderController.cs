using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GyanSagarNew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly string _connectionString;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderController(IConfiguration config, IHubContext<OrderHub> hubContext)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
            _hubContext = hubContext;
        }

        // Api to show all the orders made by users. This can be accessed by members only
        
        [HttpGet("orders/{userId}")]
        public IActionResult GetOrders(int userId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Fetch all orders placed by the user
                var orders = new List<OrderDto>();

                var cmd = new MySqlCommand(@"
            SELECT o.OrderID, o.OrderDate, o.TotalAmount, o.DiscountAmount, o.PromoCodeID, o.VerificationCode, o.Status
            FROM `Order` o
            JOIN userorder uo ON o.OrderID = uo.OrderID
            WHERE uo.UserID = @UserID ORDER BY o.OrderDate DESC", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                        decimal discountAmount = Convert.ToDecimal(reader["DiscountAmount"]);
                        decimal finalAmount = totalAmount - discountAmount;

                        orders.Add(new OrderDto
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            TotalAmount = totalAmount,
                            DiscountAmount = discountAmount,
                            FinalAmount = finalAmount,
                            PromoCodeID = reader["PromoCodeID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PromoCodeID"]),
                            VerificationCode = reader["VerificationCode"].ToString(),
                            Status = reader["Status"].ToString()
                        });
                    }
                }

                if (orders.Count == 0)
                    return NotFound("No orders found.");
                
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        /// Api to cancle order can be used by both admin and members 

        [HttpPost("cancel/{orderId}")]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Optional: Check if order exists or is already canceled

                var cmd = new MySqlCommand("UPDATE `Order` SET Status = 'Cancelled' WHERE OrderID = @OrderID", conn);
                cmd.Parameters.AddWithValue("@OrderID", orderId);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                    return NotFound("Order not found or already cancelled.");

                return Ok("Order cancelled successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }


        //// Api to see details of one specific order

        [HttpGet("details/{orderId}")]
        public IActionResult GetOrderDetails(int orderId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                OrderDto order = null;

                var cmd = new MySqlCommand(@"
            SELECT * FROM `Order` WHERE OrderID = @OrderID", conn);
                cmd.Parameters.AddWithValue("@OrderID", orderId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        order = new OrderDto
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                            FinalAmount = Convert.ToDecimal(reader["TotalAmount"]) - Convert.ToDecimal(reader["DiscountAmount"]),
                            PromoCodeID = reader["PromoCodeID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PromoCodeID"]),
                            VerificationCode = reader["VerificationCode"].ToString(),
                            Status = reader["Status"].ToString(),
                            Books = new List<OrderDetailsDto>()
                        };
                    }
                }

                if (order == null)
                    return NotFound();

                // Get book details
                var bookCmd = new MySqlCommand(@"
            SELECT b.BookID, b.Title, b.Author, b.Price, uob.Quantity
            FROM userorderbook uob
            JOIN books b ON b.BookID = uob.BookID
            WHERE uob.OrderID = @OrderID", conn);
                bookCmd.Parameters.AddWithValue("@OrderID", orderId);

                using (var reader = bookCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        order.Books.Add(new OrderDetailsDto
                        {
                            BookID = Convert.ToInt32(reader["BookID"]),
                            Title = reader["Title"].ToString(),
                            Author = reader["Author"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });
                    }
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }





        /// Api to see all the pending orders. Used by only admin 

        [HttpGet("pending-orders")]
        public IActionResult GetPendingOrders()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var orders = new List<OrderDto>();

                var cmd = new MySqlCommand(@"
            SELECT o.OrderID, o.OrderDate, o.TotalAmount, o.DiscountAmount, o.PromoCodeID,
                   o.VerificationCode, o.Status
            FROM `Order` o
            WHERE o.Status = 'Pending'
            ORDER BY o.OrderDate DESC", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                        decimal discountAmount = Convert.ToDecimal(reader["DiscountAmount"]);
                        decimal finalAmount = totalAmount - discountAmount;

                        orders.Add(new OrderDto
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            TotalAmount = totalAmount,
                            DiscountAmount = discountAmount,
                            FinalAmount = finalAmount,
                            PromoCodeID = reader["PromoCodeID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PromoCodeID"]),
                            VerificationCode = reader["VerificationCode"].ToString(),
                            Status = reader["Status"].ToString()
                        });
                    }
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        // Api to change the status of orders to successful and also insert into invoice table

        [HttpPost("verify/{orderId}")]
        public async Task<IActionResult> VerifyOrder(int orderId, [FromBody] string code)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Step 1: Update status to Successful
                var updateCmd = new MySqlCommand(@"
            UPDATE `Order`
            SET Status = 'Successful'
            WHERE OrderID = @OrderID AND VerificationCode = @Code AND Status = 'Pending'", conn);
                updateCmd.Parameters.AddWithValue("@OrderID", orderId);
                updateCmd.Parameters.AddWithValue("@Code", code);

                int rowsAffected = updateCmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                    return BadRequest("Invalid code or order already processed.");

                // Step 2: Fetch order and user info
                var orderCmd = new MySqlCommand(@"
            SELECT o.OrderID, o.OrderDate, o.TotalAmount, o.DiscountAmount, uo.UserID,
                   u.FullName, u.Email, u.PhoneNumber, u.Address
            FROM `Order` o
            JOIN userorder uo ON o.OrderID = uo.OrderID
            JOIN Users u ON u.Id = uo.UserID
            WHERE o.OrderID = @OrderID", conn);
                orderCmd.Parameters.AddWithValue("@OrderID", orderId);

                int userId;
                string fullName, email, phone, address;
                DateTime orderDate;
                decimal totalAmount, discountAmount;

                using (var reader = orderCmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return BadRequest("Order not found.");

                    userId = Convert.ToInt32(reader["UserID"]);
                    fullName = reader["FullName"].ToString();
                    email = reader["Email"].ToString();
                    phone = reader["PhoneNumber"].ToString();
                    address = reader["Address"].ToString();
                    orderDate = Convert.ToDateTime(reader["OrderDate"]);
                    totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                    discountAmount = Convert.ToDecimal(reader["DiscountAmount"]);
                }

                // Step 3: Load book list first
                var bookList = new List<(int BookID, string Title, string Author, decimal Price, int Quantity)>();

                var bookCmd = new MySqlCommand(@"
            SELECT b.BookID, b.Title, b.Author, b.Price, uob.Quantity
            FROM userorderbook uob
            JOIN books b ON b.BookID = uob.BookID
            WHERE uob.OrderID = @OrderID", conn);
                bookCmd.Parameters.AddWithValue("@OrderID", orderId);

                using (var bookReader = bookCmd.ExecuteReader())
                {
                    while (bookReader.Read())
                    {
                        bookList.Add((
                            Convert.ToInt32(bookReader["BookID"]),
                            bookReader["Title"].ToString(),
                            bookReader["Author"].ToString(),
                            Convert.ToDecimal(bookReader["Price"]),
                            Convert.ToInt32(bookReader["Quantity"])
                        ));
                    }
                }

                // Step 4: Now inserting invoice rows

                var bookTitlesList = new List<string>();

                foreach (var book in bookList)
                {
                    var invoiceInsertCmd = new MySqlCommand(@"
                INSERT INTO invoice (
                    UserID, UserFullName, UserEmail, UserPhoneNumber, UserAddress,
                    OrderID, OrderDate, BookID, BookTitle, BookAuthor, BookPrice,
                    Quantity, Total, Discount, FinalAmount
                )
                VALUES (
                    @UserID, @UserFullName, @UserEmail, @UserPhoneNumber, @UserAddress,
                    @OrderID, @OrderDate, @BookID, @BookTitle, @BookAuthor, @BookPrice,
                    @Quantity, @Total, @Discount, @FinalAmount
                )", conn);

                    invoiceInsertCmd.Parameters.AddWithValue("@UserID", userId);
                    invoiceInsertCmd.Parameters.AddWithValue("@UserFullName", fullName);
                    invoiceInsertCmd.Parameters.AddWithValue("@UserEmail", email);
                    invoiceInsertCmd.Parameters.AddWithValue("@UserPhoneNumber", phone);
                    invoiceInsertCmd.Parameters.AddWithValue("@UserAddress", address);
                    invoiceInsertCmd.Parameters.AddWithValue("@OrderID", orderId);
                    invoiceInsertCmd.Parameters.AddWithValue("@OrderDate", orderDate);
                    invoiceInsertCmd.Parameters.AddWithValue("@BookID", book.BookID);
                    invoiceInsertCmd.Parameters.AddWithValue("@BookTitle", book.Title);
                    invoiceInsertCmd.Parameters.AddWithValue("@BookAuthor", book.Author);
                    invoiceInsertCmd.Parameters.AddWithValue("@BookPrice", book.Price);
                    invoiceInsertCmd.Parameters.AddWithValue("@Quantity", book.Quantity);
                    invoiceInsertCmd.Parameters.AddWithValue("@Total", book.Price * book.Quantity);
                    invoiceInsertCmd.Parameters.AddWithValue("@Discount", discountAmount);
                    invoiceInsertCmd.Parameters.AddWithValue("@FinalAmount", totalAmount - discountAmount);

                    //invoiceInsertCmd.ExecuteNonQuery();

                    try
                    {
                        // Execute the insert command
                        invoiceInsertCmd.ExecuteNonQuery();
                        Console.WriteLine($"Invoice successfully inserted for OrderID={orderId} and BookID={book.BookID}");

                        bookTitlesList.Add(book.Title);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception if the insert fails
                        Console.WriteLine($"Error inserting into Invoice for OrderID={orderId} and BookID={book.BookID}: {ex.Message}");
                    }
            }

                var bookTitles = string.Join(", ", bookTitlesList);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveBooks",
                    $"✅ Order #{orderId} marked successful!",
                    bookTitles
                );

                return Ok("Order verified and invoice created.");
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error in VerifyOrder: " + ex.Message);

                return StatusCode(500, "Server error: " + ex.Message);



            }
        }




        // Api that shows the invoice of 

        [HttpGet("invoices")]
        public IActionResult GetAllInvoicesGroupedByOrder()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
            SELECT *
            FROM invoice
            ORDER BY OrderID Desc, CreatedAt", conn);

                var invoices = new List<ViewInvoiceDto>();
                var invoiceDict = new Dictionary<int, ViewInvoiceDto>();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int orderId = Convert.ToInt32(reader["OrderID"]);

                    if (!invoiceDict.ContainsKey(orderId))
                    {
                        invoiceDict[orderId] = new ViewInvoiceDto
                        {
                            OrderID = orderId,
                            UserID = Convert.ToInt32(reader["UserID"]),
                            UserFullName = reader["UserFullName"].ToString(),
                            UserEmail = reader["UserEmail"].ToString(),
                            UserPhoneNumber = reader["UserPhoneNumber"].ToString(),
                            UserAddress = reader["UserAddress"].ToString(),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            Discount = Convert.ToDecimal(reader["Discount"]),
                            FinalAmount = Convert.ToDecimal(reader["FinalAmount"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            Books = new List<BookDInvoice>()
                        };
                    }

                    invoiceDict[orderId].Books.Add(new BookDInvoice
                    {
                        BookID = Convert.ToInt32(reader["BookID"]),
                        BookTitle = reader["BookTitle"].ToString(),
                        BookAuthor = reader["BookAuthor"].ToString(),
                        BookPrice = Convert.ToDecimal(reader["BookPrice"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Total = Convert.ToDecimal(reader["Total"])
                    });
                }

                invoices = invoiceDict.Values.ToList();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching all invoices: " + ex.Message);
                return StatusCode(500, "Server error: " + ex.Message);
            }
        }





    }
}
