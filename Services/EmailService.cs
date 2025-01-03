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
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task<bool> SendEmailAsync(string name, string email, string subject, string message)
        {
            try
            {
                using (var smtpClient = new SmtpClient())
                {
                    if (_environment.IsDevelopment())
                    {
                        // local development
                        smtpClient.Host = "localhost";
                        smtpClient.Port = 25;
                        smtpClient.EnableSsl = false;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.UseDefaultCredentials = false;
                    }
                    else
                    {
                        // for production
                        smtpClient.Host = "smtp.gmail.com";
                        smtpClient.Port = 587;
                        smtpClient.EnableSsl = true;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(
                            _emailSettings.SmtpUsername,
                            _emailSettings.SmtpPassword);
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
        .footer {{ text-align: center; margin-top: 20px; font-size: 0.9em; color: #666; }}
        .timestamp {{ 
            text-align: center;
            color: #666;
            font-size: 0.9em;
            margin-bottom: 15px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>New Message from {name}</h2>
        </div>
        <div class='content'>
            <div class='timestamp'>
                {DateTime.UtcNow.AddHours(-5).ToString("MMMM dd, yyyy 'at' h:mm tt")} EST
            </div>
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
        </div>
    </div>
</body>
</html>";

                    var plainTextBody = $@"New Contact Form Submission

From: {name} ({email})
Subject: {subject}

Message:
{message}

---
This email was sent from the I 6:8 Foundation contact form.";

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(email),
                        Subject = subject,
                        IsBodyHtml = true,
                        Body = htmlBody
                    };

                    // Add plain text alternative
                    var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                    mailMessage.AlternateViews.Add(plainTextView);
                    mailMessage.AlternateViews.Add(htmlView);

                    mailMessage.To.Add(new MailAddress(_emailSettings.ToEmail));
                    mailMessage.ReplyToList.Add(new MailAddress(email, name));
                    mailMessage.Headers.Add("In-Reply-To", email);

                    _logger.LogInformation($"Attempting to send email to {_emailSettings.ToEmail} via {smtpClient.Host}:{smtpClient.Port}");
                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return false;
            }
        }
    }
} 