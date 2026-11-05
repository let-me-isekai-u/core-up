using System.Data;
using System.Security.Claims;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Infrastructure.Administration;
public sealed class PremiumRequestService(CoreUpDbContext db, AccountAccessService access, LocalPanelAdministrator panel)
{
    public async Task SubmitAsync(Guid userId, CancellationToken ct) {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if ((await access.CurrentAsync(userId, ct)).HasPremium) throw new AppException(409,"Tài khoản đã có Premium.");
        if (await db.Set<PremiumDemoRequest>().AnyAsync(x=>x.UserId==userId && x.Status=="Pending",ct))
            throw new AppException(409,"Bạn đã có yêu cầu đang chờ.");
        db.Add(new PremiumDemoRequest { Id=Guid.NewGuid(),UserId=userId,CreatedAt=DateTime.UtcNow });
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id=Guid.NewGuid(),TargetUserId=userId,Action="User:PremiumRequestSubmitted",PreviousState="None",NewState="Pending",Reason="Yêu cầu demo do người dùng chủ động gửi",CreatedAt=DateTime.UtcNow });
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); } catch (Exception ex) when (ex is DbUpdateException or Npgsql.PostgresException) { throw new AppException(409,"Yêu cầu đã thay đổi. Làm mới trạng thái trước khi thử lại."); }
    }
    public async Task CancelAsync(Guid userId, CancellationToken ct) {
        var request=await db.Set<PremiumDemoRequest>().SingleOrDefaultAsync(x=>x.UserId==userId && x.Status=="Pending",ct)
            ?? throw new AppException(404,"Không có yêu cầu đang chờ.");
        request.Status="Cancelled"; request.DecidedAt=DateTime.UtcNow; request.Revision++;
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id=Guid.NewGuid(),TargetUserId=userId,Action="User:PremiumRequestCancelled",PreviousState="Pending",NewState="Cancelled",Reason="Người dùng hủy yêu cầu demo",CreatedAt=DateTime.UtcNow });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { throw new AppException(409,"Yêu cầu đã thay đổi. Làm mới trạng thái."); }
    }
    public async Task DecideAsync(ClaimsPrincipal actor, Guid id, bool approve, int days, string? reason, bool confirm, CancellationToken ct) {
        if (!confirm || days is not (7 or 30 or 180 or 365)) throw new AppException(400,"Xác nhận và chọn thời hạn hợp lệ.");
        reason=(reason??"").Trim(); if(reason.Length>1000) throw new AppException(400,"Lý do tối đa 1000 ký tự.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        if (!panel.IsAuthenticated(actor) && !(Guid.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && await db.Users.AnyAsync(x=>x.Id==actorId && x.AccountState=="Active" && x.EmailVerified && x.Role==ApplicationRole.Admin,ct))) throw new AppException(403,"Cần quyền Admin.");
        var request=await db.Set<PremiumDemoRequest>().SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AppException(404,"Không tìm thấy yêu cầu.");
        if(request.Status!="Pending") throw new AppException(409,"Yêu cầu đã được xử lý.");
        if(!await db.Users.AnyAsync(x=>x.Id==request.UserId && x.AccountState=="Active",ct)) throw new AppException(409,"Tài khoản không hoạt động.");
        request.Status=approve?"Approved":"Rejected"; request.DecidedAt=DateTime.UtcNow;request.Reason=reason;request.Revision++;
        if(approve) {
            var e=await db.PremiumEntitlements.SingleOrDefaultAsync(x=>x.UserId==request.UserId,ct);
            if((await access.CurrentAsync(request.UserId,ct)).HasPremium) throw new AppException(409,"Tài khoản đã có Premium. Xem chi tiết để gia hạn.");
            await access.ChangeAsync(actor,request.UserId,false,DateTime.UtcNow.AddDays(days),false,false,reason,e?.Revision??0,ct);
        }
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id=Guid.NewGuid(),TargetUserId=request.UserId,
            AdminUserId=Guid.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier),out var admin)?admin:null,
            Action="ControlPanel:Request:"+request.Status,PreviousState="Pending",NewState=request.Status,Reason=reason,CreatedAt=DateTime.UtcNow });
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
