using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Planova.Domain.Entities;
using Planova.Domain.Enums;

namespace Planova.IntegrationTests.Testing;

public static class TestData
{
	public sealed record BaselineIds(Guid AdminId, Guid CreatorId, Guid Event1Id, Guid Event2Id);

	public static async Task<BaselineIds> SeedBaselineAsync(IServiceProvider sp)
	{
		var db = sp.GetRequiredService<AppDbContext>();

		var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@planova.com");
		var existingCreator = await db.Users.FirstOrDefaultAsync(u => u.Email == "creator@planova.com");
		if (existingAdmin != null && existingCreator != null)
		{
			var e1 = await db.Events.FirstOrDefaultAsync(e => e.Capacity == 50);
			var e2 = await db.Events.FirstOrDefaultAsync(e => e.Capacity == 10);
			if (e1 != null && e2 != null)
				return new BaselineIds(existingAdmin.Id, existingCreator.Id, e1.Id, e2.Id);
		}

		var passwordHash = GetPasswordHashForTests();

		var adminId = Guid.NewGuid();
		var creatorId = Guid.NewGuid();

		var admin = new User("admin@planova.com", passwordHash, UserRole.Admin);
		var creator = new User("creator@planova.com", passwordHash, UserRole.EventCreator);

		try
		{
			await db.Users.AddRangeAsync(admin, creator);
			await db.SaveChangesAsync();
			adminId = admin.Id;
			creatorId = creator.Id;
		}
		catch (DbUpdateException)
		{
			existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@planova.com");
			existingCreator = await db.Users.FirstOrDefaultAsync(u => u.Email == "creator@planova.com");
			if (existingAdmin != null && existingCreator != null)
			{
				adminId = existingAdmin.Id;
				creatorId = existingCreator.Id;
			}
			else
				throw;
		}

		Guid event1Id;
		Guid event2Id;
		var existingE1 = await db.Events.FirstOrDefaultAsync(e => e.Capacity == 50);
		var existingE2 = await db.Events.FirstOrDefaultAsync(e => e.Capacity == 10);
		if (existingE1 != null && existingE2 != null)
		{
			event1Id = existingE1.Id;
			event2Id = existingE2.Id;
		}
		else
		{
			var event1 = new Event(
				"Planova Kickoff",
				"Initial project kickoff meeting",
				"Berlin HQ",
				new DateTime(2027, 3, 1, 10, 0, 0),
				new DateTime(2027, 3, 1, 12, 0, 0),
				50,
				creatorId);

			var event2 = new Event(
				"Planova Workshop",
				"Hands-on development workshop",
				"Berlin HQ",
				new DateTime(2027, 3, 2, 14, 0, 0),
				new DateTime(2027, 3, 2, 17, 0, 0),
				10,
				creatorId);

			await db.Events.AddRangeAsync(event1, event2);
			await db.SaveChangesAsync();
			event1Id = event1.Id;
			event2Id = event2.Id;
		}

		var existingBookings = await db.Bookings.CountAsync(b => b.EventId == event2Id);
		if (existingBookings < 7)
		{
			var startIdx = existingBookings + 2;
			var bookingsForEvent2 = Enumerable.Range(startIdx, 7 - existingBookings).Select(i => new Booking(
				event2Id,
				$"Jane Smith",
				$"Jane{i}@a.com",
				"0987654321",
				null)).ToList();
			await db.Bookings.AddRangeAsync(bookingsForEvent2);
			await db.SaveChangesAsync();
		}

		return new BaselineIds(adminId, creatorId, event1Id, event2Id);
	}

	private static string GetPasswordHashForTests()
	{
		const int KeySize = 32;
		const int Iterations = 100_000;
		const string Delimiter = ";";

		byte[] fixedSalt = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
		var saltBase64 = Convert.ToBase64String(fixedSalt);
		const string password = "123456";

		var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
			password,
			fixedSalt,
			Iterations,
			HashAlgorithmName.SHA256,
			KeySize);

		var hashBase64 = Convert.ToBase64String(hashBytes);
		return $"{saltBase64}{Delimiter}{hashBase64}{Delimiter}{Iterations}";
	}
}
