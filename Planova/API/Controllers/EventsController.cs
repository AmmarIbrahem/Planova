using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Planova.API.DTOs.Events;
using Planova.Application.Common.Interfaces;
using Planova.Application.EventManagement.CreateEvent;
using Planova.Application.EventManagement.DeleteEvent;
using Planova.Application.EventManagement.GetEventBookings;
using Planova.Application.EventManagement.GetOwnedEvents;
using Planova.Application.EventManagement.UpdateEvent;
using Planova.Application.PublicDiscovery.GetAllEvents;
using Planova.Application.PublicDiscovery.GetEventById;

namespace Planova.API.Controllers
{
	/// <summary>
	/// Handles event management endpoints, including public discovery, creation, update, and deletion of events.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public sealed class EventsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<EventsController> _logger;

		public EventsController(IMediator mediator, ICurrentUserService currentUser, ILogger<EventsController> logger)
		{
			_mediator = mediator;
			_currentUser = currentUser;
			_logger = logger;
		}

		/// <summary>
		/// Retrieves a list of all public events.
		/// </summary>
		/// <response code="200">Returns a list of events.</response>
		/// <response code="400">If the request fails.</response>
		[HttpGet]
		[OutputCache(PolicyName = "ReadCache")]
		[ProducesResponseType(typeof(List<EventListItemDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<List<EventListItemDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllEventsQuery(), cancellationToken);
			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to retrieve events. Reason: {Reason}", result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully retrieved events");
			return Ok(result.Data);
		}

		/// <summary>
		/// Retrieves a specific event by its ID.
		/// </summary>
		/// <param name="id">The event identifier.</param>
		/// <response code="200">Returns event details.</response>
		/// <response code="400">If the event id is not valid.</response>
		[HttpGet("{id:guid}")]
		[OutputCache(PolicyName = "ReadCache")]
		[ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<EventDetailsDto>> GetById(Guid id, CancellationToken ct)
		{
			var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to retrieve event with ID: {EventId}. Reason: {Reason}", id, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully retrieved event with ID: {EventId}", id);
			return Ok(result.Data);
		}

		/// <summary>
		/// Retrieves all events owned by the authenticated user.
		/// </summary>
		/// <response code="200">Returns a list of owned events.</response>
		/// <response code="401">If the user is not authenticated.</response>
		/// <response code="400">If the request fails due to business validation.</response>
		[HttpGet("ownedEvents")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[OutputCache(PolicyName = "ReadCache")]
		[ProducesResponseType(typeof(List<EventDetailsDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<EventDetailsDto>> GetOwnedEvents(CancellationToken ct)
		{
			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Unauthorized attempt to access owned events");
				return Unauthorized();
			}

			var result = await _mediator.Send(new GetOwnedEventsQuery(), ct);
			if(!result.IsSuccess)
			{
				_logger.LogWarning("Failed to retrieve owned events for user ID: {UserId}. Reason: {Reason}", _currentUser.UserId, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully retrieved owned events for user ID: {UserId}", _currentUser.UserId);
			return Ok(result.Data);
		}

		/// <summary>
		/// Retrieves all bookings/registrations for a specific event.
		/// </summary>
		/// <param name="eventId">The event identifier.</param>
		/// <response code="200">Returns a list of registrations.</response>
		/// <response code="401">If the user is not authenticated.</response>
		/// <response code="400">If the request fails due to business validation.</response>
		[HttpGet("{eventId:guid}/registrations")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[OutputCache(PolicyName = "ReadCache")]
		[ProducesResponseType(typeof(List<BookingsListDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<List<BookingsListDto>>> GetRegistrations(
			Guid eventId,
			CancellationToken cancellationToken)
		{
			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Unauthorized attempt to access registrations for event ID: {EventId}", eventId);
				return Unauthorized();
			}

			var result = await _mediator.Send(new GetEventBookingsQuery(eventId), cancellationToken);
			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to retrieve registrations for event ID: {EventId}. Reason: {Reason}", eventId, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully retrieved registrations for event ID: {EventId}", eventId);
			return Ok(result.Data);
		}

		/// <summary>
		/// Creates a new event.
		/// </summary>
		/// <param name="request">Event creation details.</param>
		/// <response code="201">Event created successfully.</response>
		/// <response code="400">Invalid request or business validation failed.</response>
		/// <response code="401">If the user is not authenticated.</response>
		[HttpPost]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[EnableRateLimiting("fixed")]
		[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<Guid>> Create(
			[FromBody] CreateEventRequest request,
			CancellationToken cancellationToken)
		{
			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Unauthorized attempt to create an event");
				return Unauthorized();
			}
			var command = new CreateEventCommand(
				request.Name,
				request.Description,
				request.Location,
				request.StartTime,
				request.EndTime,
				request.Capacity,
				_currentUser.UserId.Value);

			var result = await _mediator.Send(command, cancellationToken);
			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to create event for user ID: {UserId}. Reason: {Reason}", _currentUser.UserId, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			
			_logger.LogInformation("Successfully created event with ID: {EventId} for user ID: {UserId}", result.Data, _currentUser.UserId);
			return Ok(result.Data);
		}

		/// <summary>
		/// Updates an existing event.
		/// </summary>
		/// <param name="id">The event identifier.</param>
		/// <param name="request">Updated event data.</param>
		/// <response code="200">Event updated successfully.</response>
		/// <response code="400">If update fails due to invalid data or business rules.</response>
		/// <response code="401">If the user is not authenticated.</response>
		[HttpPut("{id}")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[EnableRateLimiting("fixed")]
		[ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> Update(
			Guid id,
			UpdateEventRequest request,
			CancellationToken cancellationToken)
		{
			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Unauthorized attempt to update event with ID: {EventId}", id);
				return Unauthorized();	
			}

			var command = new UpdateEventCommand(
				id,
				request.Name,
				request.Description,
				request.Location,
				request.StartTime,
				request.EndTime,
				request.Capacity);

			var result = await _mediator.Send(command, cancellationToken);
			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to update event with ID: {EventId}. Reason: {Reason}", id, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully updated event with ID: {EventId}", id);
			return Ok(result.Data);
		}

		/// <summary>
		/// Deletes an existing event.
		/// </summary>
		/// <param name="id">The event identifier.</param>
		/// <response code="204">Event deleted successfully.</response>
		/// <response code="400">If deletion fails due to business validation.</response>
		/// <response code="401">If the user is not authenticated.</response>
		[HttpDelete("{id}")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[EnableRateLimiting("fixed")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> Delete(
			Guid id,
			CancellationToken cancellationToken)
		{
			if (_currentUser.UserId == null)
			{
				_logger.LogWarning("Unauthorized attempt to delete event with ID: {EventId}", id);
				return Unauthorized();
			}

			var result =  await _mediator.Send(new DeleteEventCommand(id), cancellationToken);

			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to delete event with ID: {EventId}. Reason: {Reason}", id, result.ErrorMessage);
				return BadRequest(new { ErrorMessage = result.ErrorMessage });
			}
			_logger.LogInformation("Successfully deleted event with ID: {EventId}", id);
			return NoContent();
		}
	}


}
