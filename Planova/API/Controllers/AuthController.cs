using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Planova.API.DTOs.Auth;
using Planova.Application.Auth.Login;
using Planova.Application.Auth.Register;
using Planova.Application.Common;

namespace Planova.API.Controllers
{
	/// <summary>
	/// Provides authentication endpoints including user registration and login.
	/// </summary>
	/// <remarks>
	/// This controller acts as an API boundary layer and delegates business logic
	/// to the Application layer via MediatR.
	/// </remarks>
	[ApiController]
	[Route("api/auth")]
	[Produces("application/json")]
	public sealed class AuthController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly ILogger<AuthController> _logger;

		public AuthController(IMediator mediator, ILogger<AuthController> logger)
		{
			_mediator = mediator;
			_logger = logger;
		}

		/// <summary>
		/// Registers a new user in the system.
		/// </summary>
		/// <param name="request">Registration details including name, email, password, phone number, and role.</param>
		/// <returns>
		/// Returns 201 Created when registration succeeds.
		/// Returns 400 BadRequest if registration fails.
		/// </returns>
		/// <response code="201">User successfully registered.</response>
		/// <response code="400">Invalid registration data.</response>
		[HttpPost("register")]
		[EnableRateLimiting("auth")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<Result>> Register([FromBody] RegisterRequest request)
		{
			var result = await _mediator.Send(
				new RegisterCommand(
					request.Name,
					request.Email,
					request.PhoneNumber,
					request.Password,
					request.Role)
			);

			if (!result.IsSuccess)
			{
				_logger.LogWarning("Registration failed for email: {Email}. Reason: {Reason}", request.Email, result.ErrorMessage);
				return BadRequest(result);
			}

			_logger.LogInformation("User registered successfully with email: {Email}", request.Email);
			return Ok(result);
		}

		/// <summary>
		/// Authenticates a user and returns a JWT access token.
		/// </summary>
		/// <param name="request">User login credentials.</param>
		/// <returns>
		/// Returns 200 OK with authentication token if credentials are valid.
		/// Returns 401 Unauthorized if credentials are invalid.
		/// </returns>
		/// <response code="200">Login successful. Returns JWT token and user info.</response>
		/// <response code="401">Invalid email or password.</response>
		[HttpPost("login")]
		[EnableRateLimiting("auth")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult<Result>> Login(LoginRequest request)
		{
			var result = await _mediator.Send(
				new LoginCommand(request.Email, request.Password));

			if (!result.IsSuccess)
			{
				_logger.LogWarning("Login failed for email: {Email}. Reason: {Reason}", request.Email, result.ErrorMessage);
				return Unauthorized(result);
			}
			_logger.LogInformation("User logged in successfully with email: {Email}", request.Email);
			return Ok(result);
		}
	}
}
