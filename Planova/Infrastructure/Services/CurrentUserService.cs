using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using System.Data;
using System.Security.Claims;

namespace Planova.Infrastructure.Services
{
	using System.Security.Claims;

	public class CurrentUserService : ICurrentUserService
	{
		private readonly IHttpContextAccessor _httpContext;

		public CurrentUserService(IHttpContextAccessor httpContext)
		{
			_httpContext = httpContext;
		}

		private ClaimsPrincipal User => _httpContext.HttpContext?.User!;

		public Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
		public string? Email => User.FindFirstValue(ClaimTypes.Email);
		public UserRole? Role => Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;
		public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
	}
}

