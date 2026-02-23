namespace Planova.Application.Common.Interfaces
{
	public interface IUserRepository
	{
		Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
		Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
		Task<Guid> AddAsync(User entity, CancellationToken cancellationToken);
	}
}
