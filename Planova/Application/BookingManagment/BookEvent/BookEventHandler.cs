using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Entities;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.BookingManagment.BookEvent
{
	public sealed class BookEventHandler : IRequestHandler<BookEventCommand, Result>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IBookingRepository _bookingRepository;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<BookEventHandler> _logger;


		public BookEventHandler(
			IEventRepository eventRepository,
			IBookingRepository bookingRepository,
			IUserRepository userRepository,
			IUnitOfWork unitOfWork,
			ICurrentUserService currentUser,
			ILogger<BookEventHandler> logger)
		{
			_eventRepository = eventRepository;
			_bookingRepository = bookingRepository;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
			_currentUser = currentUser;
			_logger = logger;
		}

		public async Task<Result> Handle(BookEventCommand request, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Attempting to book event with ID: {EventId} for participant with email: {Email}", request.EventId, request.Participant.Email);

			if (request.Participant is null)
			{
				_logger.LogWarning("Booking failed for event with ID: {EventId} - participant information is missing", request.EventId);
				return Result.Failure("Participant is required.");
			}

			if (string.IsNullOrWhiteSpace(request.Participant.Email))
			{
				_logger.LogWarning("Booking failed for event with ID: {EventId} - participant email is missing", request.EventId);
				return Result.Failure("Participant email is required.");
			}

			if (string.IsNullOrWhiteSpace(request.Participant.Name))
			{
				_logger.LogWarning("Booking failed for event with ID: {EventId} - participant name is missing", request.EventId);
				return Result.Failure("Participant name is required.");
			}

			if (string.IsNullOrWhiteSpace(request.Participant.PhoneNumber))
			{
				_logger.LogWarning("Booking failed for event with ID: {EventId} - participant phone number is missing", request.EventId);
				return Result.Failure("Participant phone number is required.");
			}
			try
			{
				var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
				if (eventEntity == null)
				{
					_logger.LogWarning("Booking failed for event with ID: {EventId} - event not found", request.EventId);
					return Result.Failure("Event not found.");
				}
				var eventCreatorUser = await _userRepository.GetByIdAsync(eventEntity.CreatorId, cancellationToken);
				if(eventCreatorUser is not null && eventCreatorUser.Email.ToLower().Equals(request.Participant.Email.ToLower()))
				{
					_logger.LogWarning("Booking failed for event with ID: {EventId} - participant email matches event creator email", request.EventId);	
					return Result.Failure("Event creators cannot book their own events.");
				}

				var exists = await _bookingRepository.ExistsByEmailAsync(request.EventId, request.Participant.Email, cancellationToken);
				if (exists)
				{
					_logger.LogWarning("Booking failed for event with ID: {EventId} - participant with email: {Email} has already booked this event", request.EventId, request.Participant.Email);
					return Result.Failure($"A participant with email {request.Participant.Email} has already booked this event.");
				}

				var currentParticipants =
					await _bookingRepository.GetByEventIdAsync(request.EventId, cancellationToken);

				if (currentParticipants.Count + 1  > eventEntity.Capacity)
					return Result.Failure("Event capacity exceeded.");

				var booking = new Booking(
					request.EventId, 
					request.Participant.Name, 
					request.Participant.Email, 
					request.Participant.PhoneNumber,
					_currentUser.UserId);

				await _bookingRepository.AddAsync(booking, cancellationToken);
				await _unitOfWork.SaveChangesAsync(cancellationToken);

				_logger.LogInformation("Booking created successfully for event with ID: {EventId} and participant with email: {Email}", request.EventId, request.Participant.Email);
				return Result.Success("Booking created successfully.");
			}
			catch(Exception ex)
			{
				throw new InfrastructureException("An error occurred while booking the event.", ex);
			}
		}
	}
}
