using MediatR;
using Planova.Application.Common;

namespace Planova.Application.EventManagement.DeleteEvent
{
	public record DeleteEventCommand(Guid EventId) : IRequest<Result>;

}
