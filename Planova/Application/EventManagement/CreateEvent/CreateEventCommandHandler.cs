using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.EventManagement.CreateEvent
{
	public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<CreateEventCommandHandler> _logger;

		public CreateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<CreateEventCommandHandler> logger)
		{
			_eventRepository = eventRepository;
			_unitOfWork = unitOfWork;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(CreateEventCommand request, CancellationToken cancellationToken)
		{
			_logger.LogInformation("User with ID: {UserId} is attempting to create an event with name: {EventName}", _currentUser.UserId, request.Name);

			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Event creation failed - user is not authenticated");
				return Result.Failure("User must be authenticated.");
			}

			if (_currentUser.Role != UserRole.Admin)
			{
				if (_currentUser.Role != UserRole.EventCreator || _currentUser.UserId != request.CreatorId)
				{
					_logger.LogWarning("Event creation failed - user with ID: {UserId} does not have permission to create events", _currentUser.UserId);
					return Result.Failure("You are not authorized to create events.");
				}
			}

			if (request.StartTime < DateTime.UtcNow)
			{
				_logger.LogWarning("Event creation failed - start time {StartTime} is in the past", request.StartTime);
				return Result.Failure("Event cannot start in the past.");
			}

			if (string.IsNullOrWhiteSpace(request.Name))
			{
				_logger.LogWarning("Event creation failed - event name is missing or empty");
				return Result.Failure("Event name is required.");
			}

			if (string.IsNullOrEmpty(request.CreatorId.ToString()))
			{
				_logger.LogWarning("Event {eventName} creation failed - event creator mus be defined.", request.Name);
				return Result.Failure("Event creator must be registered");
			}

			if (string.IsNullOrWhiteSpace(request.Description))
			{
				_logger.LogWarning("Event creation failed - event description is missing or empty");
				return Result.Failure("Event description is required.");
			}

			if (request.StartTime >= request.EndTime)
			{
				_logger.LogWarning("Event creation failed - start time {StartTime} is not before end time {EndTime}", request.StartTime, request.EndTime);
				return Result.Failure("Start time must be before end time.");
			}

			if (request.Capacity < 10)
			{
				_logger.LogWarning("Event creation failed - capacity {Capacity} is less than the minimum required", request.Capacity);
				return Result.Failure("Capacity must be at least 10.");
			}
			try
			{
				var entity = new Event(
					request.Name,
					request.Description,
					request.Location,
					request.StartTime,
					request.EndTime,
					request.Capacity,
					_currentUser.UserId.Value);

				var newEventId = await _eventRepository.AddAsync(entity, cancellationToken);
				await _unitOfWork.SaveChangesAsync(cancellationToken);
				var dto = new EventDto
				{
					Id = newEventId,
					Name = entity.Name,
					Description = entity.Description,
					Location = entity.Location,
					StartTime = entity.StartTime,
					EndTime = entity.EndTime,
					Capacity = entity.Capacity
				};
				_logger.LogInformation("Event with ID: {EventId} created successfully by user with ID: {UserId}", newEventId, _currentUser.UserId);
				return Result.Success(dto);
			}
			catch(Exception ex)
			{
				throw new InfrastructureException("An error occurred while creating the event.", ex);
			}
		}
	}
}
