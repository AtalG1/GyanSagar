using GyanSagarNew.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;


namespace GyanSagarNew.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _gmailUser;
        private readonly string _gmailAppPassword;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("MySqlConnection");
            _gmailUser = configuration["GmailCredentials:GmailUser"];
            _gmailAppPassword = configuration["GmailCredentials:GmailAppPassword"];
        }


        [HttpPost("request-verification-code")]
        public IActionResult RequestVerificationCode([FromBody] RegisterDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Check for existing email or phone
                var checkEmailCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
                checkEmailCmd.Parameters.AddWithValue("@Email", dto.Email);
                if (Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0)
                    return BadRequest(new { message = "Email already registered.", status = "error" });

                var checkPhoneCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE PhoneNumber = @PhoneNumber", conn);
                checkPhoneCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
                if (Convert.ToInt32(checkPhoneCmd.ExecuteScalar()) > 0)
                    return BadRequest(new { message = "Phone number already registered.", status = "error" });

                // Generate code
                var code = GenerateVerificationCode();

                // Save code and user temporarily (for demo, use static dictionary - you can use Redis or SQL temp table in production)
                TemporaryStorage.PendingRegistrations[dto.Email] = (dto, code, DateTime.UtcNow.AddMinutes(10));

                // Send email
                SendEmail(dto.Email, "Your Gyan Sagar verification code", $"Your verification code is: {code}");

                return Ok(new { message = "Verification code sent to email.", status = "success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, status = "error" });
            }
        }


        public static class TemporaryStorage
        {
            public static Dictionary<string, (RegisterDto Dto, string Code, DateTime ExpiresAt)> PendingRegistrations = new();
        }


        [HttpPost("verify-code")]
        public IActionResult VerifyCode([FromBody] VerifyCodeDto dto)
        {
            if (!TemporaryStorage.PendingRegistrations.TryGetValue(dto.Email, out var entry))
                return BadRequest(new { message = "No pending registration found.", status = "error" });

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                TemporaryStorage.PendingRegistrations.Remove(dto.Email);
                return BadRequest(new { message = "Verification code expired.", status = "error" });
            }

            if (entry.Code != dto.Code)
                return BadRequest(new { message = "Invalid verification code.", status = "error" });

            try
            {
                var user = entry.Dto;
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var hash = ComputeHash(user.Password);

                var cmd = new MySqlCommand(@"
            INSERT INTO Users (FullName, Email, Address, PhoneNumber, PasswordHash, Role)
            VALUES (@FullName, @Email, @Address, @PhoneNumber, @PasswordHash, 'User')", conn);

                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Address", user.Address ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@PasswordHash", hash);

                cmd.ExecuteNonQuery();
                TemporaryStorage.PendingRegistrations.Remove(dto.Email);

                return Ok(new { message = "Registration successful", status = "success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, status = "error" });
            }
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
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
        }


        // Register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Check if the user already exists based on email
                var checkEmailCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
                checkEmailCmd.Parameters.AddWithValue("@Email", dto.Email);
                var emailExists = Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0;

                if (emailExists)
                    return BadRequest(new { message = "Email already exists.", status = "error" });

                // Check if the user already exists based on phone number
                var checkPhoneCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE PhoneNumber = @PhoneNumber", conn);
                checkPhoneCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
                var phoneExists = Convert.ToInt32(checkPhoneCmd.ExecuteScalar()) > 0;

                if (phoneExists)
                    return BadRequest(new { message = "Phone number already exists.", status = "error" });




                // Hash the password
                string passwordHash = ComputeHash(dto.Password);

                var cmd = new MySqlCommand(@"
                INSERT INTO Users (FullName, Email, Address, PhoneNumber, PasswordHash, Role)
                VALUES (@FullName, @Email, @Address, @PhoneNumber, @PasswordHash, 'User')", conn);

                cmd.Parameters.AddWithValue("@FullName", dto.FullName);
                cmd.Parameters.AddWithValue("@Email", dto.Email);
                cmd.Parameters.AddWithValue("@Address", dto.Address ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                cmd.ExecuteNonQuery();

                return Ok(new { message = "User registered successfully", status = "success" });
            }
            catch (Exception ex)
            {
                // Log the exception message and return the same message to the frontend
                Console.Error.WriteLine($"An error occurred during registration: {ex.Message}");

                // Return a detailed error message in the response
                return StatusCode(500, new { message = ex.Message, status = "error" });
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


        [HttpPut("update/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Check if user exists
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Id = @Id", conn);
                checkCmd.Parameters.AddWithValue("@Id", id);
                var userExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!userExists)
                    return NotFound(new { message = "User not found", status = "error" });

                // Check if updated email is used by someone else
                var emailCheckCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email AND Id != @Id", conn);
                emailCheckCmd.Parameters.AddWithValue("@Email", dto.Email);
                emailCheckCmd.Parameters.AddWithValue("@Id", id);
                var emailInUse = Convert.ToInt32(emailCheckCmd.ExecuteScalar()) > 0;

                if (emailInUse)
                    return BadRequest(new { message = "Email already in use by another user", status = "error" });

                // Check if updated phone number is used by someone else
                var phoneCheckCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE PhoneNumber = @PhoneNumber AND Id != @Id", conn);
                phoneCheckCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
                phoneCheckCmd.Parameters.AddWithValue("@Id", id);
                var phoneInUse = Convert.ToInt32(phoneCheckCmd.ExecuteScalar()) > 0;

                if (phoneInUse)
                    return BadRequest(new { message = "Phone number already in use by another user", status = "error" });

                // Update user
                var updateCmd = new MySqlCommand(@"
            UPDATE Users
            SET FullName = @FullName,
                Email = @Email,
                Address = @Address,
                PhoneNumber = @PhoneNumber
            WHERE Id = @Id", conn);

                updateCmd.Parameters.AddWithValue("@FullName", dto.FullName);
                updateCmd.Parameters.AddWithValue("@Email", dto.Email);
                updateCmd.Parameters.AddWithValue("@Address", dto.Address ?? "");
                updateCmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber ?? "");
                updateCmd.Parameters.AddWithValue("@Id", id);

                updateCmd.ExecuteNonQuery();

                return Ok(new { message = "User updated successfully", status = "success" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error updating user: " + ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating user", status = "error" });
            }
        }




        // Login

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var cmd = new MySqlCommand("SELECT Id, FullName, Email, PasswordHash, Role FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", dto.Email);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return Unauthorized(new { message = "Invalid email or password", status = "error" });

            string storedHash = reader["PasswordHash"].ToString();
            string inputHash = ComputeHash(dto.Password);

            if (storedHash != inputHash)
                return Unauthorized(new { message = "Invalid email or password", status = "error" });

            var userDto = new UserDto
            {
                Id = Convert.ToInt32(reader["Id"]),
                FullName = reader["FullName"].ToString(),
                Email = reader["Email"].ToString(),
                Role = reader["Role"].ToString()
            };

            // Generate JWT Token
            var token = GenerateJwtToken(userDto.Id, userDto.Email, userDto.Role);

            return Ok(new
            {
                token,
                user = userDto,
                message = "Login successful",
                status = "success"
            });
        }


        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        private string GenerateJwtToken(int id, string email, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var cmd = new MySqlCommand("SELECT Id, FullName, Email, Address, PhoneNumber, Role FROM Users WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return NotFound();

            var user = new
            {
                Id = reader["Id"],
                FullName = reader["FullName"],
                Email = reader["Email"],
                Address = reader["Address"],
                PhoneNumber = reader["PhoneNumber"],
                Role = reader["Role"]
            };

            return Ok(user);
        }


    }
}
