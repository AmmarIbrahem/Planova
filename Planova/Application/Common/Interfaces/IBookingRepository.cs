using Planova.Domain.Entities;

namespace Planova.Application.Common.Interfaces
{
	public interface IBookingRepository
	{
		//Task<List<Event>> GetByEmailAsync(string email, CancellationToken cancellationToken);
		//Task<List<Event>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
		Task<List<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
		//Task<List<string>> GetEmailsByEventAsync(Guid eventId, CancellationToken cancellationToken);


		Task<Guid> AddAsync(Booking entity, CancellationToken cancellationToken);
		Task<bool> ExistsByEmailAsync(Guid eventId, string email, CancellationToken cancellationToken);
		//Task<bool> ExistsByUserIdAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
		
		//void Update(Booking entity);
		//void Remove(Booking entity);
	}
}
