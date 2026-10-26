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
    // Displays received messages.
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
    // Displays messages sent by the current user.
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
    public async Task<IActionResult> Create(string? receiverId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        ViewBag.ReceiverId = receiverId;

        return View();
    }

    // POST: /Messages/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string receiverId,
        string subject,
        string content)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(senderId))
        {
            return Challenge();
        }

        // Validate the message fields.
        if (string.IsNullOrWhiteSpace(receiverId) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Please complete all message fields.");
        }

        if (subject?.Length > 150)
        {
            ModelState.AddModelError("subject", "The subject cannot exceed 150 characters.");
        }

        if (content?.Length > 5000)
        {
            ModelState.AddModelError("content", "The message cannot exceed 5000 characters.");
        }

        // Confirm the recipient exists and isn't the sender.
        var receiverExists = !string.IsNullOrWhiteSpace(receiverId) &&
            receiverId != senderId &&
            await _userManager.Users.AnyAsync(u => u.Id == receiverId);

        if (!receiverExists)
        {
            ModelState.AddModelError("receiverId", "Please select a valid recipient.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _userManager.Users
                .Where(u => u.Id != senderId)
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToListAsync();

            ViewBag.ReceiverId = receiverId;
            return View();
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Subject = subject.Trim(),
            Content = content.Trim(),
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your message has been sent.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Messages/Reply/5
    public async Task<IActionResult> Reply(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var originalMessage = await _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.Id == id &&
                (m.SenderId == userId || m.ReceiverId == userId));

        if (originalMessage == null)
        {
            return NotFound();
        }

        // Reply to the person who sent the original message,
        // unless the current user sent it.
        var receiverId = originalMessage.SenderId == userId
            ? originalMessage.ReceiverId
            : originalMessage.SenderId;

        return RedirectToAction(
            nameof(Create),
            new
            {
                receiverId,
                subject = $"Re: {originalMessage.Subject}"
            });
    }
}