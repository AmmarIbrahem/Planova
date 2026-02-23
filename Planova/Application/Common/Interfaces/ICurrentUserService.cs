using Planova.Domain.Enums;

namespace Planova.Application.Common.Interfaces
{
	public interface ICurrentUserService
	{
		public Guid? UserId { get; }
		public string? Email { get; }
		public UserRole? Role { get; }

	}
}
