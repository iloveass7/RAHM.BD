using Microsoft.Data.SqlClient;
using RAHM.BD.Models;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace RAHM.BD.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDb _db;
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;

        public NotificationService(IDb db, IConfiguration config, ILogger<NotificationService> logger, HttpClient httpClient)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;

            // Initialize Twilio
            var accountSid = _config["Twilio:AccountSid"];
            var authToken = _config["Twilio:AuthToken"];
            //TwilioClient.Init(accountSid, authToken);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Format phone number for Twilio (E.164 format with +)
                //if (!phoneNumber.StartsWith("+"))
                //{
                //    if (phoneNumber.StartsWith("01") && phoneNumber.Length == 11)
                //    {
                //        phoneNumber = "+88" + phoneNumber; // Bangladesh numbers
                //    }
                //    else if (phoneNumber.StartsWith("8801"))
                //    {
                //        phoneNumber = "+" + phoneNumber; // Already has country code
                //    }
                //    else
                //    {
                //        phoneNumber = "+88" + phoneNumber; // Assume Bangladesh number
                //    }
                //}

                // Use the correct configuration key that matches your appsettings.json
                var fromNumber = _config["Twilio:PhoneNumber"]; // Changed from "FromNumber" to "PhoneNumber"

                // Add validation for PhoneNumber
                if (string.IsNullOrEmpty(fromNumber))
                {
                    _logger.LogError("Twilio PhoneNumber is not configured");
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(fromNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                if (messageResource.Status == MessageResource.StatusEnum.Failed ||
                    messageResource.Status == MessageResource.StatusEnum.Undelivered)
                {
                    _logger.LogError("Twilio SMS failed with status: {Status}, Error: {ErrorMessage}",
                        messageResource.Status, messageResource.ErrorMessage);
                    return false;
                }

                _logger.LogInformation("Twilio SMS sent successfully. Message SID: {MessageSid}, Status: {Status}",
                    messageResource.Sid, messageResource.Status);
                return true;
            }
            catch (Twilio.Exceptions.ApiException apiEx)
            {
                _logger.LogError(apiEx, "Twilio API error sending SMS to {PhoneNumber}: {Error}",
                    phoneNumber, apiEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}: {Error}", phoneNumber, ex.Message);
                return false;
            }
        }


        public async Task<bool> SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                using var smtpClient = new SmtpClient(_config["Email:SmtpServer"])
                {
                    Port = int.Parse(_config["Email:Port"] ?? "587"),
                    Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"]),
                    EnableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true"),
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config["Email:FromAddress"] ?? "", "RAHM BD Health System"),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", email);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {Email}: {Error}", email, smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Error}", email, ex.Message);
                return false;
            }
        }

        public async Task<NotificationResult> SendBulkNotificationAsync(NotificationRequest request)
        {
            var result = new NotificationResult();

            try
            {
                // Get target users based on criteria
                var users = await GetTargetUsersAsync(request.Division, request.District, request.SendToAll);
                result.TotalTargeted = users.Count;

                if (!users.Any())
                {
                    result.Errors.Add("No users found matching the criteria");
                    return result;
                }

                // For SMS, send individually
                if (request.Channel == NotificationChannel.SMS || request.Channel == NotificationChannel.Both)
                {
                    var smsTasks = new List<Task<bool>>();
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.MobileNo))
                        {
                            smsTasks.Add(SendSmsAsync(user.MobileNo, request.Message));
                        }
                    }

                    var smsResults = await Task.WhenAll(smsTasks);
                    result.SuccessCount += smsResults.Count(r => r);
                    result.FailureCount += smsResults.Count(r => !r);
                }

                // For emails, send individually
                if (request.Channel == NotificationChannel.Email || request.Channel == NotificationChannel.Both)
                {
                    var emailTasks = new List<Task<bool>>();
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            emailTasks.Add(SendEmailAsync(user.Email, request.Title, request.Message));
                        }
                    }

                    var emailResults = await Task.WhenAll(emailTasks);
                    result.SuccessCount += emailResults.Count(r => r);
                    result.FailureCount += emailResults.Count(r => !r);
                }

                _logger.LogInformation("Bulk notification completed: {Success}/{Total} successful",
                    result.SuccessCount, result.TotalTargeted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification");
                result.Errors.Add($"System error: {ex.Message}");
            }

            return result;
        }

        private async Task<List<UserContact>> GetTargetUsersAsync(string? division, string? district, bool sendToAll)
        {
            string sql;
            List<SqlParameter> parameters = new();

            if (sendToAll)
            {
                sql = @"SELECT u.Email, u.MobileNo, u.Name FROM Users u";
            }
            else if (!string.IsNullOrEmpty(district))
            {
                sql = @"SELECT u.Email, u.MobileNo, u.Name FROM Users u 
                        INNER JOIN Locations l ON u.Id = l.UserId 
                        WHERE l.District = @District";
                parameters.Add(new SqlParameter("@District", district));
            }
            else if (!string.IsNullOrEmpty(division))
            {
                sql = @"SELECT u.Email, u.MobileNo, u.Name FROM Users u 
                        INNER JOIN Locations l ON u.Id = l.UserId 
                        WHERE l.Division = @Division";
                parameters.Add(new SqlParameter("@Division", division));
            }
            else
            {
                return new List<UserContact>();
            }

            return await _db.QueryAsync(sql, r => new UserContact
            {
                Email = r.GetString(0),
                MobileNo = r.GetString(1),
                Name = r.GetString(2)
            }, parameters.ToArray());
        }
    }

    public class UserContact
    {
        public string Email { get; set; } = "";
        public string MobileNo { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
