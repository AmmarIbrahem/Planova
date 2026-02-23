using MediatR;
using Planova.Application.Common;

namespace Planova.Application.PublicDiscovery.GetAllEvents
{
	public sealed record GetAllEventsQuery : IRequest<Result>;
}
