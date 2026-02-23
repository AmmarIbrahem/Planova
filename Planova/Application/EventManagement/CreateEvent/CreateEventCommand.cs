using MediatR;
using Planova.Application.Common;

namespace Planova.Application.EventManagement.CreateEvent
{
	public sealed record CreateEventCommand(
		string Name,
		string Description,
		string Location,
		DateTime StartTime,
		DateTime EndTime,
		int Capacity,
		Guid CreatorId
	) : IRequest<Result>;
}
