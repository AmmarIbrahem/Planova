using MediatR;
using Planova.Application.Common;

namespace Planova.Application.EventManagement.GetEventBookings
{
	public sealed record GetEventBookingsQuery(Guid EventId) : IRequest<Result>;
}
