using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class Notification
{
public int Id { get; set; }

[Required]
public string UserId { get; set; } = string.Empty;

public ApplicationUser? User { get; set; }

[Required]
[StringLength(150)]
public string Title { get; set; } = string.Empty;

[Required]
[StringLength(1000)]
public string MessageText { get; set; } = string.Empty;

[StringLength(300)]
public string? LinkUrl { get; set; }

public bool IsRead { get; set; } = false;

public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
