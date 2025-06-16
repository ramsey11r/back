using backend.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ContratController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            string query = "SELECT * FROM dbo.contrat";
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
                expert_email = row["expert_email"]?.ToString(),
                contrat_name = row["contrat_name"]?.ToString(),
                contrat_send_date = row["contrat_send_date"]?.ToString(),


            }).ToList();

            return new JsonResult(actes);
        }

        [HttpPost("Add_contrat")]
        public async Task<IActionResult> Addcontrat([FromBody] contrat log)
        {

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var insertQuery = "INSERT INTO dbo.contrat (expert_email, contrat_name,contrat_send_date) VALUES (@expert_email, @contrat_name , @contrat_send_date)";
                    using (var insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@expert_email", log.expert_email);
                        insertCmd.Parameters.AddWithValue("@contrat_name", log.contrat_name);
                        insertCmd.Parameters.AddWithValue("@contrat_send_date", log.contrat_send_date);


                        int result = await insertCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "contract created successfully." });
                        }

                        return BadRequest(new { Message = "contract creation failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }
    }
}
