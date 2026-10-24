namespace CoreUp.Domain;

public enum ApplicationRole { User, Admin }
public enum PremiumStatus { Active, Revoked }
public enum PremiumSource { AdminDemoGrant }

public sealed class UserExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Google";
    public string ProviderSubject { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// One current record per account; historical states are preserved in audit events.
public sealed class PremiumEntitlement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PremiumStatus Status { get; set; }
    public PremiumSource Source { get; set; } = PremiumSource.AdminDemoGrant;
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? GrantedByAdminId { get; set; }
    public Guid? RevokedByAdminId { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Revision { get; set; }
}

public sealed class AdminAuditEvent
{
    public Guid Id { get; set; }
    // Null actor identifies a local operator; Action distinguishes bootstrap/panel actions.
    public Guid? AdminUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PreviousState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
