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
		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

		// Ensure database created
		await context.Database.MigrateAsync();

		// Prevent duplicate seeding
		if (await context.Users.AnyAsync())
			return;

		const int KeySize = 32;
		const int Iterations = 100_000;
		const string Delimiter = ";";

		byte[] fixedSalt = new byte[16]
			{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

		var saltBase64 = Convert.ToBase64String(fixedSalt);

		string password = "123456";

		var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
			password,
			fixedSalt,
			Iterations,
			HashAlgorithmName.SHA256,
			KeySize
		);

		var hashBase64 = Convert.ToBase64String(hashBytes);
		string passwordHash = $"{saltBase64}{Delimiter}{hashBase64}{Delimiter}{Iterations}";

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
