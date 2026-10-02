using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity;

public sealed class ApiAccessToken : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Scope { get; set; } = "RecipeCosts.Read";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public long UseCount { get; set; }
}
