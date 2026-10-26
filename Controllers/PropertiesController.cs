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
public async Task<IActionResult> Index(string? city, string? propertyType)
{
var propertiesQuery = _context.Properties
.AsNoTracking()
.Include(p => p.Images)
.Where(p => p.IsActive);


if (!string.IsNullOrWhiteSpace(city))
{
    propertiesQuery = propertiesQuery.Where(p =>
        p.City.Contains(city));
}

if (!string.IsNullOrWhiteSpace(propertyType))
{
    propertiesQuery = propertiesQuery.Where(p =>
        p.PropertyType == propertyType);
}

ViewBag.City = city;
ViewBag.PropertyType = propertyType;

ViewBag.PropertyTypes = await _context.Properties
    .Where(p => p.IsActive)
    .Select(p => p.PropertyType)
    .Distinct()
    .OrderBy(type => type)
    .ToListAsync();

var properties = await propertiesQuery
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
