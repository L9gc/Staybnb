using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class ActivityLog
{
public int Id { get; set; }

public string? UserId { get; set; }

[Required]
[StringLength(100)]
public string Action { get; set; } = string.Empty;

[StringLength(2000)]
public string Details { get; set; } = string.Empty;

public DateTime Timestamp { get; set; } = DateTime.UtcNow;

[StringLength(100)]
public string? IPAddress { get; set; }

}
