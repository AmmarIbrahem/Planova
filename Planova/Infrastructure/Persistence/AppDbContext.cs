using Microsoft.EntityFrameworkCore;
using Planova.Domain.Entities;
using Planova.Domain.Enums;
using System.Security.Cryptography;

namespace Planova.Infrastructure.Persistence
{
	public sealed class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options)
		{
		}

		public DbSet<User> Users => Set<User>();
		public DbSet<Event> Events => Set<Event>();
		public DbSet<Booking> Bookings => Set<Booking>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

			ConfigureUser(modelBuilder);
			ConfigureEvent(modelBuilder);
			ConfigureBookings(modelBuilder);
		}

		private static void ConfigureUser(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(x => x.Id);
				
				entity.Property(x => x.Email)
					  .IsRequired()
					  .HasMaxLength(200);

				entity.HasIndex(x => x.Email)
					  .IsUnique();

				entity.Property(x => x.PasswordHash)
					  .IsRequired()
					  .HasMaxLength(256);

				entity.Property(x => x.Role)
					  .IsRequired()
					  .HasConversion<string>();
			});
		}

		private static void ConfigureEvent(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Event>(entity =>
			{
				entity.HasKey(e => e.Id);

				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.Property(e => e.Description).IsRequired();
				entity.Property(e => e.Location).IsRequired().HasMaxLength(200);

				entity.HasOne<User>()
					  .WithMany()
					  .HasForeignKey(e => e.CreatorId)
					  .OnDelete(DeleteBehavior.Restrict);
			});
		}

		private static void ConfigureBookings(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Booking>(entity =>
			{
				entity.HasKey(x => x.Id);
				entity.Property(b => b.Name)
			.IsRequired()
			.HasMaxLength(150);

				entity.Property(b => b.Email)
					.IsRequired()
					.HasMaxLength(200);

				entity.Property(b => b.PhoneNumber)
					.IsRequired()
					.HasMaxLength(50);

				entity.HasIndex(b => b.Email);

				entity.HasOne<Event>()
					.WithMany()
					.HasForeignKey(b => b.EventId)
					.OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}
