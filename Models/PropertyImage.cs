using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class PropertyImage
{
    public int Id { get; set; }

    [Required]
    [StringLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public int? PropertyId { get; set; }

    public Property? Property { get; set; }

    public int? HostApplicationId { get; set; }

    public HostApplication? HostApplication { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}