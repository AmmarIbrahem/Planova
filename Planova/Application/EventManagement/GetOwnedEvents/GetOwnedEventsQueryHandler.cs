using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.EventManagement.GetOwnedEvents
{
	public class GetOwnedEventsQueryHandler : IRequestHandler<GetOwnedEventsQuery, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<GetOwnedEventsQueryHandler> _logger;

		public GetOwnedEventsQueryHandler(
			IEventRepository eventRepository,
			ICurrentUserService currentUser,
			ILogger<GetOwnedEventsQueryHandler> logger)
		{
			_eventRepository = eventRepository;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(GetOwnedEventsQuery request, CancellationToken cancellationToken)
		{
			if (_currentUser.UserId == null)
			{ 
				_logger.LogWarning("Unauthorized access attempt to GetOwnedEventsQuery: User is not authenticated.");
				return Result.Failure("User must be authenticated.");
			}

			var role = _currentUser.Role;
			var userId = _currentUser.UserId.Value;

			if (role != UserRole.Admin && role != UserRole.EventCreator)
			{
				_logger.LogWarning("Unauthorized access attempt to GetOwnedEventsQuery: User ID {UserId} with role {Role} does not have permission.", userId, role);
				return Result.Failure("Unauthorized User.");
			}
			try
			{
				_logger.LogInformation("Handling GetOwnedEventsQuery for user ID: {UserId} with role: {Role}", userId, role);
				
				Guid? creatorId = role == UserRole.Admin ? null : userId;

				_logger.LogInformation("Retrieving events for user ID: {UserId} with role: {Role}. Creator ID filter: {CreatorId}", userId, role, creatorId);
				var events = await _eventRepository.GetByCreatorAsync(creatorId, cancellationToken);
				return Result.Success(events);
			}
			catch (Exception ex)
			{
				throw new InfrastructureException("Failed to retrieve owned events.", ex);
			}
		}
	}
}

