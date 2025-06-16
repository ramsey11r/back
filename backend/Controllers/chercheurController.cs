using backend.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.ML.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;

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
                cv = row["cv"]?.ToString(),


            }).ToList();

            return new JsonResult(actes);
        }

        [HttpPost("Add_Chercheur")]
        public async Task<IActionResult> AddChercheur([FromForm] chercheurs log, [FromForm] IFormFile cv)
        {

            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    
                    string fileName = null;
                    string OriginalFileName = null;
                    if (cv != null && cv.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "cv");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        fileName = Guid.NewGuid().ToString() + Path.GetExtension(cv.FileName);

                        OriginalFileName = cv.FileName;
                        var filePath = Path.Combine(uploadsFolder, cv.FileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await cv.CopyToAsync(fileStream);
                        }
                    }

                    var insertQuery = "INSERT INTO dbo.CHERCHEURS (Nom_prenom, fonction,specialite,Mail,telephone,Mots_cles,titre_dhabilitation_universitaire,Titre_de_these,Competences,Expertise,Laboratoire_de_recherche , cv) VALUES (@Nom_prenom, @fonction , @specialite ,@Mail , @telephone , @Mots_cles , @titre_dhabilitation_universitaire,@Titre_de_these,@Competences,@Expertise,@Laboratoire_de_recherche , @cv)";
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
                        insertCmd.Parameters.AddWithValue("@cv", (object)OriginalFileName ?? DBNull.Value);








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



        [HttpGet("cv/{fileName}")]
        public IActionResult GetCV(string fileName)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "cv");
                var filePath = Path.Combine(folderPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "CV file not found." });
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while retrieving CV", error = ex.Message });
            }
        }


        [HttpPut("Update_chercheur")]
        public async Task<IActionResult> UpdateChercheur([FromForm] chercheurs c, [FromForm] IFormFile cv)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    string fileName = null;
                    string OriginalFileName = null;

                    if (cv != null && cv.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "cv");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        fileName = Guid.NewGuid().ToString() + Path.GetExtension(cv.FileName);
                        OriginalFileName = cv.FileName;
                        var filePath = Path.Combine(uploadsFolder, OriginalFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await cv.CopyToAsync(fileStream);
                        }
                    }

                    // Build the SQL query dynamically
                    var updateQuery = new StringBuilder(@"
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
                    Laboratoire_de_recherche = @Laboratoire_de_recherche");

                    if (OriginalFileName != null)
                    {
                        updateQuery.Append(", cv = @cv");
                    }

                    updateQuery.Append(" WHERE id = @id");

                    using (var updateCmd = new SqlCommand(updateQuery.ToString(), con))
                    {
                        updateCmd.Parameters.AddWithValue("@id", c.id);
                        updateCmd.Parameters.AddWithValue("@Nom_prenom", c.Nom_prenom ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@fonction", c.fonction ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@specialite", c.specialite ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Mail", c.Mail ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@telephone", c.telephone ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Mots_cles", c.Mots_cles ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@titre_dhabilitation_universitaire", c.titre_dhabilitation_universitaire ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Titre_de_these", c.Titre_de_these ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Competences", c.Competences ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Expertise", c.Expertise ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Laboratoire_de_recherche", c.Laboratoire_de_recherche ?? (object)DBNull.Value);


                        if (OriginalFileName != null)
                        {
                            updateCmd.Parameters.AddWithValue("@cv", OriginalFileName);
                        }

                        int result = await updateCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "CHERCHEUR updated successfully." });
                        }

                        return NotFound(new { Message = "CHERCHEUR not found or update failed." });
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


        [HttpPost("match-chercheur")]
        public async Task<JsonResult> MatchChercheur([FromBody] MatchRequest request)
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

            var candidates = table.AsEnumerable().Select(row => new {
                Id = Convert.ToInt32(row["id"]),
                Text = $"{row["fonction"]} {row["specialite"]} {row["Mots_cles"]} {row["Competences"]} {row["Expertise"]}"
             .ToLowerInvariant()
            }).ToList();

            var ranked = RankCandidatesWithMLNET(request.ProfileText.ToLowerInvariant(), candidates.Cast<dynamic>().ToList());
            return new JsonResult(ranked.OrderByDescending(x => x.Score).Take(3));
        }

        private List<CandidateWithScore> RankCandidatesWithMLNET(string companyText, List<dynamic> candidates)
        {
            var mlContext = new MLContext();

            // Build the list of candidate + company input
            var allInputs = candidates.Select(c => new CandidateInput { Text = c.Text }).ToList();
            allInputs.Insert(0, new CandidateInput { Text = companyText });

            var dataView = mlContext.Data.LoadFromEnumerable(allInputs);

            // Apply text transformation pipeline:
            // FeaturizeText performs both normalization (e.g., lowercasing, removing punctuation, tokenization)
            // and encoding (using TF-IDF vectorization).
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(CandidateInput.Text));
            var model = pipeline.Fit(dataView);
            var transformedData = model.Transform(dataView);

            // Extract vectors
            var featureColumn = transformedData.GetColumn<VBuffer<float>>(transformedData.Schema["Features"]);

            var allVectors = featureColumn.Select(v => v.DenseValues().ToArray()).ToList();

            var companyVector = allVectors[0];

            var results = new List<CandidateWithScore>();

            for (int i = 1; i < allVectors.Count; i++)
            {
                float score = CosineSimilarity(companyVector, allVectors[i]);

                results.Add(new CandidateWithScore
                {
                    Id = candidates[i - 1].Id,
                    Text = candidates[i - 1].Text,
                    Score = score
                });
            }

            return results;
        }

        private float CosineSimilarity(float[] vecA, float[] vecB)
        {
            float dot = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < vecA.Length; i++)
            {
                dot += vecA[i] * vecB[i];
                normA += vecA[i] * vecA[i];
                normB += vecB[i] * vecB[i];
            }

            return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-10);
        }
    }
}
