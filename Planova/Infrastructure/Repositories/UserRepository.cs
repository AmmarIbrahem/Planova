using Microsoft.EntityFrameworkCore;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Entities;

namespace Planova.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;
	private readonly ILogger<UserRepository> _logger;

	public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
		_logger.LogDebug("User retrieval result for ID {UserId}: {Found}", id, user != null);
		return user;
	}

	public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Email == email.ToLower(), cancellationToken);
		_logger.LogDebug("User retrieval result for email {Email}: {Found}", email, user != null);
		return user;
	}

	public async Task<Guid> AddAsync(User entity, CancellationToken cancellationToken)
	{
		await _context.Users.AddAsync(entity, cancellationToken);
		_logger.LogDebug("New user added with email: {Email} and assigned ID: {UserId}", entity.Email, entity.Id);
		return entity.Id;
	}
}
