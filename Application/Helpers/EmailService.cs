using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task SendVerificationEmail(string toEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));
            }

            var emailSettings = _config.GetSection("EmailSettings");
            var apiKey = emailSettings["ApiKey"];
            var senderEmail = emailSettings["SenderEmail"];
            var senderName = emailSettings["SenderName"] ?? "Wapo Support Center";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("Brevo API Key or Sender Email is not configured.");
            }

            var body = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject = "Wapo account verification code",
                htmlContent = BuildVerificationEmailBody(code)
            };

            var json = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey); 
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Brevo API Error. Status: {(int)response.StatusCode}. Response: {responseContent}"
                );
            }
        }

        private static string BuildVerificationEmailBody(string code)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset='UTF-8'></head>
            <body style='font-family: Arial, sans-serif; background-color: #f6f6f6; padding: 24px;'>
                <div style='max-width: 520px; margin: auto; background-color: #ffffff; padding: 24px; border-radius: 12px;'>
                    <h2 style='color: #111111;'>Welcome to Wapo</h2>
                    <p style='font-size: 15px; color: #333333;'>Please use the verification code below to complete your registration.</p>
                    <div style='font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #111111; margin: 24px 0;'>{code}</div>
                    <p style='font-size: 14px; color: #555555;'>This code will expire soon. If you did not create this account, you can ignore this email.</p>
                    <p style='font-size: 14px; color: #777777; margin-top: 24px;'>Wapo Support Center</p>
                </div>
            </body>
            </html>";
        }
    }
}