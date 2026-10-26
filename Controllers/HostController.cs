using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using System.Security.Claims;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;
using System.IO;

namespace Staybnb.Controllers;

[Authorize(Roles = "Host")]
public class HostController : Controller
{
private readonly ApplicationDbContext _context;
private readonly IWebHostEnvironment _environment;
private readonly ActivityLogger _activityLogger;

public HostController(
    ApplicationDbContext context,
    IWebHostEnvironment environment,
    ActivityLogger activityLogger)
{
    _context = context;
    _environment = environment;
    _activityLogger = activityLogger;
}

// Display the logged-in host's properties.
[HttpGet]
public async Task<IActionResult> Index()
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var properties = await _context.Properties
        .AsNoTracking()
        .Include(p => p.Images)
        .Where(p => p.HostId == hostId)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return View(properties);
}

// Activate or deactivate a property listing.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleListing(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var property = await _context.Properties
        .FirstOrDefaultAsync(p =>
            p.Id == id && p.HostId == hostId);

    if (property == null)
    {
        return NotFound();
    }

    property.IsActive = !property.IsActive;

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        "Listing Status Changed",
        $"Property ID {property.Id} ('{property.Title}') " +
        $"was {(property.IsActive ? "activated" : "deactivated")}.");

    TempData["Message"] = property.IsActive
        ? "Your property listing is now active."
        : "Your property listing has been deactivated.";

    return RedirectToAction(nameof(Index));
}

// Display booking requests for the logged-in host's properties.
[HttpGet]
public async Task<IActionResult> Bookings()
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var bookings = await _context.Bookings
        .AsNoTracking()
        .Include(b => b.Property)
        .Include(b => b.Guest)
        .Where(b => b.Property.HostId == hostId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return View(bookings);
}

// Approve a booking request and notify the guest.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveBooking(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var booking = await _context.Bookings
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b =>
            b.Id == id &&
            b.Property.HostId == hostId);

    if (booking == null)
    {
        return NotFound();
    }

    if (booking.Status != "Pending")
    {
        TempData["Message"] =
            "This booking has already been processed.";

        return RedirectToAction(nameof(Bookings));
    }

    // Check whether another approved booking overlaps these dates.
    var hasConflict = await _context.Bookings.AnyAsync(b =>
        b.PropertyId == booking.PropertyId &&
        b.Id != booking.Id &&
        b.Status == "Approved" &&
        b.CheckInDate < booking.CheckOutDate &&
        b.CheckOutDate > booking.CheckInDate);

    if (hasConflict)
    {
        TempData["Message"] =
            "This booking conflicts with an existing approved booking.";

        return RedirectToAction(nameof(Bookings));
    }

    booking.Status = "Approved";

    var notification = new Notification
    {
        UserId = booking.GuestId,
        Title = "Booking Approved",
        MessageText =
            $"Your booking for {booking.Property.Title} has been approved!",
        LinkUrl = "/Bookings/MyBookings",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        "Booking Approved",
        $"Booking ID {booking.Id} for property " +
        $"'{booking.Property.Title}' was approved. " +
        $"Guest ID: {booking.GuestId}.");

    TempData["Message"] = "Booking approved successfully.";

    return RedirectToAction(nameof(Bookings));
}

// Reject a booking request and notify the guest.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RejectBooking(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var booking = await _context.Bookings
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b =>
            b.Id == id &&
            b.Property.HostId == hostId);

    if (booking == null)
    {
        return NotFound();
    }

    if (booking.Status != "Pending")
    {
        TempData["Message"] =
            "This booking has already been processed.";

        return RedirectToAction(nameof(Bookings));
    }

    booking.Status = "Rejected";

    var notification = new Notification
    {
        UserId = booking.GuestId,
        Title = "Booking Rejected",
        MessageText =
            $"Unfortunately, your booking for {booking.Property.Title} has been rejected.",
        LinkUrl = "/Bookings/MyBookings",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        "Booking Rejected",
        $"Booking ID {booking.Id} for property " +
        $"'{booking.Property.Title}' was rejected. " +
        $"Guest ID: {booking.GuestId}.");

    TempData["Message"] = "Booking rejected successfully.";

    return RedirectToAction(nameof(Bookings));
}

// Display check-in settings for a property.
[HttpGet]
public async Task<IActionResult> CheckInSettings(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var property = await _context.Properties
        .AsNoTracking()
        .FirstOrDefaultAsync(p =>
            p.Id == id && p.HostId == hostId);

    if (property == null)
    {
        return NotFound();
    }

    var settings = await _context.CheckInProcesses
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.PropertyId == id);

    var viewModel = new CheckInProcessViewModel
    {
        PropertyId = property.Id,
        PropertyTitle = property.Title,
        Rules = settings?.Rules ?? string.Empty,
        RequiredDocuments = settings?.RequiredDocuments ?? string.Empty
    };

    return View(viewModel);
}

