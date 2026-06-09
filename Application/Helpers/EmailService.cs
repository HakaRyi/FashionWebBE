using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MimeKit;

namespace Application.Helpers
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerificationEmail(string toEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));
            }

            var emailSettings = _config.GetSection("EmailSettings");

            var smtpServer = emailSettings["SmtpServer"];
            var portString = emailSettings["Port"];
            var senderEmail = emailSettings["SenderEmail"];
            var password = emailSettings["Password"];
            var senderName = emailSettings["SenderName"] ?? "Wapo Support Center";

            if (string.IsNullOrWhiteSpace(smtpServer) ||
                string.IsNullOrWhiteSpace(portString) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("EmailSettings are not fully configured in application.");
            }

            int port = int.Parse(portString);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Wapo account verification code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildVerificationEmailBody(code)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                bool useSsl = (port == 465);

                await client.ConnectAsync(smtpServer, port, useSsl);

                await client.AuthenticateAsync(senderEmail, password);

                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"SMTP MailKit Error: {ex.Message}", ex);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        private static string BuildVerificationEmailBody(string code)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f6f6f6; padding: 24px;'>
                <div style='max-width: 520px; margin: auto; background-color: #ffffff; padding: 24px; border-radius: 12px;'>
                    <h2 style='color: #111111;'>Welcome to Wapo</h2>

                    <p style='font-size: 15px; color: #333333;'>
                        Please use the verification code below to complete your registration.
                    </p>

                    <div style='font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #111111; margin: 24px 0;'>
                        {code}
                    </div>

                    <p style='font-size: 14px; color: #555555;'>
                        This code will expire soon. If you did not create this account, you can ignore this email.
                    </p>

                    <p style='font-size: 14px; color: #777777; margin-top: 24px;'>
                        Wapo Support Center
                    </p>
                </div>
            </body>
            </html>";
        }
    }
}