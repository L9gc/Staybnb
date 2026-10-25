using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;
using System.Security.Claims;

namespace Staybnb.Controllers;

[Authorize]
public class BookingsController : Controller
{
private readonly ApplicationDbContext _context;

public BookingsController(ApplicationDbContext context)
{
    _context = context;
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
        .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);

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
    // Check whether the property already has a booking
// that overlaps with the requested dates.
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
        TotalPrice = (property.PricePerNight * numberOfNights)
                     + property.CleaningFee
                     + property.ServiceFee,
        Status = "Pending",
        CreatedAt = DateTime.UtcNow
    };

    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
}

// GET: /Bookings/Confirmation/1
[HttpGet]
public async Task<IActionResult> Confirmation(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var booking = await _context.Bookings
        .AsNoTracking()
        .Include(b => b.Property)
        .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

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

}
