using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using SixEightFoundation.Models;

namespace SixEightFoundation.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string name, string email, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings emailSettings;
        private readonly ILogger<EmailService> logger;
        private readonly IWebHostEnvironment environment;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            this.emailSettings = emailSettings.Value;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> SendEmailAsync(string name, string email, string subject, string message)
        {
            try
            {
                using var smtpClient = new SmtpClient
                {
                    Host = environment.IsDevelopment() ? "localhost" : "smtp.gmail.com",
                    Port = environment.IsDevelopment() ? 25 : 587,
                    EnableSsl = !environment.IsDevelopment(),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!environment.IsDevelopment())
                {
                    smtpClient.Credentials = new NetworkCredential(
                        emailSettings.SmtpUsername,
                        emailSettings.SmtpPassword);
                }

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 20px auto; padding: 20px; }}
        .header {{ background-color: #37517e; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .field {{ margin-bottom: 15px; }}
        .label {{ font-weight: bold; color: #37517e; }}
        .message-box {{ background-color: white; padding: 15px; border-left: 4px solid #5c9f24; margin-top: 15px; }}
        .timestamp {{ 
            text-align: center;
            color: #888;
            font-size: 12px;
            margin-top: 20px;
            font-style: italic;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>New Message from {name}</h2>
        </div>
        <div class='content'>
            <div class='field'>
                <span class='label'>From:</span><br/>
                {email}
            </div>
            <div class='field'>
                <span class='label'>Subject:</span><br/>
                {subject}
            </div>
            <div class='field'>
                <span class='label'>Message:</span>
                <div class='message-box'>
                    {message.Replace(Environment.NewLine, "<br/>")}
                </div>
            </div>
            <div class='timestamp'>
                {DateTime.UtcNow.AddHours(-5).ToString("MMMM dd, yyyy 'at' h:mm tt")} EST
            </div>
        </div>
    </div>
</body>
</html>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(email),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(emailSettings.ToEmail);
                mailMessage.ReplyToList.Add(new MailAddress(email, name));

                logger.LogInformation($"Sending email to {emailSettings.ToEmail}");
                await smtpClient.SendMailAsync(mailMessage);
                logger.LogInformation("Email sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending email");
                return false;
            }
        }
    }
} 