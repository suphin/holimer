using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels;

public sealed class ApiAccessTokenManagementVM
{
    public ApiAccessTokenCreateVM Form { get; set; } = new();
    public List<ApiAccessTokenRowVM> Tokens { get; set; } = [];
    public string? NewToken { get; set; }
}

public sealed class ApiAccessTokenCreateVM
{
    [Required(ErrorMessage = "Anahtar adı zorunludur."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? ExpiresAt { get; set; }
}

public sealed class ApiAccessTokenRowVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public long UseCount { get; set; }
    public bool IsEnabled { get; set; }
}
