using System.Text;
using MediaMonitoring.Data;
using MediaMonitoring.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.EntityFrameworkCore;

namespace MediaMonitoring.Services
{
    public class NotificationService
    {
        private readonly ConfigService _configService;
        private readonly MediaMonitoringContext _context;

        public NotificationService(ConfigService configService, MediaMonitoringContext context)
        {
            _configService = configService;
            _context = context;
        }

        public async Task<bool> SendEmailAlertAsync(string toEmail, string subject, string body, AlertRule? triggeredRule = null)
        {
            try
            {
                var smtpServer = await _configService.GetValueAsync("SmtpServer", "smtp.gmail.com");
                var smtpPort = int.Parse(await _configService.GetValueAsync("SmtpPort", "587"));
                var smtpUser = await _configService.GetValueAsync("SmtpUsername", "");
                var smtpPassword = await _configService.GetValueAsync("SmtpPassword", "");
                var fromEmail = await _configService.GetValueAsync("FromEmail", smtpUser);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Media Monitoring System", fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = $"[ALERT] {subject}";

                var htmlBody = $@"<!DOCTYPE html><html><body>
                    <h2>{subject}</h2>
                    {(triggeredRule != null ? $"<p><strong>Rule:</strong> {triggeredRule.Keyword}</p>" : "")}
                    <p>{body}</p>
                </body></html>";

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody, TextBody = $"{subject}\n\n{body}" };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                
                if (!string.IsNullOrEmpty(smtpUser))
                    await client.AuthenticateAsync(smtpUser, smtpPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSlackNotificationAsync(string messageText, AlertRule? rule = null)
        {
            try
            {
                var webhookUrl = await _configService.GetValueAsync("SlackWebhookUrl", "");
                if (string.IsNullOrEmpty(webhookUrl)) return false;

                var slackPayload = new { text = $"🚨 Alert: {messageText}" };

                using var httpClient = new HttpClient();
                var json = System.Text.Json.JsonSerializer.Serialize(slackPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(webhookUrl, content);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Slack error: {ex.Message}");
                return false;
            }
        }

        public async Task SendAlertNotificationAsync(AlertRule rule, MediaPost post)
        {
            rule.TriggerCount++;
            await _context.SaveChangesAsync();

            var subject = $"Alert: '{rule.Keyword}' in {post.Source}";
            var body = $"<p>Source: {post.Source}<br/>Content: {post.Content}</p>";

            if (!string.IsNullOrEmpty(rule.NotificationEmail))
                await SendEmailAlertAsync(rule.NotificationEmail, subject, body, rule);

            var adminEmail = await _configService.GetValueAsync("AdminEmail", "");
            if (!string.IsNullOrEmpty(adminEmail) && adminEmail != rule.NotificationEmail)
                await SendEmailAlertAsync(adminEmail, subject, body, rule);

            await SendSlackNotificationAsync($"{rule.Keyword} found in {post.Source}", rule);
        }
    }
}