using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

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
            var emailSettings = _config.GetSection("EmailSettings");

            var smtpClient = new SmtpClient(emailSettings["SmtpServer"])
            {
                Port = int.Parse(emailSettings["Port"]!),
                Credentials = new NetworkCredential(emailSettings["SenderEmail"], emailSettings["Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"]!, emailSettings["SenderName"]),
                Subject = "Mã xác thực đăng ký Wado",
                Body = $"<h1>Xin chào!</h1><p>Mã xác thực của bạn là: <b style='font-size: 24px; color: blue;'>{code}</b></p><p>Mã này sẽ hết hạn sau 5 phút.</p>",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
