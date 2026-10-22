using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using CoreUp.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreUp.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string From { get; set; } = "noreply@coreup.local";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

// Transient, bounded in-memory delivery queue: no OTP body in the database/logs.
// Restart can discard pending mail; the user can resend after cooldown. Not a durable outbox.
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    : BackgroundService, IEmailSender
{
    private sealed record Message(string Recipient, string Subject, string Body);
    private readonly Channel<Message> queue = Channel.CreateBounded<Message>(100);
    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!queue.Writer.TryWrite(new(recipient, subject, body)))
            throw new AppException(503, "Hệ thống email đang bận. Vui lòng thử lại sau.");
        return Task.CompletedTask;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var settings = options.Value;
                    using var smtp = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl, Timeout = 10000 };
                    if (!string.IsNullOrEmpty(settings.Username)) smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
                    using var mail = new MailMessage(settings.From, message.Recipient, message.Subject, message.Body);
                    await smtp.SendMailAsync(mail, stoppingToken);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch
                {
                    // Never log exception/body/recipient/credentials or OTP.
                    if (attempt == 2) logger.LogWarning("Email delivery failed after three attempts; resend is available.");
                    else await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
    }
}
