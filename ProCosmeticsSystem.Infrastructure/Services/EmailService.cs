using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        await SendAsync([to], subject, body);
    }

    public async Task SendAsync(List<string> to, string subject, string body)
    {
        var recipients = string.Join(", ", to);
        try
        {
            _logger.LogInformation("Preparing to send email to {To}: {Subject}", recipients, subject);
            var email = BuildMessage(to, subject, body);
            await SendWithRetryAsync(email);
            _logger.LogInformation("Email sent successfully to {To}: {Subject}", recipients, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", recipients, subject);
        }
    }

    public async Task SendWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string fileName)
    {
        try
        {
            var email = BuildMessage([to], subject, body);

            var builder = new BodyBuilder { HtmlBody = body };
            builder.Attachments.Add(fileName, attachment);
            email.Body = builder.ToMessageBody();

            await SendWithRetryAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {To}", to);
        }
    }

    private MimeMessage BuildMessage(List<string> to, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            _config["EmailSettings:SenderName"],
            _config["EmailSettings:SenderEmail"] ?? "noreply@procosmetics.me"));

        foreach (var recipient in to)
            email.To.Add(MailboxAddress.Parse(recipient));

        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };
        return email;
    }

    private async Task SendWithRetryAsync(MimeMessage email, int maxRetries = 1)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var smtp = new SmtpClient();
                var port = _config.GetValue<int>("EmailSettings:SmtpPort");
                var useSsl = _config.GetValue<bool>("EmailSettings:UseSsl");

                // Port 465 uses implicit SSL (SslOnConnect), other ports use StartTls
                var socketOptions = port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

                await smtp.ConnectAsync(
                    _config["EmailSettings:SmtpHost"],
                    port,
                    socketOptions);

                var username = _config["EmailSettings:Username"];
                if (!string.IsNullOrEmpty(username))
                    await smtp.AuthenticateAsync(username, _config["EmailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                return;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                _logger.LogWarning("Email send attempt {Attempt} failed, retrying...", attempt + 1);
                await Task.Delay(1000);
            }
        }
    }
}
