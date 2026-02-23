using MediatR;
using Planova.Application.Common;

namespace Planova.Application.EventManagement.GetOwnedEvents
{
	public class GetOwnedEventsQuery : IRequest<Result>
	{
		// Optional: could add filters later (date range, pagination)
	}
}
