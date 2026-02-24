using MediatR;
using Planova.API.Exceptions;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.EventManagement.GetEventBookings
{
	public sealed class GetEventBookingsHandler : IRequestHandler<GetEventBookingsQuery, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IBookingRepository _bookingRepository;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<GetEventBookingsHandler> _logger;

		public GetEventBookingsHandler(IEventRepository eventRepository, IBookingRepository bookingRepository, ICurrentUserService currentUser, ILogger<GetEventBookingsHandler> logger)
		{
			_eventRepository = eventRepository;
			_bookingRepository = bookingRepository;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(GetEventBookingsQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var evnt = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

				if (evnt is null)
				{
					_logger.LogWarning("Event with ID: {EventId} not found when attempting to retrieve bookings", request.EventId);
					return Result.Failure("Event not found.");
				}
			
				if (_currentUser.Role != UserRole.Admin && evnt.CreatorId != _currentUser.UserId)
				{
					_logger.LogWarning("User with ID: {UserId} is not authorized to view bookings for event ID: {EventId}", _currentUser.UserId, request.EventId);
					return Result.Failure("You are not authorized to view bookings.");
				}

				var bookings = await _bookingRepository.GetByEventIdAsync(request.EventId, cancellationToken);
				var bookingsDTO = bookings.Select(b => new BookingsListDto
				{
					Id = b.Id,
					Name = b.Name,
					Email = b.Email,
					PhoneNumber = b.PhoneNumber
				}).ToList();
				_logger.LogInformation("Successfully retrieved {Count} bookings for event ID: {EventId}", bookingsDTO.Count, request.EventId);
				return Result.Success(bookingsDTO);
			}
			catch (Exception ex)
			{
				throw new InfrastructureException("An error occurred while retrieving event bookings.", ex);
			}
		}
	}
}
