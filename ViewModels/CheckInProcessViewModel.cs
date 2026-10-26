using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

public class CheckInProcessViewModel
{
public int PropertyId { get; set; }

public string PropertyTitle { get; set; } = string.Empty;

[Required(ErrorMessage = "Please enter the check-in instructions.")]
[StringLength(2000)]
[Display(Name = "Check-in instructions and rules")]
public string Rules { get; set; } = string.Empty;

[Required(ErrorMessage = "Please specify the required documents.")]
[StringLength(1000)]
[Display(Name = "Required documents")]
public string RequiredDocuments { get; set; } = string.Empty;

}
