using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.EventManagement.DeleteEvent
{
	public sealed class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<DeleteEventCommandHandler> _logger;

		public DeleteEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<DeleteEventCommandHandler> logger)
		{
			_eventRepository = eventRepository;
			_unitOfWork = unitOfWork;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(
			DeleteEventCommand request,
			CancellationToken cancellationToken)
		{
			var entity = await _eventRepository.GetByIdAsync(
				request.EventId,
				cancellationToken);

			if (entity is null)
			{
				_logger.LogWarning("Event with ID {EventId} not found for deletion", request.EventId);
				return Result.Failure("Event not found.");
			}

			if (_currentUser.Role != UserRole.Admin && entity.CreatorId != _currentUser.UserId)
			{
				_logger.LogWarning("User with ID {UserId} is not authorized to delete event with ID {EventId}", _currentUser.UserId, request.EventId);
				return Result.Failure("You are not authorized to delete this event.");
			}
			try
			{
				_eventRepository.Remove(entity);
				await _unitOfWork.SaveChangesAsync(cancellationToken);

				_logger.LogInformation("Successfully deleted event with ID {EventId}", request.EventId);

				return Result.Success(entity.Id);
			}
			catch (Exception ex)
			{
				throw new InfrastructureException("An error occurred while deleting the event.", ex);
			}
		}
	}
}
