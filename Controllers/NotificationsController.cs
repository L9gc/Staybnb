using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using System.Security.Claims;

namespace Staybnb.Controllers;

[Authorize]
public class NotificationsController : Controller
{
private readonly ApplicationDbContext _context;

public NotificationsController(ApplicationDbContext context)
{
    _context = context;
}

// GET: /Notifications
// Displays notifications belonging to the logged-in user.
public async Task<IActionResult> Index()
{
    var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var notifications = await _context.Notifications
        .AsNoTracking()
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return View(notifications);
}

// POST: /Notifications/MarkAsRead/5
// Marks one notification as read.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkAsRead(int id)
{
    var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var notification = await _context.Notifications
        .FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId);

    if (notification == null)
    {
        return NotFound();
    }

    notification.IsRead = true;

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] =
        "Notification marked as read.";

    return RedirectToAction(nameof(Index));
}

// POST: /Notifications/MarkAllAsRead
// Marks every unread notification belonging to this user as read.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkAllAsRead()
{
    var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var unreadNotifications = await _context.Notifications
        .Where(n =>
            n.UserId == userId && !n.IsRead)
        .ToListAsync();

    foreach (var notification in unreadNotifications)
    {
        notification.IsRead = true;
    }

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] =
        "All notifications have been marked as read.";

    return RedirectToAction(nameof(Index));
}

// GET: /Notifications/Open/5
// Marks a notification as read and opens its destination.
public async Task<IActionResult> Open(int id)
{
    var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var notification = await _context.Notifications
        .FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId);

    if (notification == null)
    {
        return NotFound();
    }

    notification.IsRead = true;

    await _context.SaveChangesAsync();

    // Only redirect to local application URLs.
    if (!string.IsNullOrWhiteSpace(notification.LinkUrl) &&
        Url.IsLocalUrl(notification.LinkUrl))
    {
        return LocalRedirect(notification.LinkUrl);
    }

    return RedirectToAction(nameof(Index));
}

}
