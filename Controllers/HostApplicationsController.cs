using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;
using System.Security.Claims;

namespace Staybnb.Controllers;

[Authorize(Roles = "Guest")]
public class HostApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public HostApplicationsController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: /HostApplications/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Prevent the same user from submitting another pending application.
        var hasPendingApplication = await _context.HostApplications
            .AnyAsync(a =>
                a.ApplicantId == userId &&
                a.Status == "Pending");

        if (hasPendingApplication)
        {
            TempData["Message"] =
                "You already have a pending host application.";

            return RedirectToAction(nameof(MyApplication));
        }

        return View(new HostApplicationViewModel());
    }

    // POST: /HostApplications/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create(HostApplicationViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Confirm that the user does not already have a pending application.
        var hasPendingApplication = await _context.HostApplications
            .AnyAsync(a =>
                a.ApplicantId == userId &&
                a.Status == "Pending");

        if (hasPendingApplication)
        {
            TempData["Message"] =
                "You already have a pending host application.";

            return RedirectToAction(nameof(MyApplication));
        }

        // At least one image is mandatory.
        if (model.Images == null || model.Images.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Images),
                "Upload at least one property image.");
        }

        // Validate uploaded files on the server.
        if (model.Images != null)
        {
            foreach (var image in model.Images)
            {
                if (image.Length == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.Images),
                        "One of the selected images is empty.");

                    continue;
                }

                if (image.Length > MaxFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.Images),
                        "Each image must be 5 MB or smaller.");

                    continue;
                }

                var extension = Path.GetExtension(image.FileName);

                if (!AllowedExtensions.Contains(
                        extension,
                        StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(model.Images),
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Create the application record.
        var application = new HostApplication
        {
            ApplicantId = userId!,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            PricePerNight = model.PricePerNight,
            Address = model.Address.Trim(),
            City = model.City.Trim(),
            PropertyType = model.PropertyType.Trim(),
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _context.HostApplications.Add(application);

        // Save first so the application receives a database ID.
        await _context.SaveChangesAsync();

        // Store images under wwwroot/uploads/host-applications.
        var uploadFolder = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "host-applications");

        Directory.CreateDirectory(uploadFolder);

        foreach (var image in model.Images)
        {
            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            // Generate a unique filename rather than trusting the original.
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var fullPath = Path.Combine(uploadFolder, fileName);

            await using (var stream = new FileStream(
                fullPath,
                FileMode.CreateNew))
            {
                await image.CopyToAsync(stream);
            }

            var propertyImage = new PropertyImage
            {
                HostApplicationId = application.Id,
                ImagePath = $"/uploads/host-applications/{fileName}",
                UploadedAt = DateTime.UtcNow
            };

            _context.PropertyImages.Add(propertyImage);
        }

        await _context.SaveChangesAsync();

        TempData["Message"] =
            "Your host application has been submitted for review.";

        return RedirectToAction(nameof(MyApplication));
    }

    // GET: /HostApplications/MyApplication
    [HttpGet]
    public async Task<IActionResult> MyApplication()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var application = await _context.HostApplications
            .AsNoTracking()
            .Include(a => a.Images)
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync();

        return View(application);
    }
}