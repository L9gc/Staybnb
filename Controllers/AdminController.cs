using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;
private readonly RoleManager<IdentityRole> _roleManager;
private readonly ActivityLogger _activityLogger;

public AdminController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ActivityLogger activityLogger)
{
    _context = context;
    _userManager = userManager;
    _roleManager = roleManager;
    _activityLogger = activityLogger;
}

// Display pending host applications.
[HttpGet]
public async Task<IActionResult> Index()
{
    var applications = await _context.HostApplications
        .AsNoTracking()
        .Include(a => a.Applicant)
        .Include(a => a.Images)
        .Where(a => a.Status == "Pending")
        .OrderBy(a => a.SubmittedAt)
        .ToListAsync();

    return View(applications);
}

// Display the details of a host application.
[HttpGet]
public async Task<IActionResult> ReviewApplication(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var application = await _context.HostApplications
        .AsNoTracking()
        .Include(a => a.Applicant)
        .Include(a => a.Images)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (application == null)
    {
        return NotFound();
    }

    return View(application);
}

// Approve a host application, create its property listing,
// update the applicant's role, notify the applicant, and log the actions.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveApplication(int id)
{
    var application = await _context.HostApplications
        .Include(a => a.Images)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (application == null)
    {
        return NotFound();
    }

    if (application.Status != "Pending")
    {
        TempData["Message"] =
            "This application has already been reviewed.";

        return RedirectToAction(nameof(Index));
    }

    var applicant = await _userManager.FindByIdAsync(
        application.ApplicantId);

    if (applicant == null)
    {
        return NotFound();
    }

    // Make sure the required roles exist.
    if (!await _roleManager.RoleExistsAsync("Host") ||
        !await _roleManager.RoleExistsAsync("Guest"))
    {
        TempData["Message"] =
            "The required user roles could not be found.";

        return RedirectToAction(
            nameof(ReviewApplication),
            new { id });
    }

    // Create the property listing.
    var property = new Property
    {
        Title = application.Title,
        Description = application.Description,
        PricePerNight = application.PricePerNight,
        CleaningFee = 0,
        ServiceFee = 0,
        Address = application.Address,
        City = application.City,
        PropertyType = application.PropertyType,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        HostId = applicant.Id
    };

    // Transfer uploaded images to the new property.
    foreach (var image in application.Images)
    {
        image.Property = property;
        image.HostApplication = null;
    }

    // Update the applicant's role.
    var removeGuestResult =
        await _userManager.RemoveFromRoleAsync(
            applicant,
            "Guest");

    if (!removeGuestResult.Succeeded)
    {
        TempData["Message"] =
            "Could not update the applicant's role.";

        return RedirectToAction(
            nameof(ReviewApplication),
            new { id });
    }

    var addHostResult =
        await _userManager.AddToRoleAsync(
            applicant,
            "Host");

    if (!addHostResult.Succeeded)
    {
        // Restore the Guest role if adding Host fails.
        await _userManager.AddToRoleAsync(
            applicant,
            "Guest");

        TempData["Message"] =
            "Could not assign the Host role.";

        return RedirectToAction(
            nameof(ReviewApplication),
            new { id });
    }

    // Save the new property.
    _context.Properties.Add(property);

    // Update the application status.
    application.Status = "Approved";
    application.ReviewedAt = DateTime.UtcNow;
    application.ReviewedById =
        _userManager.GetUserId(User);

    // Notify the applicant.
    var notification = new Notification
    {
        UserId = applicant.Id,
        Title = "Host Application Approved",
        MessageText =
            $"Congratulations, {applicant.FullName}! " +
            $"Your application to host '{application.Title}' " +
            "has been approved. You can now manage your property " +
            "from your Host Dashboard.",
        LinkUrl = "/Host/Index",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    // Log the successful application approval.
    await _activityLogger.LogAsync(
        "Host Application Approved",
        $"Application ID {application.Id} for " +
        $"'{application.Title}' was approved. " +
        $"Applicant ID: {applicant.Id}. " +
        $"Property ID: {property.Id}.");

    // Log the role change.
    await _activityLogger.LogAsync(
        "Role Changed",
        $"User ID {applicant.Id} changed from Guest to Host " +
        $"after approval of application ID {application.Id}.");

    TempData["Message"] =
        $"Host application approved. " +
        $"{applicant.FullName} is now a Host.";

    return RedirectToAction(nameof(Index));
}

// Reject a host application, notify the applicant, and log the action.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RejectApplication(int id)
{
    var application = await _context.HostApplications
        .FirstOrDefaultAsync(a => a.Id == id);

    if (application == null)
    {
        return NotFound();
    }

    if (application.Status != "Pending")
    {
        TempData["Message"] =
            "This application has already been reviewed.";

        return RedirectToAction(nameof(Index));
    }

    application.Status = "Rejected";
    application.ReviewedAt = DateTime.UtcNow;
    application.ReviewedById =
        _userManager.GetUserId(User);

    // Notify the applicant.
    var notification = new Notification
    {
        UserId = application.ApplicantId,
        Title = "Host Application Rejected",
        MessageText =
            $"Unfortunately, your application to host " +
            $"'{application.Title}' has been rejected. " +
            "You can review your application status from " +
            "your host application page.",
        LinkUrl = "/HostApplications/MyApplication",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);

    await _context.SaveChangesAsync();

    // Log the successful rejection.
    await _activityLogger.LogAsync(
        "Host Application Rejected",
        $"Application ID {application.Id} for " +
        $"'{application.Title}' was rejected. " +
        $"Applicant ID: {application.ApplicantId}.");

    TempData["Message"] =
        $"The application for '{application.Title}' has been rejected.";

    return RedirectToAction(nameof(Index));
}

// Display recent activity logs for administrators.
[HttpGet]
public async Task<IActionResult> ActivityLogs()
{
    var activities = await _context.ActivityLogs
        .AsNoTracking()
        .OrderByDescending(a => a.Timestamp)
        .Take(200)
        .ToListAsync();

    return View(activities);
}


}
