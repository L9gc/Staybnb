using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace Staybnb.Controllers;

[Authorize]
public class BookingsController : Controller
{
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;
private readonly IWebHostEnvironment _environment;

public BookingsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment)
{
    _context = context;
    _userManager = userManager;
    _environment = environment;
}

// GET: /Bookings/Create?propertyId=1
[HttpGet]
public async Task<IActionResult> Create(int propertyId)
{
    var property = await _context.Properties
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);

    if (property == null)
    {
        return NotFound();
    }

    ViewBag.Property = property;

    var model = new CreateBookingViewModel
    {
        PropertyId = property.Id,
        CheckInDate = DateTime.Today.AddDays(1),
        CheckOutDate = DateTime.Today.AddDays(2),
        NumberOfGuests = 1
    };

    return View(model);
}

// POST: /Bookings/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateBookingViewModel model)
{
    var property = await _context.Properties
        .FirstOrDefaultAsync(p =>
            p.Id == model.PropertyId && p.IsActive);

    if (property == null)
    {
        return NotFound();
    }

    ViewBag.Property = property;

    if (model.CheckInDate.Date < DateTime.Today)
    {
        ModelState.AddModelError(
            nameof(model.CheckInDate),
            "Check-in cannot be in the past.");
    }

    if (model.CheckOutDate.Date <= model.CheckInDate.Date)
    {
        ModelState.AddModelError(
            nameof(model.CheckOutDate),
            "Check-out must be after check-in.");
    }

    // Check whether another booking overlaps the requested dates.
    if (model.CheckOutDate.Date > model.CheckInDate.Date)
    {
        var hasOverlappingBooking = await _context.Bookings
            .AnyAsync(b =>
                b.PropertyId == model.PropertyId &&
                b.CheckInDate < model.CheckOutDate.Date &&
                b.CheckOutDate > model.CheckInDate.Date &&
                b.Status != "Rejected" &&
                b.Status != "Cancelled");

        if (hasOverlappingBooking)
        {
            ModelState.AddModelError(
                nameof(model.CheckInDate),
                "This property already has a booking for some of these dates. Please choose different dates.");
        }
    }

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var numberOfNights =
        (model.CheckOutDate.Date - model.CheckInDate.Date).Days;

    var booking = new Booking
    {
        GuestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        PropertyId = property.Id,
        CheckInDate = model.CheckInDate.Date,
        CheckOutDate = model.CheckOutDate.Date,
        NumberOfGuests = model.NumberOfGuests,
        PricePerNight = property.PricePerNight,
        CleaningFee = property.CleaningFee,
        ServiceFee = property.ServiceFee,
        TotalPrice =
            (property.PricePerNight * numberOfNights)
            + property.CleaningFee
            + property.ServiceFee,
        Status = "Pending",
        CreatedAt = DateTime.UtcNow
    };

    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();

    return RedirectToAction(
        nameof(Confirmation),
        new { id = booking.Id });
}

// GET: /Bookings/Confirmation/1
[HttpGet]
public async Task<IActionResult> Confirmation(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var booking = await _context.Bookings
        .AsNoTracking()
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b =>
            b.Id == id && b.GuestId == userId);

    if (booking == null)
    {
        return NotFound();
    }

    return View(booking);
}

// GET: /Bookings/MyBookings
[HttpGet]
public async Task<IActionResult> MyBookings()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var bookings = await _context.Bookings
        .AsNoTracking()
        .Include(b => b.Property)
        .Where(b => b.GuestId == userId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return View(bookings);
}

// GET: /Bookings/UploadDocument/1
// Display the document upload form for an approved booking.
[HttpGet]
public async Task<IActionResult> UploadDocument(int id)
{
    var guestId = _userManager.GetUserId(User);

    var booking = await _context.Bookings
        .AsNoTracking()
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b =>
            b.Id == id &&
            b.GuestId == guestId &&
            b.Status == "Approved");

    if (booking == null)
    {
        return NotFound();
    }

    ViewBag.PropertyTitle = booking.Property.Title;

    return View(new GuestDocumentViewModel
    {
        BookingId = booking.Id
    });
}

// POST: /Bookings/UploadDocument
// Validate and save a guest document.
[HttpPost]
[ValidateAntiForgeryToken]
[RequestSizeLimit(5_500_000)]
public async Task<IActionResult> UploadDocument(
    GuestDocumentViewModel model)
{
    var guestId = _userManager.GetUserId(User);

    var booking = await _context.Bookings
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b =>
            b.Id == model.BookingId &&
            b.GuestId == guestId &&
            b.Status == "Approved");

    if (booking == null)
    {
        return NotFound();
    }

    ViewBag.PropertyTitle = booking.Property.Title;

    // Validate the uploaded file.
    if (model.File == null || model.File.Length == 0)
    {
        ModelState.AddModelError(
            nameof(model.File),
            "Please select a document to upload.");
    }
    else if (model.File.Length > 5 * 1024 * 1024)
    {
        ModelState.AddModelError(
            nameof(model.File),
            "The document must not exceed 5 MB.");
    }

    var allowedExtensions = new[]
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    if (model.File != null && model.File.Length > 0)
    {
        var extension = Path
            .GetExtension(model.File.FileName)
            .ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(model.File),
                "Only PDF, JPG, JPEG and PNG files are allowed.");
        }
    }

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var fileExtension = Path
        .GetExtension(model.File!.FileName)
        .ToLowerInvariant();

    // Store documents outside wwwroot so they aren't publicly accessible.
    var uploadDirectory = Path.Combine(
        _environment.ContentRootPath,
        "App_Data",
        "GuestDocuments");

    Directory.CreateDirectory(uploadDirectory);

    // Use a generated filename rather than the original filename.
    var storedFileName =
        $"{Guid.NewGuid()}{fileExtension}";

    var fullPath = Path.Combine(
        uploadDirectory,
        storedFileName);

    await using (var stream = new FileStream(
        fullPath,
        FileMode.CreateNew,
        FileAccess.Write))
    {
        await model.File.CopyToAsync(stream);
    }

    var document = new GuestDocument
    {
        BookingId = booking.Id,
        DocumentType = model.DocumentType,
        FilePath = storedFileName,
        IsVerified = false,
        UploadedAt = DateTime.UtcNow
    };

    _context.GuestDocuments.Add(document);

    await _context.SaveChangesAsync();

    TempData["Message"] =
        "Your document was uploaded successfully.";

    return RedirectToAction(nameof(MyBookings));
}

}
