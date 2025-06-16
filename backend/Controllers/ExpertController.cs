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
    public class ExpertController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public ExpertController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            string query = "SELECT * FROM dbo.expert";
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
                Id = Convert.ToInt32(row["Id"]),
                Nom_et_prénom = row["Nom_et_prénom"]?.ToString(),
                Domaine_dexpertise = row["Domaine_dexpertise"]?.ToString(),
                Années_dexpérience = row["Années_dexpérience"]?.ToString(),
                Téléphone = row["Téléphone"]?.ToString(),
                Email = row["Email"]?.ToString(),
                Certifications_clés = row["Certifications_clés"]?.ToString(),
                Formation_principale = row["Formation_principale"]?.ToString(),
                role = row["role"]?.ToString(),
                password = row["password"]?.ToString(),
                cv = row["cv"]?.ToString(),



            }).ToList();

            return new JsonResult(actes);
        }



        [HttpPost("Add_Expert")]
        public async Task<IActionResult> AddExpert([FromForm] Expert log, [FromForm] IFormFile cv)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    // Check if the email already exists
                    var checkEmailQuery = "SELECT COUNT(*) FROM dbo.expert WHERE Email = @Email";
                    using (var checkCmd = new SqlCommand(checkEmailQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", log.Email);
                        int emailExists = (int)await checkCmd.ExecuteScalarAsync();
                        if (emailExists > 0)
                        {
                            return Conflict(new { message = "Email already exists." });
                        }
                    }

                    // Handle file upload
                    string fileName = null;
                    string OriginalFileName = null;
                    if (cv != null && cv.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),"cv");
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

                
                    var insertQuery = @"
                INSERT INTO dbo.expert 
                    (Nom_et_prénom, Domaine_dexpertise, Années_dexpérience, Téléphone, Email, Certifications_clés, Formation_principale, role, password , cv) 
                VALUES 
                    (@Nom_et_prénom, @Domaine_dexpertise, @Annee_exp, @Téléphone, @Email, @Certifications_clés, @Formation_principale, @role, @password , @cv)";

                    using (var insertCmd = new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@Nom_et_prénom", log.Nom_et_prénom);
                        insertCmd.Parameters.AddWithValue("@Domaine_dexpertise", log.Domaine_dexpertise);
                        insertCmd.Parameters.AddWithValue("@Annee_exp", log.Années_dexpérience);
                        insertCmd.Parameters.AddWithValue("@Téléphone", log.Téléphone);
                        insertCmd.Parameters.AddWithValue("@Email", log.Email);
                        insertCmd.Parameters.AddWithValue("@Certifications_clés", log.Certifications_clés);
                        insertCmd.Parameters.AddWithValue("@Formation_principale", log.Formation_principale);
                        insertCmd.Parameters.AddWithValue("@role", log.role);
                        insertCmd.Parameters.AddWithValue("@password", log.password);
                        insertCmd.Parameters.AddWithValue("@cv", (object)OriginalFileName ?? DBNull.Value); 

                        int result = await insertCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Expert created successfully." });
                        }

                        return BadRequest(new { Message = "Expert creation failed." });
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

        [HttpPut("Update_Expert")]
        public async Task<IActionResult> UpdateExpert([FromForm] Expert expert, [FromForm] IFormFile cv)

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

                    var updateQuery = new StringBuilder(@"

              UPDATE dbo.expert
                SET 
                    Nom_et_prénom = @Nom_et_prénom,
                    Domaine_dexpertise = @Domaine_dexpertise,
                    Années_dexpérience = @Années_dexpérience,
                    Téléphone = @Téléphone,
                    Email = @Email,
                    Certifications_clés = @Certifications_clés,
                    Formation_principale = @Formation_principale,
                    password = @password");
      

                    if (OriginalFileName != null)
                    {
                        updateQuery.Append(", cv = @cv");
                    }

                    updateQuery.Append(" WHERE Id = @Id");

                    using (var updateCmd = new SqlCommand(updateQuery.ToString(), con))
                    {
                        updateCmd.Parameters.AddWithValue("@Id", expert.Id);
                        updateCmd.Parameters.AddWithValue("@Nom_et_prénom", expert.Nom_et_prénom);
                        updateCmd.Parameters.AddWithValue("@Domaine_dexpertise", expert.Domaine_dexpertise);
                        updateCmd.Parameters.AddWithValue("@Années_dexpérience", expert.Années_dexpérience);
                        updateCmd.Parameters.AddWithValue("@Téléphone", expert.Téléphone);
                        updateCmd.Parameters.AddWithValue("@Email", expert.Email);
                        updateCmd.Parameters.AddWithValue("@Certifications_clés", expert.Certifications_clés);
                        updateCmd.Parameters.AddWithValue("@Formation_principale", expert.Formation_principale);
                        updateCmd.Parameters.AddWithValue("@password", expert.password);

                        if (OriginalFileName != null)
                        {
                            updateCmd.Parameters.AddWithValue("@cv", OriginalFileName);
                        }

                        int result = await updateCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Expert updated successfully." });
                        }

                        return NotFound(new { Message = "Expert not found or update failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpDelete("Delete_Expert")]
        public async Task<IActionResult> DeleteExpert([FromQuery] int id)
        {
            try
            {
                using (var con = new SqlConnection(_configuration.GetConnectionString("dbConn")))
                {
                    await con.OpenAsync();

                    var deleteQuery = "DELETE FROM dbo.expert WHERE Id = @Id";
                    using (var deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", id);

                        int result = await deleteCmd.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            return Ok(new { Message = "Expert deleted successfully." });
                        }

                        return NotFound(new { Message = "Expert not found or already deleted." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }


        [HttpPost("match")]
        public async Task<JsonResult> MatchCandidates([FromBody] MatchRequest request)
        {
            string query = "SELECT * FROM dbo.expert";
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
                Id = Convert.ToInt32(row["Id"]),
                Text = $"{row["Domaine_dexpertise"]} {row["Certifications_clés"]} {row["Formation_principale"]} experience_{row["Années_dexpérience"]}"
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
