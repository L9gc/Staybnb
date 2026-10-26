using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using System.Security.Claims;

namespace Staybnb.Controllers;

[Authorize]
public class MessagesController : Controller
{
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;

public MessagesController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}

// GET: /Messages
// Displays messages received by the logged-in user.
public async Task<IActionResult> Index()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var messages = await _context.Messages
        .AsNoTracking()
        .Include(m => m.Sender)
        .Include(m => m.Receiver)
        .Where(m => m.ReceiverId == userId)
        .OrderByDescending(m => m.SentAt)
        .ToListAsync();

    return View(messages);
}

// GET: /Messages/Sent
// Displays messages sent by the logged-in user.
public async Task<IActionResult> Sent()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var messages = await _context.Messages
        .AsNoTracking()
        .Include(m => m.Sender)
        .Include(m => m.Receiver)
        .Where(m => m.SenderId == userId)
        .OrderByDescending(m => m.SentAt)
        .ToListAsync();

    return View(messages);
}

// GET: /Messages/Details/5
// Displays a message if the current user sent or received it.
public async Task<IActionResult> Details(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var message = await _context.Messages
        .Include(m => m.Sender)
        .Include(m => m.Receiver)
        .FirstOrDefaultAsync(m =>
            m.Id == id &&
            (m.SenderId == userId || m.ReceiverId == userId));

    if (message == null)
    {
        return NotFound();
    }

    // Mark received messages as read.
    if (message.ReceiverId == userId && !message.IsRead)
    {
        message.IsRead = true;
        await _context.SaveChangesAsync();
    }

    return View(message);
}

// GET: /Messages/Create
// Displays the compose form.
// Supports preselecting a recipient and subject when replying.
public async Task<IActionResult> Create(
    string? receiverId,
    string? subject)
{
    var currentUserId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    await LoadRecipientsAsync(currentUserId);

    ViewBag.ReceiverId = receiverId;
    ViewBag.Subject = subject;

    return View();
}

// POST: /Messages/Create
// Validates and sends a message.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    string? receiverId,
    string? subject,
    string? content)
{
    var senderId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(senderId))
    {
        return Challenge();
    }

    // Validate required fields.
    if (string.IsNullOrWhiteSpace(receiverId))
    {
        ModelState.AddModelError(
            nameof(receiverId),
            "Please select a recipient.");
    }

    if (string.IsNullOrWhiteSpace(subject))
    {
        ModelState.AddModelError(
            nameof(subject),
            "Please enter a subject.");
    }
    else if (subject.Trim().Length > 150)
    {
        ModelState.AddModelError(
            nameof(subject),
            "The subject cannot exceed 150 characters.");
    }

    if (string.IsNullOrWhiteSpace(content))
    {
        ModelState.AddModelError(
            nameof(content),
            "Please enter a message.");
    }
    else if (content.Trim().Length > 5000)
    {
        ModelState.AddModelError(
            nameof(content),
            "The message cannot exceed 5000 characters.");
    }

    // Verify that the recipient exists and isn't the sender.
    var receiverExists =
        !string.IsNullOrWhiteSpace(receiverId) &&
        receiverId != senderId &&
        await _userManager.Users.AnyAsync(
            u => u.Id == receiverId);

    if (!receiverExists)
    {
        ModelState.AddModelError(
            nameof(receiverId),
            "Please select a valid recipient.");
    }

    if (!ModelState.IsValid)
    {
        await LoadRecipientsAsync(senderId);

        ViewBag.ReceiverId = receiverId;
        ViewBag.Subject = subject;

        // Preserve the submitted values for the form.
        ViewBag.Content = content;

        return View();
    }

    var message = new Message
    {
        SenderId = senderId,
        ReceiverId = receiverId!,
        Subject = subject!.Trim(),
        Content = content!.Trim(),
        IsRead = false,
        SentAt = DateTime.UtcNow
    };

    _context.Messages.Add(message);

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Your message has been sent.";

    return RedirectToAction(nameof(Index));
}

// GET: /Messages/Reply/5
// Opens the compose form addressed to the other participant.
public async Task<IActionResult> Reply(int id)
{
    var userId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var originalMessage = await _context.Messages
        .AsNoTracking()
        .FirstOrDefaultAsync(m =>
            m.Id == id &&
            (m.SenderId == userId ||
             m.ReceiverId == userId));

    if (originalMessage == null)
    {
        return NotFound();
    }

    // Reply to the other participant.
    var receiverId = originalMessage.SenderId == userId
        ? originalMessage.ReceiverId
        : originalMessage.SenderId;

    var subject = originalMessage.Subject.StartsWith(
        "Re: ",
        StringComparison.OrdinalIgnoreCase)
        ? originalMessage.Subject
        : $"Re: {originalMessage.Subject}";

    return RedirectToAction(
        nameof(Create),
        new
        {
            receiverId,
            subject
        });
}

// Loads available recipients for the compose form.
private async Task LoadRecipientsAsync(string? currentUserId)
{
    ViewBag.Users = await _userManager.Users
        .Where(u => u.Id != currentUserId)
        .OrderBy(u => u.FullName)
        .Select(u => new
        {
            u.Id,
            u.FullName,
            u.Email
        })
        .ToListAsync();
}

}
