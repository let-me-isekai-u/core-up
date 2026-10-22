using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CoreUp.Application;

namespace CoreUp.Tests;

public sealed record TestMail(string Recipient, string Subject, string Body);
public sealed class TestEmailSender : IEmailSender
{
    public ConcurrentQueue<TestMail> Messages { get; } = new();
    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    { Messages.Enqueue(new(recipient, subject, body)); return Task.CompletedTask; }
    public string Code(string recipient, string subject = "xác minh") => Regex.Match(
        Messages.Last(x => x.Recipient == recipient && x.Subject.Contains(subject)).Body, @"\b\d{6}\b").Value;
}
