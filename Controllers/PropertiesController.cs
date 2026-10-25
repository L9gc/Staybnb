using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

public class PropertiesController : Controller
{
private readonly ApplicationDbContext _context;

public PropertiesController(ApplicationDbContext context)
{
    _context = context;
}

// GET: /Properties
public async Task<IActionResult> Index()
{
    var properties = await _context.Properties
        .AsNoTracking()
        .Where(p => p.IsActive)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return View(properties);
}

// GET: /Properties/Details/5
public async Task<IActionResult> Details(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var property = await _context.Properties
        .AsNoTracking()
        .Include(p => p.Images)
        .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

    if (property == null)
    {
        return NotFound();
    }

    return View(property);
}
}
