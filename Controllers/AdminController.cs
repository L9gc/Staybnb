using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
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
}