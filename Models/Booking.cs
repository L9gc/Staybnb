using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staybnb.Models;

public class Booking
{
public int Id { get; set; }

[Required]
public string GuestId { get; set; } = string.Empty;

public ApplicationUser? Guest { get; set; }

public int PropertyId { get; set; }

public Property? Property { get; set; }

[Required]
public DateTime CheckInDate { get; set; }

[Required]
public DateTime CheckOutDate { get; set; }

[Range(1, 20)]
public int NumberOfGuests { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal PricePerNight { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal CleaningFee { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal ServiceFee { get; set; }

[Column(TypeName = "decimal(18,2)")]
public decimal TotalPrice { get; set; }

[Required]
[StringLength(20)]
public string Status { get; set; } = "Pending";

public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

public ICollection<GuestDocument> GuestDocuments { get; set; }
    = new List<GuestDocument>();

}
