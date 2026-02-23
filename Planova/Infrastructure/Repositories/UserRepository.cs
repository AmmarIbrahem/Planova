using Microsoft.EntityFrameworkCore;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Entities;

namespace Planova.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UserRepository> _logger;

	public UserRepository(AppDbContext context, IUnitOfWork unitOfWork, ILogger<UserRepository> logger)
	{
		_context = context;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
	{
		return await _context.Users
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
	}

	public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Retrieving user with email: {Email}", email);
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Email == email.ToLower(), cancellationToken);
		_logger.LogInformation("User retrieval result for email {Email}: {Found}", email, user != null);
		return user;
	}

	public async Task<Guid> AddAsync(User entity, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding new user with email: {Email}", entity.Email);
		await _context.Users.AddAsync(entity, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		_logger.LogInformation("New user added with email: {Email} and assigned ID: {UserId}", entity.Email, entity.Id);
		return entity.Id;
	}
}
