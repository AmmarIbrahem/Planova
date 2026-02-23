using MediatR;
using Planova.Application.Common;

namespace Planova.Application.PublicDiscovery.GetEventById;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<Result>;
