using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Services;

public class ActivityLogger
{
private readonly ApplicationDbContext _context;
private readonly IHttpContextAccessor _httpContextAccessor;

public ActivityLogger(
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor)
{
    _context = context;
    _httpContextAccessor = httpContextAccessor;
}

public async Task LogAsync(string action, string details)
{
    var httpContext = _httpContextAccessor.HttpContext;

    var userId = httpContext?.User
        .FindFirstValue(ClaimTypes.NameIdentifier);

    var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

    var activity = new ActivityLog
    {
        UserId = userId,
        Action = action,
        Details = details,
        Timestamp = DateTime.UtcNow,
        IPAddress = ipAddress
    };

    _context.ActivityLogs.Add(activity);

    await _context.SaveChangesAsync();
}

}
