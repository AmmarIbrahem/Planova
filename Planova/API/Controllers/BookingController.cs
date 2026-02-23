using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Planova.API.DTOs.Booking;
using Planova.Application.BookingManagment.BookEvent;

namespace Planova.API.Controllers
{
	/// <summary>
	/// Handles event booking operations.
	/// </summary>
	/// <remarks>
	/// This endpoint allows users to book an event without requiring authentication.
	/// Business rules such as capacity validation are handled in the Application layer.
	/// </remarks>
	[ApiController]
	[Route("api/bookings")]
	[EnableRateLimiting("fixed")]
	[Produces("application/json")]
	public sealed class BookingController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly ILogger<BookingController> _logger;

		public BookingController(IMediator mediator, ILogger<BookingController> logger)
		{
			_mediator = mediator;
			_logger = logger;
		}

		/// <summary>
		/// Books a participant into a specific event.
		/// </summary>
		/// <param name="eventId">The unique identifier of the event.</param>
		/// <param name="request">Participant booking details.</param>
		/// <param name="cancellationToken">Request cancellation token.</param>
		/// <returns>
		/// 201 Created when booking succeeds.  
		/// 400 BadRequest when validation fails or booking rules are violated.
		/// </returns>
		/// <response code="201">Booking successfully created.</response>
		/// <response code="400">Invalid request or booking not allowed.</response>
		[HttpPost("{eventId:guid}/book")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<Guid>> Register(
			[FromRoute] Guid eventId,
			[FromBody] BookEventRequest request,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation("Received booking request for event ID: {EventId} with participant email: {Email}", eventId, request.Email);
			if (eventId == Guid.Empty)
			{
				_logger.LogWarning("Booking attempt with empty event ID.");
				return BadRequest(new { error = "Event ID is required." });
			}

			if (request.Email == null)
			{
				_logger.LogWarning("Booking attempt with missing participant email.");
				return BadRequest(new { error = "At least one participant is required." });
			}

			var newParticipant = new CreateBookingsDto{
				Name = request.Name,
				Email = request.Email,
				PhoneNumber = request.PhoneNumber
			};
			var bookEvent = new BookEventCommand {
					EventId = eventId,
					Participant = newParticipant
			};

			var result = await _mediator.Send(bookEvent, cancellationToken);

			if (!result.IsSuccess)
			{
				_logger.LogWarning("Booking failed for event ID: {EventId}. Reason: {Reason}", eventId, result.ErrorMessage);
				return BadRequest(new
				{
					ErrorMessage = result.ErrorMessage
				});
				
			}

			_logger.LogInformation("Booking successful for event ID: {EventId}.", eventId);
			return CreatedAtAction(
					"GetRegistrations",
					"Events",
					new { eventId },
					result.Data
				);
		}
	}
}
