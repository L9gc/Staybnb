using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

public class CreateBookingViewModel
{
[Required]
public int PropertyId { get; set; }

[Required]
[DataType(DataType.Date)]
[Display(Name = "Check-in Date")]
public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

[Required]
[DataType(DataType.Date)]
[Display(Name = "Check-out Date")]
public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

[Required]
[Range(1, 20)]
[Display(Name = "Number of Guests")]
public int NumberOfGuests { get; set; } = 1;

}
