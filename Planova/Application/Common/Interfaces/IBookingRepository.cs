using Planova.Domain.Entities;

namespace Planova.Application.Common.Interfaces
{
	public interface IBookingRepository
	{
		Task<List<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

		Task<Guid> AddAsync(Booking entity, CancellationToken cancellationToken);
		Task<bool> ExistsByEmailAsync(Guid eventId, string email, CancellationToken cancellationToken);
	}
}
