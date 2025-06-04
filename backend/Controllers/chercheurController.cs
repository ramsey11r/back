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
    public class chercheurController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public chercheurController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            string query = "SELECT * FROM dbo.CHERCHEURS";
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
                Nom_prenom = row["Nom_prenom"]?.ToString(),
                fonction = row["fonction"]?.ToString(),
                specialite = row["specialite"]?.ToString(),
                Mail = row["Mail"]?.ToString(),
                telephone = row["telephone"]?.ToString(),
                Mots_cles = row["Mots_cles"]?.ToString(),
                titre_dhabilitation_universitaire = row["titre_dhabilitation_universitaire"]?.ToString(),
                Titre_de_these = row["Titre_de_these"]?.ToString(),
                Competences = row["Competences"]?.ToString(),
                Expertise = row["Expertise"]?.ToString(),
                Laboratoire_de_recherche = row["Laboratoire_de_recherche"]?.ToString(),

            }).ToList();

            return new JsonResult(actes);
        }

        [HttpPost("Add_Chercheur")]
        public async Task<IActionResult> AddChercheur([FromBody] chercheurs log)
        {

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var insertQuery = "INSERT INTO dbo.CHERCHEURS (Nom_prenom, fonction,specialite,Mail,telephone,Mots_cles,titre_dhabilitation_universitaire,Titre_de_these,Competences,Expertise,Laboratoire_de_recherche) VALUES (@Nom_prenom, @fonction , @specialite ,@Mail , @telephone , @Mots_cles , @titre_dhabilitation_universitaire,@Titre_de_these,@Competences,@Expertise,@Laboratoire_de_recherche)";
                    using (var insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@Nom_prenom", log.Nom_prenom);
                        insertCmd.Parameters.AddWithValue("@fonction", log.fonction);
                        insertCmd.Parameters.AddWithValue("@specialite", log.specialite);
                        insertCmd.Parameters.AddWithValue("@Mail", log.Mail);
                        insertCmd.Parameters.AddWithValue("@telephone", log.telephone);
                        insertCmd.Parameters.AddWithValue("@Mots_cles", log.Mots_cles);
                        insertCmd.Parameters.AddWithValue("@titre_dhabilitation_universitaire", log.titre_dhabilitation_universitaire);
                        insertCmd.Parameters.AddWithValue("@Titre_de_these", log.Titre_de_these);
                        insertCmd.Parameters.AddWithValue("@Competences", log.Competences);
                        insertCmd.Parameters.AddWithValue("@Expertise", log.Expertise);
                        insertCmd.Parameters.AddWithValue("@Laboratoire_de_recherche", log.Laboratoire_de_recherche);






                        int result = await insertCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "CHERCHEURS created successfully." });
                        }

                        return BadRequest(new { Message = "CHERCHEURS creation failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpPut("Update_chercheur")]
        public async Task<IActionResult> UpdateChercheur([FromBody] chercheurs c)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var updateQuery = @"
                UPDATE dbo.CHERCHEURS
                SET 
                    Nom_prenom = @Nom_prenom,
                    fonction = @fonction,
                    specialite = @specialite,
                    Mail = @Mail,
                    telephone = @telephone,
                    Mots_cles = @Mots_cles,
                    titre_dhabilitation_universitaire = @titre_dhabilitation_universitaire,
                    Titre_de_these = @Titre_de_these,
                    Competences = @Competences,
                    Expertise = @Expertise,
                    Laboratoire_de_recherche = @Laboratoire_de_recherche

                WHERE id = @id";

                    using (var updateCmd = new SqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@id", c.id);
                        updateCmd.Parameters.AddWithValue("@Nom_prenom", c.Nom_prenom);
                        updateCmd.Parameters.AddWithValue("@fonction", c.fonction);
                        updateCmd.Parameters.AddWithValue("@specialite", c.specialite);
                        updateCmd.Parameters.AddWithValue("@Mail", c.Mail);
                        updateCmd.Parameters.AddWithValue("@telephone", c.telephone);
                        updateCmd.Parameters.AddWithValue("@Mots_cles", c.Mots_cles);
                        updateCmd.Parameters.AddWithValue("@titre_dhabilitation_universitaire", c.titre_dhabilitation_universitaire);
                        updateCmd.Parameters.AddWithValue("@Titre_de_these", c.Titre_de_these);
                        updateCmd.Parameters.AddWithValue("@Competences", c.Competences);
                        updateCmd.Parameters.AddWithValue("@Expertise", c.Expertise);
                        updateCmd.Parameters.AddWithValue("@Laboratoire_de_recherche", c.Laboratoire_de_recherche);



                        int result = await updateCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "CHERCHEURS updated successfully." });
                        }

                        return NotFound(new { Message = "CHERCHEURS not found or update failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpDelete("Delete_chercheur")]
        public async Task<IActionResult> DeleteChercheur([FromQuery] int id)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var deleteQuery = "DELETE FROM dbo.CHERCHEURS WHERE id = @id";
                    using (var deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);

                        int result = await deleteCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "chercheur deleted successfully." });
                        }

                        return NotFound(new { Message = "chercheur not found or already deleted." });
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
