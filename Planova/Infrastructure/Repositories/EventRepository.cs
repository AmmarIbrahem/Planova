using Microsoft.EntityFrameworkCore;
using Planova.Application.Common.Interfaces;
using Planova.Application.EventManagement.GetOwnedEvents;
using Planova.Application.PublicDiscovery.GetEventById;

namespace Planova.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
	private readonly AppDbContext _context;
	private readonly ILogger<EventRepository> _logger;

	public EventRepository(AppDbContext context, ILogger<EventRepository> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
	{
		var events = await _context.Events
			.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

		_logger.LogDebug("Retrieved event with ID {EventId}: {EventName}", id, events?.Name ?? "Not Found");

		return events;
	}

	public async Task<List<EventDetailsDto>> GetAvailableEventsAsync(CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;
		var avaialbleEvents = await _context.Events
			.AsNoTracking()
			.Where(e => e.EndTime > now) 
			.Select(e => new EventDetailsDto
			{
				Id = e.Id,
				Name = e.Name,
				Location = e.Location,
				Description = e.Description,
				StartTime = e.StartTime,
				EndTime = e.EndTime,
				Capacity = e.Capacity,
				CurrentParticipants = e.Bookings.Count(),
			})
			.Where(e => e.CurrentParticipants < e.Capacity)
			.ToListAsync(cancellationToken);

		_logger.LogDebug("Retrieved {Count} available events", avaialbleEvents.Count);

		return avaialbleEvents;
	}

	public async Task<List<OwnedEventDto>> GetByCreatorAsync(Guid? creatorId, CancellationToken cancellationToken)
	{
		var query = _context.Events.AsNoTracking();
		if (creatorId.HasValue)
		{
			query = query.Where(e => e.CreatorId == creatorId.Value);
		}

		var ownedEvents = await query
			.Select(e => new OwnedEventDto
			{
				Id = e.Id,
				Name = e.Name,
				Description = e.Description,
				Location = e.Location,
				StartTime = e.StartTime,
				EndTime = e.EndTime,
				Capacity = e.Capacity,
				TotalParticipants = e.Bookings.Count(),  // navigation property
				Bookings = e.Bookings.Select(b => new BookingDto
				{
					BookingId = b.Id,
					BookerUserId = b.BookerUserId,
					Participant = new BookingParticipantDto
					{
						Name = b.Name,
						Email = b.Email,
						PhoneNumber = b.PhoneNumber
					}
				}).ToList()
			})
			.OrderByDescending(e => e.StartTime)
			.ToListAsync(cancellationToken);

		_logger.LogDebug("Retrieved {Count} owned events for creator ID: {CreatorId}", ownedEvents.Count, creatorId.HasValue ? creatorId.Value.ToString() : "All (Admin)");

		return ownedEvents;
	}

	public async Task<EventDetailsDto?> GetEventDetailsByIdAsync(Guid eventId, CancellationToken cancellationToken)
	{
		var events =  await _context.Events
			.Where(e => e.Id == eventId)
			.Select(e => new EventDetailsDto
			{
				Id = e.Id,
				Name = e.Name,
				Description = e.Description,
				Location = e.Location,
				StartTime = e.StartTime,
				EndTime =e.EndTime,
				Capacity = e.Capacity,
				CurrentParticipants = e.Bookings.Count(),
				Creator = new CreatorDto
				{
					Id = e.CreatorId,
					Email = _context.Users
						.Where(u => u.Id == e.CreatorId)
						.Select(u => u.Email)
						.FirstOrDefault()!
				}
			})
			.FirstOrDefaultAsync(cancellationToken);

		_logger.LogDebug("Retrieved public details for event ID: {EventId}: {EventName}", eventId, events?.Name ?? "Not Found");
		return events;
	}

	public async Task<Guid> AddAsync(Event entity, CancellationToken cancellationToken)
	{
		await _context.Events.AddAsync(entity, cancellationToken);
		_logger.LogDebug("Added new event: {EventName} with ID: {EventId}", entity.Name, entity.Id);

		return entity.Id;
	}

	public void Update(Event entity)
	{
		_context.Events.Update(entity);
		_logger.LogDebug("Updated event with ID: {EventId}", entity.Id);
	}

	public void Remove(Event entity)
	{
		_context.Events.Remove(entity);
		_logger.LogDebug("Removed event with ID: {EventId}", entity.Id);
	}

}
