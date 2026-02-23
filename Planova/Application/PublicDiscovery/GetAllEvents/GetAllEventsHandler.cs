using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.PublicDiscovery.GetAllEvents
{
	public sealed class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, Result>
	{
		private readonly IEventRepository _repository;
		private readonly ILogger<GetAllEventsHandler> _logger;

		public GetAllEventsHandler(IEventRepository repository, ILogger<GetAllEventsHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Result> Handle(GetAllEventsQuery query, CancellationToken cancellationToken)
		{
			try
			{
				_logger.LogInformation("Handling GetAllEventsQuery");
				var events = await _repository.GetAvailableEventsAsync(cancellationToken);
				_logger.LogInformation("Successfully retrieved {Count} events for GetAllEventsQuery", events.Count);
				return Result.Success(events);
			}
			catch (Exception ex)
			{
				throw new InfrastructureException("Failed to retrieve events.", ex);
			}
		}
	}
}
