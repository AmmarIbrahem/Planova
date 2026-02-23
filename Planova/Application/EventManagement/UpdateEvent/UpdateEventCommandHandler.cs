using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.EventManagement.UpdateEvent
{
	public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<UpdateEventCommandHandler> _logger;

		public UpdateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<UpdateEventCommandHandler> logger)
		{
			_eventRepository = eventRepository;
			_unitOfWork = unitOfWork;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
		{
			try
			{
				_logger.LogInformation("Handling UpdateEventCommand for event ID: {EventId} by user ID: {UserId}", request.EventId, _currentUser.UserId);
				var entity = await _eventRepository.GetByIdAsync(
					request.EventId,
					cancellationToken);

				if (entity is null)
				{
					_logger.LogWarning("Event with ID {EventId} not found for update", request.EventId);
					return Result.Failure(("Event not found."));
				}

				if (_currentUser.Role != UserRole.Admin && entity.CreatorId != _currentUser.UserId)
				{
					_logger.LogWarning("User with ID {UserId} is not authorized to update event with ID {EventId}", _currentUser.UserId, request.EventId);
					return Result.Failure("You are not authorized to update this event.");
				}

				if (request.StartTime < DateTime.UtcNow)
				{
					_logger.LogWarning("Event update failed - start time {StartTime} is in the past for event ID {EventId}", request.StartTime, request.EventId);
					return Result.Failure("Event cannot start in the past.");
				}

				if (string.IsNullOrWhiteSpace(request.Name))
				{
					_logger.LogWarning("Event update failed - event name is missing or empty for event ID {EventId}", request.EventId);
					return Result.Failure("Event name is required.");
				}

				if (string.IsNullOrWhiteSpace(request.Description))
				{
					_logger.LogWarning("Event update failed - event description is missing or empty for event ID {EventId}", request.EventId);
					return Result.Failure("Event description is required.");
				}

				if (request.StartTime >= request.EndTime)
				{
					_logger.LogWarning("Event update failed - start time {StartTime} is not before end time {EndTime} for event ID {EventId}", request.StartTime, request.EndTime, request.EventId);	
					return Result.Failure("Start time must be before end time.");
				}

				if (request.Capacity < 10)
				{
					_logger.LogWarning("Event update failed - capacity {Capacity} is less than the minimum required for event ID {EventId}", request.Capacity, request.EventId);
					return Result.Failure("Capacity must be at least 10.");
				}

				entity.Update(
					request.Name,
					request.Description,
					request.Location,
					request.StartTime,
					request.EndTime,
					request.Capacity);
				_eventRepository.Update(entity);
				
				await _unitOfWork.SaveChangesAsync(cancellationToken);			
				_logger.LogInformation("Successfully updated event with ID {EventId}", request.EventId);

				return Result.Success(entity.Id);
			}
			catch (Exception ex)
			{
				throw new InfrastructureException("An error occurred while updating the event.", ex);
			}
		}
	}
}
