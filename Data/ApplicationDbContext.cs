using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
public ApplicationDbContext(
DbContextOptions<ApplicationDbContext> options)
: base(options)
{
}

public DbSet<Property> Properties => Set<Property>();

public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();

public DbSet<HostApplication> HostApplications => Set<HostApplication>();

public DbSet<Booking> Bookings => Set<Booking>();

public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();

public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

public DbSet<Message> Messages => Set<Message>();

public DbSet<Notification> Notifications => Set<Notification>();

public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // A property belongs to one host.
    builder.Entity<Property>()
        .HasOne(p => p.Host)
        .WithMany()
        .HasForeignKey(p => p.HostId)
        .OnDelete(DeleteBehavior.Restrict);

    // A property can have multiple images.
    builder.Entity<PropertyImage>()
        .HasOne(i => i.Property)
        .WithMany(p => p.Images)
        .HasForeignKey(i => i.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);

    // A host application belongs to its applicant.
    builder.Entity<HostApplication>()
        .HasOne(a => a.Applicant)
        .WithMany()
        .HasForeignKey(a => a.ApplicantId)
        .OnDelete(DeleteBehavior.Restrict);

    // An image can belong to a host application.
    builder.Entity<PropertyImage>()
        .HasOne(i => i.HostApplication)
        .WithMany(a => a.Images)
        .HasForeignKey(i => i.HostApplicationId)
        .OnDelete(DeleteBehavior.Cascade);

    // A booking belongs to a guest.
    builder.Entity<Booking>()
        .HasOne(b => b.Guest)
        .WithMany()
        .HasForeignKey(b => b.GuestId)
        .OnDelete(DeleteBehavior.Restrict);

    // A booking refers to a property.
    builder.Entity<Booking>()
        .HasOne(b => b.Property)
        .WithMany()
        .HasForeignKey(b => b.PropertyId)
        .OnDelete(DeleteBehavior.Restrict);

    // A property has one check-in configuration.
    builder.Entity<CheckInProcess>()
        .HasOne(c => c.Property)
        .WithMany()
        .HasForeignKey(c => c.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);

    // A booking can have multiple guest documents.
    builder.Entity<GuestDocument>()
        .HasOne(d => d.Booking)
        .WithMany(b => b.GuestDocuments)
        .HasForeignKey(d => d.BookingId)
        .OnDelete(DeleteBehavior.Cascade);

    // Messages have separate sender and receiver relationships.
    builder.Entity<Message>()
        .HasOne(m => m.Sender)
        .WithMany()
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Message>()
        .HasOne(m => m.Receiver)
        .WithMany()
        .HasForeignKey(m => m.ReceiverId)
        .OnDelete(DeleteBehavior.Restrict);

    // A notification belongs to one user.
    builder.Entity<Notification>()
        .HasOne(n => n.User)
        .WithMany()
        .HasForeignKey(n => n.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Prevent multiple check-in configurations for one property.
    builder.Entity<CheckInProcess>()
        .HasIndex(c => c.PropertyId)
        .IsUnique();
}

}
