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
public sealed class UserModel(CoreUpDbContext db, AccountAccessService access, IPasswordHasher<User> passwordHasher, IWebHostEnvironment environment, AccountSecurityService security) : PageModel
{
    public sealed record AccountRow(Guid Id, string Username, string Email, DateTime CreatedAt, bool EmailVerified, ApplicationRole Role, string AccountState);
    public UserProfile? Profile { get; private set; }
    public bool CanSetPassword => environment.IsDevelopment();
    public bool HasPassword { get; private set; }
    public bool HasGoogle { get; private set; }
    public List<AdminAuditEvent> History { get; private set; } = [];
    [BindProperty] public string? TargetConfirmation { get; set; }
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
        Account = await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new AccountRow(x.Id,x.Username,x.Email,x.CreatedAt,x.EmailVerified,x.Role,x.AccountState)).SingleOrDefaultAsync(ct);
        Profile = await db.Set<UserProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        HasPassword = await db.Users.AnyAsync(x => x.Id == id && x.PasswordHash != null, ct);
        Entitlement = await db.PremiumEntitlements.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        Access = await access.CurrentAsync(id, ct);
        HasGoogle = await db.ExternalLogins.AnyAsync(x => x.UserId == id && x.Provider == "Google", ct);
        History = await db.AdminAuditEvents.AsNoTracking().Where(x => x.TargetUserId == id).OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
    }
    public async Task<IActionResult> OnPostLifecycleAsync(Guid id, string operation, CancellationToken ct) {
        await Load(id, ct);
        if (Account == null) return NotFound();
        if (!Confirm || TargetConfirmation != Account.Username || operation is not ("disable" or "restore" or "delete" or "sessions")) {
            ModelState.AddModelError("", "Xác nhận thao tác và nhập chính xác username đích."); return Page();
        }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var target = await db.Users.SingleAsync(x => x.Id == id, ct);
        if ((operation is "disable" or "delete") && target.Role == ApplicationRole.Admin &&
            !await db.Users.AnyAsync(x => x.Id != id && x.Role == ApplicationRole.Admin && x.EmailVerified && x.AccountState == "Active", ct)) {
            ModelState.AddModelError("", "Không thể vô hiệu hóa hoặc xóa mềm Admin cuối cùng."); return Page();
        }
        var previous = target.AccountState;
        target.AccountState = operation switch { "disable" => "Disabled", "delete" => "Deleted", "restore" => "Active", _ => target.AccountState };
        target.TokenVersion++; target.UpdatedAt = DateTime.UtcNow;
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id = Guid.NewGuid(), TargetUserId = id,
            AdminUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
            Action = "ControlPanel:Account:" + operation, PreviousState = previous, NewState = target.AccountState,
            Reason = "Xác nhận hai bước từ Control Panel; không xóa dữ liệu liên quan", CreatedAt = DateTime.UtcNow });
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or PostgresException || ex is DbUpdateException) {
            ModelState.AddModelError("", "Dữ liệu đã thay đổi. Tải lại trang trước khi xác nhận lại."); return Page();
        }
        Notice = "Đã cập nhật trạng thái và thu hồi phiên. Xóa mềm có thể khôi phục; dữ liệu vẫn được giữ.";
        return RedirectToPage("/Admin/User", new { id });
    }
    public async Task<IActionResult> OnPostRecoveryAsync(Guid id, CancellationToken ct) {
        await Load(id, ct);
        if (Account == null) return NotFound();
        if (!Confirm) { ModelState.AddModelError("", "Xác nhận gửi yêu cầu reset."); return Page(); }
        try { await security.ForgotAsync(Account.Email, ct); }
        catch (AppException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id = Guid.NewGuid(), TargetUserId = id,
            AdminUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
            Action = "ControlPanel:RecoveryRequested", PreviousState = "{}", NewState = "{}",
            Reason = "Gửi yêu cầu để người dùng tự đặt mật khẩu", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        Notice = "Đã tiếp nhận yêu cầu reset qua email. Người dùng tự đặt mật khẩu trong ứng dụng.";
        return RedirectToPage("/Admin/User", new { id });
    }
    public async Task<IActionResult> OnPostPasswordAsync(Guid id, CancellationToken ct) {
        if (!CanSetPassword) return NotFound();
        await Load(id, ct);
        if (Account == null) return NotFound();
        if (!Confirm || string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8 || NewPassword.Length > 128) {
            ModelState.AddModelError("", "Xác nhận thao tác và nhập mật khẩu mới từ 8 đến 128 ký tự.");
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
            Notice = "Đã cập nhật quyền Premium demo và ghi nhật ký.";
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
