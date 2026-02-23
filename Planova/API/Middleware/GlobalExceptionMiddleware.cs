using Planova.API.Exceptions;
using Planova.Domain.Exceptions;
using Planova.Infrastructure.Exceptions;

namespace Planova.API.Middleware
{
	public sealed class GlobalExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<GlobalExceptionMiddleware> _logger;

		public GlobalExceptionMiddleware(
			RequestDelegate next,
			ILogger<GlobalExceptionMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch(ApiException ex)
			{
				_logger.LogError(ex, "ApiException");
				context.Response.StatusCode = StatusCodes.Status409Conflict;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch(ApplicationException ex)
			{
				_logger.LogError(ex, "ApplicationException");
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch(InfrastructureException ex)
			{
				_logger.LogError(ex, "InfrastructureException");
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (EventCapacityReachedException ex)
			{
				_logger.LogError(ex, "EventCapacityReachedException");
				context.Response.StatusCode = StatusCodes.Status409Conflict;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (DuplicateEventRegistrationException ex)
			{
				_logger.LogError(ex, "DuplicateEventRegistrationException");
				context.Response.StatusCode = StatusCodes.Status409Conflict;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (DomainException ex)
			{
				_logger.LogError(ex, "DomainException");
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogError(ex, "KeyNotFoundException");
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (ArgumentException ex)
			{
				_logger.LogError(ex, "ArgumentException");
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unhandled exception");
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
			}
		}
	}

}
