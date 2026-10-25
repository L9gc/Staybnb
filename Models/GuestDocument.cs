using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class GuestDocument
{
public int Id { get; set; }

public int BookingId { get; set; }

public Booking? Booking { get; set; }

[Required]
[StringLength(50)]
public string DocumentType { get; set; } = string.Empty;

[Required]
[StringLength(500)]
public string FilePath { get; set; } = string.Empty;

public bool IsVerified { get; set; } = false;

public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

public DateTime? VerifiedAt { get; set; }

}
