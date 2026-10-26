using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Microsoft.AspNetCore.Identity;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    _context = context;
    _userManager = userManager;
    _roleManager = roleManager;
}
    // Display pending host applications.
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

    // Approve a host application and create its property listing.
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
        TempData["Message"] = "This application has already been reviewed.";
        return RedirectToAction(nameof(Index));
    }

    var applicant = await _userManager.FindByIdAsync(application.ApplicantId);

    if (applicant == null)
    {
        return NotFound();
    }

    // Make sure the required roles exist.
    if (!await _roleManager.RoleExistsAsync("Host") ||
        !await _roleManager.RoleExistsAsync("Guest"))
    {
        TempData["Message"] = "The required user roles could not be found.";
        return RedirectToAction(nameof(ReviewApplication), new { id });
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

    // Transfer the uploaded images to the new property.
    foreach (var image in application.Images)
    {
        image.Property = property;
        image.HostApplication = null;
    }

    // Update the applicant's role.
    var removeGuestResult = await _userManager.RemoveFromRoleAsync(
        applicant, "Guest");

    if (!removeGuestResult.Succeeded)
    {
        TempData["Message"] = "Could not update the applicant's role.";
        return RedirectToAction(nameof(ReviewApplication), new { id });
    }

    var addHostResult = await _userManager.AddToRoleAsync(
        applicant, "Host");

    if (!addHostResult.Succeeded)
    {
        // Restore the Guest role if adding Host fails.
        await _userManager.AddToRoleAsync(applicant, "Guest");

        TempData["Message"] = "Could not assign the Host role.";
        return RedirectToAction(nameof(ReviewApplication), new { id });
    }

    _context.Properties.Add(property);

    application.Status = "Approved";
    application.ReviewedAt = DateTime.UtcNow;
    application.ReviewedById = _userManager.GetUserId(User);

    await _context.SaveChangesAsync();

    TempData["Message"] =
        $"Host application approved. {applicant.FullName} is now a Host.";

    return RedirectToAction(nameof(Index));
}

// Reject a host application.
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
        TempData["Message"] = "This application has already been reviewed.";
        return RedirectToAction(nameof(Index));
    }

    application.Status = "Rejected";
    application.ReviewedAt = DateTime.UtcNow;
    application.ReviewedById = _userManager.GetUserId(User);

    await _context.SaveChangesAsync();

    TempData["Message"] =
        $"The application for '{application.Title}' has been rejected.";

    return RedirectToAction(nameof(Index));
}
}