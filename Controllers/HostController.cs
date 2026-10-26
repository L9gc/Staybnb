using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using System.Security.Claims;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize(Roles = "Host")]
public class HostController : Controller
{
private readonly ApplicationDbContext _context;

public HostController(ApplicationDbContext context)
{
    _context = context;
}

// Display the logged-in host's properties.
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

    TempData["Message"] = property.IsActive
        ? "Your property listing is now active."
        : "Your property listing has been deactivated.";

    return RedirectToAction(nameof(Index));
     }
     // Display booking requests for the logged-in host's properties.
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

// Approve a booking request.
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

    await _context.SaveChangesAsync();

    TempData["Message"] = "Booking approved successfully.";

    return RedirectToAction(nameof(Bookings));
}
// Reject a booking request.
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

    await _context.SaveChangesAsync();

    TempData["Message"] = "Booking rejected successfully.";

    return RedirectToAction(nameof(Bookings));
}
}
