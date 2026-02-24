using Microsoft.EntityFrameworkCore;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Entities;
using Planova.Domain.Enums;
using Planova.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace Planova.Infrastructure.Persistence.DevSeed;

public static class DevUserSeeder
{
	public static async Task SeedAsync(IServiceProvider sp)
	{
		var context = sp.GetRequiredService<AppDbContext>();
		var hasher = sp.GetRequiredService<IPasswordHasher>();

		await context.Database.MigrateAsync();

		if (await context.Users.AnyAsync())
			return;

		string passwordHash = hasher.Hash("123456");

		// -------------------------------
		// Users
		// -------------------------------

		var admin = new User("admin@planova.com", passwordHash, UserRole.Admin);
		var creator = new User("creator@planova.com", passwordHash, UserRole.EventCreator);
		var participant1 = new User("participant1@planova.com", passwordHash, UserRole.Participant);
		var participant2 = new User("participant2@planova.com", passwordHash, UserRole.Participant);

		await context.Users.AddRangeAsync(admin, creator, participant1, participant2);
		await context.SaveChangesAsync();

		// -------------------------------
		// Events
		// -------------------------------

		var event1 = new Event(
			"Planova Kickoff",
			"Initial project kickoff meeting",
			"Berlin HQ",
			new DateTime(2026, 3, 1, 10, 0, 0),
			new DateTime(2026, 3, 1, 12, 0, 0),
			50,
			creator.Id
		);

		var event2 = new Event(
			"Planova Workshop",
			"Hands-on development workshop",
			"Berlin HQ",
			new DateTime(2026, 3, 2, 14, 0, 0),
			new DateTime(2026, 3, 2, 17, 0, 0),
			10,
			creator.Id
		);

		await context.Events.AddRangeAsync(event1, event2);
		await context.SaveChangesAsync();

		// -------------------------------
		// Bookings
		// -------------------------------

		var bookings = new List<Booking>
		{
			new Booking(event1.Id, "John Doe", "john@a.com", "1234567890", participant1.Id),
			new Booking(event1.Id, "Jane Smith", "jane@a.com", "0987654321", participant2.Id),

			new Booking(event2.Id, "Jane2", "jane2@a.com", "0987654321"),
			new Booking(event2.Id, "Jane3", "jane3@a.com", "0987654321"),
			new Booking(event2.Id, "Jane4", "jane4@a.com", "0987654321"),
			new Booking(event2.Id, "Jane5", "jane5@a.com", "0987654321"),
			new Booking(event2.Id, "Jane6", "jane6@a.com", "0987654321"),
			new Booking(event2.Id, "Jane7", "jane7@a.com", "0987654321"),
			new Booking(event2.Id, "Jane8", "jane8@a.com", "0987654321"),
		};

		await context.Bookings.AddRangeAsync(bookings);
		await context.SaveChangesAsync();
	}
}
