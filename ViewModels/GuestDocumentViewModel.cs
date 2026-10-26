using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Staybnb.ViewModels;

public class GuestDocumentViewModel
{
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Please select a document type.")]
    [Display(Name = "Document Type")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a document to upload.")]
    [Display(Name = "Document File")]
    public IFormFile? File { get; set; }
}