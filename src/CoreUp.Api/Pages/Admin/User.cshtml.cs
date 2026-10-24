using System.Globalization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Administration;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CoreUp.Api.Pages.Admin;
public sealed class UserModel(CoreUpDbContext db, AccountAccessService access, IPasswordHasher<User> passwordHasher) : PageModel
{
    public sealed record AccountRow(Guid Id, string Username, string Email, DateTime CreatedAt, bool EmailVerified, ApplicationRole Role);
    public UserProfile? Profile { get; private set; }
    public bool HasPassword { get; private set; }
    [BindProperty] public string? NewPassword { get; set; }
    [TempData] public string? Notice { get; set; }
    public AccountRow? Account { get; private set; }
    public PremiumEntitlement? Entitlement { get; private set; }
    public EntitlementResponse? Access { get; private set; }
    [BindProperty] public string Duration { get; set; } = "30";
    [BindProperty] public string? CustomUtc { get; set; }
    [BindProperty] public string? Reason { get; set; }
    [BindProperty] public bool Confirm { get; set; }
    [BindProperty] public bool Extend { get; set; }
    [BindProperty] public int ExpectedRevision { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) {
        await Load(id, ct); ExpectedRevision = Entitlement?.Revision ?? 0;
        return Account == null ? NotFound() : Page();
    }
    private async Task Load(Guid id, CancellationToken ct) {
        Account = await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new AccountRow(x.Id,x.Username,x.Email,x.CreatedAt,x.EmailVerified,x.Role)).SingleOrDefaultAsync(ct);
        Profile = await db.Set<UserProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        HasPassword = await db.Users.AnyAsync(x => x.Id == id && x.PasswordHash != null, ct);
        Entitlement = await db.PremiumEntitlements.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        Access = await access.CurrentAsync(id, ct);
    }
    public async Task<IActionResult> OnPostPasswordAsync(Guid id, CancellationToken ct) {
        await Load(id, ct);
        if (Account == null) return NotFound();
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length > 128) {
            ModelState.AddModelError("", "Nhập mật khẩu mới từ 1 đến 128 ký tự.");
            ExpectedRevision = Entitlement?.Revision ?? 0;
            return Page();
        }
        var user = await db.Users.SingleAsync(x => x.Id == id, ct);
        user.PasswordHash = passwordHasher.HashPassword(user, NewPassword);
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        db.AdminAuditEvents.Add(new AdminAuditEvent {
            Id = Guid.NewGuid(), TargetUserId = id,
            AdminUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
            Action = "ControlPanel:PasswordReset", PreviousState = "{}", NewState = "{}",
            Reason = "Đặt lại mật khẩu từ Control Panel", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        Notice = "Đã đặt lại mật khẩu. Người dùng cần đăng nhập lại.";
        return RedirectToPage("/Admin/User", new { id });
    }
    public async Task<IActionResult> OnPostAsync(Guid id, string operation, CancellationToken ct) {
        await Load(id, ct);
        if (Account == null) return NotFound();
        if (!ModelState.IsValid || !Confirm || operation is not ("grant" or "revoke")) {
            ModelState.AddModelError("", "Kiểm tra thông tin và xác nhận tài khoản đích."); return Page();
        }
        DateTime? expires = null;
        if (operation == "grant") {
            if (Duration is "7" or "30" or "180" or "365") expires = DateTime.UtcNow.AddDays(int.Parse(Duration));
            else if (Duration == "custom" && DateTimeOffset.TryParse(CustomUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)) expires = parsed.UtcDateTime;
            else if (Duration != "forever") { ModelState.AddModelError("", "Chọn thời hạn hợp lệ (UTC)."); return Page(); }
        }
        try {
            await access.ChangeAsync(User,id,operation == "revoke",expires,Duration == "forever",Extend,Reason,ExpectedRevision,ct);
            return RedirectToPage("/Admin/User", new { id });
        } catch (AppException ex) {
            ModelState.AddModelError("", ex.Message); return Page();
        } catch (DbUpdateConcurrencyException) {
            ModelState.AddModelError("", "Dữ liệu đã thay đổi. Tải lại trang và xác nhận lại."); return Page();
        } catch (Exception ex) when (ex is PostgresException { SqlState: "40001" } || ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "40001" } }) {
            ModelState.AddModelError("", "Yêu cầu khác đã thay đổi dữ liệu. Tải lại trang trước khi thử lại."); return Page();
        }
    }
}
