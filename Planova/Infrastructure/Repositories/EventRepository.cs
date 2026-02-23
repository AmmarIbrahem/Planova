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
		_logger.LogInformation("Retrieving event with ID {EventId}", id);
		
		var events = await _context.Events
			.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

		_logger.LogInformation("Retrieved event with ID {EventId}: {EventName}", id, events?.Name ?? "Not Found");

		return events;
	}

	public async Task<List<EventDetailsDto>> GetAvailableEventsAsync(CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;
		_logger.LogInformation("Retrieving available events at {CurrentTime}", now);
		var avaialbleEvents = await _context.Events
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
				CurrentParticipants = _context.Bookings
					.Where(b => b.EventId == e.Id)
					.Count()
			})
			.Where(e => e.CurrentParticipants < e.Capacity)
			.ToListAsync(cancellationToken);

		_logger.LogInformation("Retrieved {Count} available events", avaialbleEvents.Count);

		return avaialbleEvents;
	}

	public async Task<List<OwnedEventDto>> GetByCreatorAsync(Guid? creatorId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Retrieving events for creator ID: {CreatorId}", creatorId.HasValue ? creatorId.Value.ToString() : "All (Admin)");
		var query = _context.Events.AsNoTracking();
		if (creatorId.HasValue)
		{
			query = query.Where(e => e.CreatorId == creatorId.Value);
		}

		var ownedEvents = await query
			.GroupJoin(
				_context.Bookings.AsNoTracking(), 
				e => e.Id,
				b => b.EventId,
				(e, bookings) => new OwnedEventDto
				{
					Id = e.Id,
					Name = e.Name,
					Description = e.Description,
					Location = e.Location,
					StartTime = e.StartTime,
					EndTime = e.EndTime,
					Capacity = e.Capacity,

					TotalParticipants = bookings.Count(),

					Bookings = bookings.Select(b => new BookingDto
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

		_logger.LogInformation("Retrieved {Count} owned events for creator ID: {CreatorId}", ownedEvents.Count, creatorId.HasValue ? creatorId.Value.ToString() : "All (Admin)");

		return ownedEvents;
	}

	public async Task<EventDetailsDto?> GetEventDetailsByIdAsync(Guid eventId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Retrieving public details for event ID: {EventId}", eventId);
		
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
				CurrentParticipants = 
					_context.Bookings
						.Where(b => b.EventId == e.Id)
						.Count(),

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

		_logger.LogInformation("Retrieved public details for event ID: {EventId}: {EventName}", eventId, events?.Name ?? "Not Found");
		return events;
	}

	public async Task<Guid> AddAsync(Event entity, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding new event: {EventName} by creator ID: {CreatorId}", entity.Name, entity.CreatorId);
		await _context.Events.AddAsync(entity, cancellationToken);
		_logger.LogInformation("Added new event: {EventName} with ID: {EventId}", entity.Name, entity.Id);

		return entity.Id;
	}

	public void Update(Event entity)
	{
		_logger.LogInformation("Updating event with ID: {EventId}", entity.Id);
		_context.Events.Update(entity);
		_logger.LogInformation("Updated event with ID: {EventId}", entity.Id);
	}

	public void Remove(Event entity)
	{
		_logger.LogInformation("Removing event with ID: {EventId}", entity.Id);
		_context.Events.Remove(entity);
		_logger.LogInformation("Removed event with ID: {EventId}", entity.Id);
	}

}
