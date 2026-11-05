namespace CoreUp.Domain;
public sealed class PremiumDemoRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string Reason { get; set; } = "";
    public int Revision { get; set; }
}
