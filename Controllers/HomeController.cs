using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

public class HomeController : Controller
{
private readonly ApplicationDbContext _context;

public HomeController(ApplicationDbContext context)
{
    _context = context;
}

public async Task<IActionResult> Index()
{
    var featuredProperties = await _context.Properties
        .AsNoTracking()
        .Include(p => p.Images)
        .Where(p => p.IsActive)
        .OrderByDescending(p => p.CreatedAt)
        .Take(6)
        .ToListAsync();

    return View(featuredProperties);
}

public IActionResult Privacy()
{
    return View();
}

[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public IActionResult Error()
{
    return View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}

}
