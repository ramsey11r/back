using backend.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class brevetsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public brevetsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            string query = "SELECT * FROM dbo.Brevets";
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
                ID = Convert.ToInt32(row["ID"]),
                Numero = row["Numero"]?.ToString(),
                Titre = row["Titre"]?.ToString(),
                Annee = row["Annee"]?.ToString(),
                Type = row["Type"]?.ToString(),
                

            }).ToList();

            return new JsonResult(actes);
        }

        [HttpPost("Add_Brevets")]
        public async Task<IActionResult> AddBrevets([FromBody] brevet log)
        {

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var insertQuery = "INSERT INTO dbo.Brevets (Numero, Titre,Annee,Type) VALUES (@Numero, @Titre , @Annee ,@Type)";
                    using (var insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@Numero", log.Numero);
                        insertCmd.Parameters.AddWithValue("@Titre", log.Titre);
                        insertCmd.Parameters.AddWithValue("@Annee", log.Annee);
                        insertCmd.Parameters.AddWithValue("@Type", log.Type);
                      






                        int result = await insertCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Brevets created successfully." });
                        }

                        return BadRequest(new { Message = "Brevets creation failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpPut("Update_Brevets")]
        public async Task<IActionResult> UpdateBrevets([FromBody] brevet c)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var updateQuery = @"
                UPDATE dbo.Brevets
                SET 
                    Numero = @Numero,
                    Titre = @Titre,
                    Annee = @Annee,
                    Type = @Type

                WHERE ID = @ID";

                    using (var updateCmd = new SqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@ID", c.ID);
                        updateCmd.Parameters.AddWithValue("@Numero", c.Numero);
                        updateCmd.Parameters.AddWithValue("@Titre", c.Titre);
                        updateCmd.Parameters.AddWithValue("@Annee", c.Annee);
                        updateCmd.Parameters.AddWithValue("@Type", c.Type);

                        int result = await updateCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Brevets updated successfully." });
                        }

                        return NotFound(new { Message = "Brevets not found or update failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpDelete("Delete_Brevets")]
        public async Task<IActionResult> DeleteBrevets([FromQuery] int id)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var deleteQuery = "DELETE FROM dbo.Brevets WHERE ID = @ID";
                    using (var deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@ID", id);

                        int result = await deleteCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Brevets deleted successfully." });
                        }

                        return NotFound(new { Message = "Brevets not found or already deleted." });
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
