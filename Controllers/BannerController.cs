using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using GyanSagarNew.Model;

namespace GyanSagarNew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BannerController : Controller
    {
        private readonly string _connectionString;

        public BannerController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("MySqlConnection");
        }

        // API to create a new banner message
        [HttpPost("create")]
        public IActionResult CreateBanner([FromBody] BannerDto banner)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var transaction = conn.BeginTransaction();

               
                var deleteCmd = new MySqlCommand("DELETE FROM Banners", conn, transaction);
                deleteCmd.ExecuteNonQuery();

                var cmd = new MySqlCommand(@"
                    INSERT INTO Banners (Message, IsActive, StartDate, EndDate) 
                    VALUES (@Message, @IsActive, @StartDate, @EndDate)", conn);

                cmd.Parameters.AddWithValue("@Message", banner.Message);
                cmd.Parameters.AddWithValue("@IsActive", banner.IsActive);
                cmd.Parameters.AddWithValue("@StartDate", banner.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", banner.EndDate);

                cmd.ExecuteNonQuery();

                transaction.Commit();

                return Ok(new { message = "Banner created successfully." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpDelete("delete")]
        public IActionResult DeleteBanner()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand("DELETE FROM Banners WHERE IsActive = 1", conn); // Deleting the active banner
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Previous banner deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }


        // API to retrieve the current active banner
        [HttpGet("current")]
        public IActionResult GetCurrentBanner()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
                    SELECT * FROM Banners 
                    WHERE IsActive = TRUE AND StartDate <= NOW() AND EndDate >= NOW()", conn);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var banner = new BannerDto
                    {
                        BannerID = Convert.ToInt32(reader["BannerID"]),
                        Message = reader["Message"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        StartDate = Convert.ToDateTime(reader["StartDate"]),
                        EndDate = Convert.ToDateTime(reader["EndDate"]),
                    };
                    return Ok(banner);
                }

                return NotFound("No active banner found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }
    }

    
}
