using System.Diagnostics;
using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Entities;
using Planova.Infrastructure.Exceptions;
using Planova.Infrastructure.Observability;

namespace Planova.Application.BookingManagment.BookEvent;

public sealed class BookEventHandler : IRequestHandler<BookEventCommand, Result>
{
	private readonly IEventRepository _eventRepository;
	private readonly IBookingRepository _bookingRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ICurrentUserService _currentUser;
	private readonly PlanovaMetrics _metrics;
	private readonly ILogger<BookEventHandler> _logger;

	public BookEventHandler(
		IEventRepository eventRepository,
		IBookingRepository bookingRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ICurrentUserService currentUser,
		PlanovaMetrics metrics,
		ILogger<BookEventHandler> logger)
	{
		_eventRepository = eventRepository;
		_bookingRepository = bookingRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_currentUser = currentUser;
		_metrics = metrics;
		_logger = logger;
	}

	public async Task<Result> Handle(BookEventCommand request, CancellationToken cancellationToken)
	{
		_metrics.RecordBookingAttempt();
		var sw = Stopwatch.StartNew();

		try
		{
			if (request.Participant is null)
				return Fail(request.EventId, "missing_participant", "Participant is required.");

			if (string.IsNullOrWhiteSpace(request.Participant.Email))
				return Fail(request.EventId, "missing_email", "Participant email is required.");

			if (string.IsNullOrWhiteSpace(request.Participant.Name))
				return Fail(request.EventId, "missing_name", "Participant name is required.");

			if (string.IsNullOrWhiteSpace(request.Participant.PhoneNumber))
				return Fail(request.EventId, "missing_phone", "Participant phone number is required.");

			var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
			if (eventEntity is null)
				return Fail(request.EventId, "event_not_found", "Event not found.");

			var creator = await _userRepository.GetByIdAsync(eventEntity.CreatorId, cancellationToken);
			if (creator is not null &&
				creator.Email.Equals(request.Participant.Email, StringComparison.OrdinalIgnoreCase))
				return Fail(request.EventId, "creator_self_booking", "Event creators cannot book their own events.");

			var alreadyBooked = await _bookingRepository.ExistsByEmailAsync(
				request.EventId, request.Participant.Email, cancellationToken);
			if (alreadyBooked)
				return Fail(request.EventId, "duplicate_booking",
					$"A participant with email {request.Participant.Email} has already booked this event.");

			var bookings = await _bookingRepository.GetByEventIdAsync(request.EventId, cancellationToken);
			if (bookings.Count >= eventEntity.Capacity)
				return Fail(request.EventId, "capacity_exceeded", "Event capacity exceeded.");

			var booking = new Booking(
				request.EventId,
				request.Participant.Name,
				request.Participant.Email,
				request.Participant.PhoneNumber,
				_currentUser.UserId);

			await _bookingRepository.AddAsync(booking, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			sw.Stop();
			_metrics.RecordBookingSuccess();
			_metrics.RecordBookingDuration(sw.Elapsed.TotalMilliseconds);

			_logger.LogInformation(
				"Booking created. EventId={EventId} Email={Email} DurationMs={DurationMs}",
				request.EventId, request.Participant.Email, sw.Elapsed.TotalMilliseconds);

			return Result.Success("Booking created successfully.");
		}
		catch (Exception ex)
		{
			sw.Stop();
			_metrics.RecordBookingFailure("exception");
			throw new InfrastructureException("An error occurred while booking the event.", ex);
		}
	}

	private Result Fail(Guid eventId, string reason, string message)
	{
		_metrics.RecordBookingFailure(reason);
		_logger.LogWarning(
			"Booking rejected. EventId={EventId} Reason={Reason} Message={Message}",
			eventId, reason, message);
		return Result.Failure(message);
	}
}