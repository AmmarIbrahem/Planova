using MediatR;
using Planova.Application.Common;

namespace Planova.Application.EventManagement.UpdateEvent
{
	public sealed record UpdateEventCommand(
		Guid EventId,
		string Name,
		string Description,
		string Location,
		DateTime StartTime,
		DateTime EndTime,
		int Capacity
	) : IRequest<Result>;

}
