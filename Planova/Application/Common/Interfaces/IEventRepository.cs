using Planova.Application.EventManagement.GetOwnedEvents;
using Planova.Application.PublicDiscovery.GetEventById;


namespace Planova.Application.Common.Interfaces
{
	public interface IEventRepository
	{
		Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
		Task<List<EventDetailsDto>> GetAvailableEventsAsync(CancellationToken cancellationToken);
		Task<List<OwnedEventDto>> GetByCreatorAsync(Guid? creatorUserId, CancellationToken cancellationToken);
		Task<EventDetailsDto?> GetEventDetailsByIdAsync(Guid eventId, CancellationToken cancellationToken);


		Task<Guid> AddAsync(Event entity, CancellationToken cancellationToken);
		void Update(Event entity);
		void Remove(Event entity);
	}
}
