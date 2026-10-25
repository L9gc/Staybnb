using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Staybnb.ViewModels;

public class HostApplicationViewModel
{
    [Required]
    [StringLength(150)]
    [Display(Name = "Property Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Property Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, 1000000)]
    [Display(Name = "Price Per Night (R)")]
    public decimal PricePerNight { get; set; }

    [Required]
    [StringLength(250)]
    [Display(Name = "Street Address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Property Type")]
    public string PropertyType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Upload at least one property image.")]
    [MinLength(1, ErrorMessage = "Upload at least one property image.")]
    [Display(Name = "Property Images")]
    public List<IFormFile> Images { get; set; } = new();
}