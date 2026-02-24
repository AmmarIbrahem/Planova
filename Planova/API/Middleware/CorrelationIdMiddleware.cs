namespace Planova.API.Middleware
{
	public class CorrelationIdMiddleware
	{

		private readonly RequestDelegate _next;
		private readonly ILogger<CorrelationIdMiddleware> _logger;
		public CorrelationIdMiddleware(
			RequestDelegate next,
			ILogger<CorrelationIdMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
								?? Guid.NewGuid().ToString("N");

			context.Response.Headers["X-Correlation-ID"] = correlationId;

			using (_logger.BeginScope(new Dictionary<string, object>
			{
				["CorrelationId"] = correlationId,
				["RequestPath"] = context.Request.Path.Value ?? "",
			}))
			{
				await _next(context);
			}
		}
	}
}
