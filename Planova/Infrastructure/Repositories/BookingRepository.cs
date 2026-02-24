using Microsoft.EntityFrameworkCore;
using Planova.Application.Common.Interfaces;
using Planova.Application.EventManagement.GetEventBookings;
using Planova.Domain.Entities;
using System.Linq;
using System.Threading;

namespace Planova.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
	private readonly AppDbContext _context;
	private readonly ILogger<BookingRepository> _logger;

	public BookingRepository(AppDbContext context, ILogger<BookingRepository> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<List<Booking>> GetByEventIdAsync(
		Guid eventId,
		CancellationToken cancellationToken)
	{
		var bookings =  
			await _context.Bookings
			.Where(p => p.EventId == eventId)
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		_logger.LogDebug("Successfully retrieved bookings for event ID: {EventId}", eventId);
		return bookings;
	}

	public async Task<int> GetCountByEventIdAsync (Guid eventId, CancellationToken cancellationToken)
	{
		var bookingsCount =
			await _context.Bookings
			.Where(p => p.EventId == eventId)
			.AsNoTracking()
			.CountAsync(cancellationToken);

		_logger.LogDebug("Successfully retrieved {Count} bookings for event ID: {EventId}", bookingsCount, eventId);
		return bookingsCount;
	}


	public async Task<Guid> AddAsync(Booking entity, CancellationToken cancellationToken)
	{
		await _context.Bookings.AddAsync(entity, cancellationToken);
		_logger.LogDebug("Successfully added booking for event ID: {EventId} with email: {Email}", entity.EventId, entity.Email);

		return entity.Id;
	}

	public async Task<bool> ExistsByEmailAsync(Guid eventId, string email, CancellationToken cancellationToken)
	{
		var existsByEmail =  await _context.Bookings
		.Where(bp => bp.EventId == eventId)
		.AnyAsync(bp => bp.Email.ToLower() == email.ToLower(), cancellationToken);
		_logger.LogDebug("Booking existence check for event ID: {EventId} with email: {Email} returned: {Exists}", eventId, email, existsByEmail);
		return existsByEmail;
	}
}
