using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Api.Pages.Admin;

public sealed class OtpModel(CoreUpDbContext db, IHttpClientFactory clients, IWebHostEnvironment environment) : PageModel
{
    public sealed record Recipient(string Address);
    public sealed record Mail(string ID, string Subject, string Snippet, DateTimeOffset Created, List<Recipient>? To);
    public sealed record Inbox(int Total, List<Mail> Messages);
    public sealed record Row(Guid? UserId, string Username, string Email, bool HasPassword, string Code, string Subject, DateTimeOffset SentAt);
    public List<Row> Rows { get; } = [];
    public string? Error { get; private set; }
    public int Total { get; private set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        PageNumber = Math.Clamp(PageNumber, 1, 100000);
        try {
            var inbox = await clients.CreateClient("LocalMailpit").GetFromJsonAsync<Inbox>($"api/v1/messages?start={(PageNumber-1)*25}&limit=25", ct);
            Total = inbox?.Total ?? 0;
            var messages = (inbox?.Messages ?? []).Where(m => m.Subject.StartsWith("Core-Up:", StringComparison.Ordinal)).ToList();
            var addresses = messages.SelectMany(m => m.To ?? []).Select(r => r.Address.ToLowerInvariant()).Distinct().ToList();
            var users = await db.Users.AsNoTracking().Where(u => addresses.Contains(u.Email))
                .Select(u => new { u.Id, u.Username, u.Email, HasPassword = u.PasswordHash != null }).ToListAsync(ct);
            foreach (var mail in messages) {
                var match = Regex.Match(mail.Snippet ?? "", @"Mã OTP của bạn:\s*(\d{6})", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                if (!match.Success) continue;
                foreach (var recipient in mail.To ?? []) {
                    var user = users.FirstOrDefault(u => string.Equals(u.Email, recipient.Address, StringComparison.OrdinalIgnoreCase));
                    Rows.Add(new(user?.Id, user?.Username ?? "Không có tài khoản", recipient.Address, user?.HasPassword ?? false,
                        match.Groups[1].Value, mail.Subject, mail.Created));
                }
            }
        } catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException || ex is TaskCanceledException && !ct.IsCancellationRequested) {
            Error = "Chưa đọc được hộp thư OTP. Kiểm tra Mailpit của Core-Up ở localhost:8025 rồi tải lại.";
        }
        return Page();
    }
}
