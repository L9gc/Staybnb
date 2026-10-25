using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class Message
{
public int Id { get; set; }

[Required]
public string SenderId { get; set; } = string.Empty;

public ApplicationUser? Sender { get; set; }

[Required]
public string ReceiverId { get; set; } = string.Empty;

public ApplicationUser? Receiver { get; set; }

[Required]
[StringLength(150)]
public string Subject { get; set; } = string.Empty;

[Required]
[StringLength(3000)]
public string Content { get; set; } = string.Empty;

public bool IsRead { get; set; } = false;

public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
