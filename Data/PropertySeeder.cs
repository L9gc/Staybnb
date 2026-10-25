using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

public static class PropertySeeder
{
public static async Task SeedAsync(ApplicationDbContext context)
{
// Avoid adding the same sample properties every time the app starts.
if (await context.Properties.AnyAsync())
{
return;
}


    // Find a user to associate with the sample properties.
    var host = await context.Users.FirstOrDefaultAsync();

    if (host == null)
    {
        return;
    }

    var properties = new List<Property>
    {
        new Property
        {
            Title = "Modern City Apartment",
            Description = "A comfortable modern apartment close to restaurants, shops and city attractions. Ideal for a short city break.",
            PricePerNight = 850m,
            CleaningFee = 250m,
            ServiceFee = 100m,
            Address = "10 Example Street",
            City = "Johannesburg",
            PropertyType = "Apartment",
            IsActive = true,
            HostId = host.Id
        },

        new Property
        {
            Title = "Coastal Holiday Home",
            Description = "A spacious holiday home suitable for families and small groups looking for a relaxing coastal getaway.",
            PricePerNight = 1450m,
            CleaningFee = 350m,
            ServiceFee = 150m,
            Address = "25 Beach Road",
            City = "Durban",
            PropertyType = "House",
            IsActive = true,
            HostId = host.Id
        },

        new Property
        {
            Title = "Cosy Weekend Cottage",
            Description = "A cosy cottage offering a peaceful setting for couples or solo travellers looking for a relaxing escape.",
            PricePerNight = 650m,
            CleaningFee = 180m,
            ServiceFee = 80m,
            Address = "5 Garden Lane",
            City = "Pretoria",
            PropertyType = "Cottage",
            IsActive = true,
            HostId = host.Id
        }
    };

    context.Properties.AddRange(properties);

    await context.SaveChangesAsync();
}

}
