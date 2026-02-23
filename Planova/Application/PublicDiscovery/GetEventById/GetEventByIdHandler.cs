using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.PublicDiscovery.GetEventById;

public sealed class GetEventByIdHandler : IRequestHandler<GetEventByIdQuery, Result>
{
	private readonly IEventRepository _eventRepository;
	private readonly ILogger<GetEventByIdHandler> _logger;

	public GetEventByIdHandler(IEventRepository eventRepository, ILogger<GetEventByIdHandler> logger)
	{
		_eventRepository = eventRepository;
		_logger = logger;
	}

	public async Task<Result> Handle(GetEventByIdQuery query, CancellationToken ct)
	{
		try
		{
			_logger.LogInformation("Handling GetEventByIdQuery for event ID: {EventId}", query.Id);
			var eventDto = await _eventRepository.GetEventDetailsByIdAsync(query.Id, ct);
			if (eventDto is null)
			{
				_logger.LogWarning("Event with ID: {EventId} not found", query.Id);
				return Result.Failure("Event.NotFound");
			}

			_logger.LogInformation("Successfully retrieved event with ID: {EventId}", query.Id);
			return Result.Success(eventDto);
		}
		catch (Exception ex)
		{
			throw new InfrastructureException($"An error occurred while retrieving event with ID: {query.Id}", ex);
		}
	}
}