// Save check-in settings for a property.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CheckInSettings(
    CheckInProcessViewModel model)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var property = await _context.Properties
        .FirstOrDefaultAsync(p =>
            p.Id == model.PropertyId &&
            p.HostId == hostId);

    if (property == null)
    {
        return NotFound();
    }

    model.PropertyTitle = property.Title;

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var settings = await _context.CheckInProcesses
        .FirstOrDefaultAsync(c =>
            c.PropertyId == model.PropertyId);

    if (settings == null)
    {
        settings = new CheckInProcess
        {
            PropertyId = property.Id
        };

        _context.CheckInProcesses.Add(settings);
    }

    settings.Rules = model.Rules;
    settings.RequiredDocuments = model.RequiredDocuments;
    settings.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        "Check-in Settings Updated",
        $"Check-in rules and required documents were updated " +
        $"for property ID {property.Id} ('{property.Title}').");

    TempData["Message"] =
        "Check-in instructions saved successfully.";

    return RedirectToAction(
        nameof(CheckInSettings),
        new { id = property.Id });
}

// Display documents uploaded for bookings belonging to this host.
[HttpGet]
public async Task<IActionResult> Documents()
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var documents = await _context.GuestDocuments
        .AsNoTracking()
        .Include(d => d.Booking)
            .ThenInclude(b => b.Property)
        .Include(d => d.Booking)
            .ThenInclude(b => b.Guest)
        .Where(d => d.Booking.Property.HostId == hostId)
        .OrderByDescending(d => d.UploadedAt)
        .ToListAsync();

    return View(documents);
}

// View a guest document securely.
[HttpGet]
public async Task<IActionResult> ViewDocument(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var document = await _context.GuestDocuments
        .AsNoTracking()
        .Include(d => d.Booking)
            .ThenInclude(b => b.Property)
        .FirstOrDefaultAsync(d =>
            d.Id == id &&
            d.Booking.Property.HostId == hostId);

    if (document == null)
    {
        return NotFound();
    }

    var extension = Path.GetExtension(document.FilePath)
        .ToLowerInvariant();

    var contentType = extension switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => null
    };

    if (contentType == null)
    {
        return BadRequest("This document type cannot be viewed.");
    }

    var fileName = Path.GetFileName(document.FilePath);

    if (string.IsNullOrWhiteSpace(fileName) ||
        fileName != document.FilePath)
    {
        return BadRequest("Invalid document path.");
    }

    var uploadDirectory = Path.Combine(
        _environment.ContentRootPath,
        "App_Data",
        "GuestDocuments");

    var fullPath = Path.Combine(uploadDirectory, fileName);

    if (!System.IO.File.Exists(fullPath))
    {
        return NotFound("The document file could not be found.");
    }

    // Return the file for inline viewing where supported.
    Response.Headers["X-Content-Type-Options"] = "nosniff";
    Response.Headers["Cache-Control"] = "no-store";

    return PhysicalFile(fullPath, contentType);
}

// Download a guest document securely.
[HttpGet]
public async Task<IActionResult> DownloadDocument(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var document = await _context.GuestDocuments
        .AsNoTracking()
        .Include(d => d.Booking)
            .ThenInclude(b => b.Property)
        .FirstOrDefaultAsync(d =>
            d.Id == id &&
            d.Booking.Property.HostId == hostId);

    if (document == null)
    {
        return NotFound();
    }

    var extension = Path.GetExtension(document.FilePath)
        .ToLowerInvariant();

    var contentType = extension switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => null
    };

    if (contentType == null)
    {
        return BadRequest("This document type cannot be downloaded.");
    }

    var fileName = Path.GetFileName(document.FilePath);

    if (string.IsNullOrWhiteSpace(fileName) ||
        fileName != document.FilePath)
    {
        return BadRequest("Invalid document path.");
    }

    var uploadDirectory = Path.Combine(
        _environment.ContentRootPath,
        "App_Data",
        "GuestDocuments");

    var fullPath = Path.Combine(uploadDirectory, fileName);

    if (!System.IO.File.Exists(fullPath))
    {
        return NotFound("The document file could not be found.");
    }

    Response.Headers["X-Content-Type-Options"] = "nosniff";
    Response.Headers["Cache-Control"] = "no-store";

    var downloadName = $"GuestDocument_{document.Id}{extension}";

    return PhysicalFile(fullPath, contentType, downloadName);
}

// Mark a guest document as verified and notify the guest.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> VerifyDocument(int id)
{
    var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var document = await _context.GuestDocuments
        .Include(d => d.Booking)
            .ThenInclude(b => b.Property)
        .FirstOrDefaultAsync(d =>
            d.Id == id &&
            d.Booking.Property.HostId == hostId);

    if (document == null)
    {
        return NotFound();
    }

    if (document.IsVerified)
    {
        TempData["Message"] =
            "This document has already been verified.";

        return RedirectToAction(nameof(Documents));
    }

    document.IsVerified = true;
    document.VerifiedAt = DateTime.UtcNow;

    var notification = new Notification
    {
        UserId = document.Booking.GuestId,
        Title = "Guest Document Verified",
        MessageText =
            $"Your {document.DocumentType} for " +
            $"{document.Booking.Property.Title} has been verified.",
        LinkUrl = "/Bookings/MyBookings",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        "Guest Document Verified",
        $"Document ID {document.Id} (type: {document.DocumentType}) " +
        $"for booking ID {document.BookingId} and property " +
        $"'{document.Booking.Property.Title}' was verified.");

    TempData["Message"] = "Document verified successfully.";

    return RedirectToAction(nameof(Documents));
}

}
