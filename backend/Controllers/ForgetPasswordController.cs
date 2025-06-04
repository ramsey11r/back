using backend.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols;
using System.Data;
using System.Net.Http;
using System.Net;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgetPasswordController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ForgetPasswordController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword([FromBody] foPas model)
        {
            string connectionString = _configuration.GetConnectionString("dbConn");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string resetToken = GeneratePasswordResetToken();

                string query = "INSERT INTO forgetPass (email, resetToken, ExpiryDate) VALUES (@email, @ResetToken, @ExpiryDate)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", model.email);
                    command.Parameters.AddWithValue("@ResetToken", resetToken);
                    command.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddHours(1).AddMinutes(10));
                    command.ExecuteNonQuery();
                }

                SendPasswordResetEmail(model.email, resetToken);
                return Ok(new { message = "Password reset instructions sent to your email." });
            }
        }

        private string GeneratePasswordResetToken()
        {
            byte[] randomBytes = new byte[32];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace(" ", "");
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword([FromBody] ResetPassword resetPasswordModel)
        {
            string connectionString = _configuration.GetConnectionString("dbConn");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                DateTime dateNow = DateTime.UtcNow.AddHours(1);

                string query = "SELECT email FROM forgetPass WHERE ExpiryDate > @DateNow";

                Console.WriteLine("resetToken: " + resetPasswordModel.FoPasModel.resetToken);
                Console.WriteLine("DateNow: " + dateNow);

                // Optionally, construct the query with parameters for debugging purposes (not recommended for production)
                string debugQuery = $"SELECT email FROM forgetPass WHERE ExpiryDate > '{dateNow}'";
                Console.WriteLine("Debug Query: " + debugQuery);
                using (SqlCommand selectCommand = new SqlCommand(query, connection))
                {


                    selectCommand.Parameters.AddWithValue("@ResetToken", resetPasswordModel.FoPasModel.resetToken);
                    selectCommand.Parameters.AddWithValue("@DateNow", dateNow);
                    var email = selectCommand.ExecuteScalar() as string;

                    if (email != null)
                    {
                        string updateQuery = "UPDATE login SET password = @NewPassword WHERE email = @email";
                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@NewPassword", resetPasswordModel.UsersModel.password);
                            updateCommand.Parameters.AddWithValue("@email", email);
                            updateCommand.ExecuteNonQuery();
                        }

                        string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @ResetToken";
                        using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@ResetToken", resetPasswordModel.FoPasModel.resetToken);
                            deleteCommand.ExecuteNonQuery();
                        }

                        return Ok(new { message = "Password reset successful." });
                    }
                    else
                    {
                        string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @ResetToken";
                        using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@ResetToken", resetPasswordModel.FoPasModel.resetToken);
                            deleteCommand.ExecuteNonQuery();
                        }
                        return BadRequest("Invalid or expired token.");
                    }
                }
            }
        }

        [HttpPost("UpdateUserPassword")]
        public IActionResult updatePassword([FromBody] ResetPassword resetPasswordModel)
        {
            string connectionString = _configuration.GetConnectionString("dbConn");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                DateTime dateNow = DateTime.UtcNow.AddHours(1);
                string receivedToken = resetPasswordModel.FoPasModel.resetToken?.Replace(" ", "+");

                // Check if token exists and is not expired
                string query = "SELECT email FROM forgetPass WHERE resetToken = @resetToken AND ExpiryDate > @DateNow";
                using (SqlCommand selectCommand = new SqlCommand(query, connection))
                {
                    selectCommand.Parameters.Add("@resetToken", SqlDbType.NVarChar, 255).Value = receivedToken;
                    selectCommand.Parameters.AddWithValue("@DateNow", dateNow);

                    var email = selectCommand.ExecuteScalar() as string;

                    if (email != null)
                    {
                        string updateQuery = "UPDATE login SET password = @NewPassword WHERE Email = @Email";
                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@NewPassword", resetPasswordModel.UsersModel.password);
                            updateCommand.Parameters.AddWithValue("@Email", email);
                            updateCommand.ExecuteNonQuery();
                        }

                        // Delete used token
                        string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @resetToken";
                        using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@resetToken", resetPasswordModel.FoPasModel.resetToken);
                            deleteCommand.ExecuteNonQuery();
                        }

                        return Ok("Password reset successful.");
                    }
                    else
                    {
                        // Remove invalid/expired token
                        string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @resetToken";
                        using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@resetToken", resetPasswordModel.FoPasModel.resetToken);
                            deleteCommand.ExecuteNonQuery();
                        }

                        return BadRequest("Invalid or expired token.");
                    }
                }
            }
        }


        private void SendPasswordResetEmail(string email, string token)
        {
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUsername = "ramzirihane0@gmail.com";
            string smtpPassword = "huin clgo fnvt tiwg";
            string senderEmail = email;


            using (SmtpClient smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = smtpPort;
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true; // Enable SSL if required

                using (MailMessage mailMessage = new MailMessage(senderEmail, email))
                {
                    mailMessage.Subject = "Password Reset Request";
                    mailMessage.Body = $"To reset your password, click the following link: http://localhost:4200/reset-password?token={token}";
                    mailMessage.IsBodyHtml = true;

                    smtpClient.Send(mailMessage);
                }
            }
        }
    }
}
