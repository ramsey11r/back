using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using System;
using backend.models;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet]
        public async Task<JsonResult> Get()
        {
            string query = "SELECT * FROM dbo.login";
            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("dbConn");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                {
                    table.Load(myReader);
                }
            }

            var actes = table.AsEnumerable().Select(row => new
            {
                id = Convert.ToInt32(row["id"]),
                email = row["email"]?.ToString(),
                password = row["password"]?.ToString(),
                role = row["role"]?.ToString(),
                adress = row["adress"]?.ToString(),
                tel = row["tel"]?.ToString(),
                besoin = row["besoin"]?.ToString(),
                solution = row["solution"]?.ToString(),
                priorite = row["priorite"]?.ToString()
            }).ToList();

            return new JsonResult(actes); 
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] account log)
        {
            if (string.IsNullOrWhiteSpace(log.email) || string.IsNullOrWhiteSpace(log.password))
            {
                return BadRequest(new { Message = "Email and password are required." });
            }

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();
                   
                    var checkQuery = "SELECT COUNT(*) FROM dbo.login WHERE email = @Email";
                    using (var checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", log.email);
                        int existing = (int)await checkCmd.ExecuteScalarAsync();

                        if (existing > 0)
                        {
                            return Conflict(new { Message = "Email already registered." });
                        }
                    }                
                    
                    var insertQuery = "INSERT INTO dbo.login (email, password,role,companyName,tel,besoin,solution,priorite) VALUES (@Email, @Password , @Role ,@CompanyName , @Tel , @Besoin , @Solution , @Priorite)";
                    using (var insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@Email", log.email);
                        insertCmd.Parameters.AddWithValue("@Password", log.password);
                        insertCmd.Parameters.AddWithValue("@Role", log.role);
                        insertCmd.Parameters.AddWithValue("@CompanyName", log.companyName);
                        insertCmd.Parameters.AddWithValue("@Tel", log.tel);
                        insertCmd.Parameters.AddWithValue("@Besoin", log.besoin);
                        insertCmd.Parameters.AddWithValue("@Solution", log.solution);
                        insertCmd.Parameters.AddWithValue("@Priorite", log.priorite);


                        int result = await insertCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Account created successfully." });
                        }

                        return BadRequest(new { Message = "Account creation failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Login([FromBody] account credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.email) || string.IsNullOrWhiteSpace(credentials.password))
            {
                return BadRequest(new { Message = "Email and password are required." });
            }

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    // First, check login table
                    var loginQuery = "SELECT role FROM dbo.login WHERE email = @Email AND password = @Password";
                    using (var cmd = new SqlCommand(loginQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", credentials.email);
                        cmd.Parameters.AddWithValue("@Password", credentials.password);

                        var role = await cmd.ExecuteScalarAsync();

                        if (role != null)
                        {
                            return Ok(new { Message = "Login successful", Role = role.ToString() });
                        }
                    }

                    // If not found in login, check expert table
                    var expertQuery = "SELECT role FROM dbo.expert WHERE email = @Email AND password = @Password";
                    using (var cmdExpert = new SqlCommand(expertQuery, con))
                    {
                        cmdExpert.Parameters.AddWithValue("@Email", credentials.email);
                        cmdExpert.Parameters.AddWithValue("@Password", credentials.password);

                        var expertRole = await cmdExpert.ExecuteScalarAsync();

                        if (expertRole != null)
                        {
                            return Ok(new { Message = "Login successful", Role = expertRole.ToString() });
                        }
                    }

                    // If not found in either table
                    return Unauthorized(new { Message = "Invalid email or password." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }


    }
}
