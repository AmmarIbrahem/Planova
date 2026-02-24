using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Domain.Enums;
using Planova.Infrastructure.Exceptions;
using System.Text.RegularExpressions;


namespace Planova.Application.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ILogger<RegisterCommandHandler> _logger;

	public RegisterCommandHandler(
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		IPasswordHasher passwordHasher,
		ILogger<RegisterCommandHandler> logger)
	{
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_passwordHasher = passwordHasher;
		_logger = logger;
	}

	public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Email) ||
			!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
		{
			_logger.LogWarning("Invalid email format for email: {Email}", request.Email);
			return Result.Failure("Invalid email format.");
		}

		if (string.IsNullOrWhiteSpace(request.Password) ||
			request.Password.Length < 6)
		{
			_logger.LogWarning("Password too short for email: {Email}", request.Email);
			return Result.Failure("Password must be at least 6 characters long.");
		}
		try
		{
			var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
			if (existingUser != null)
			{
				_logger.LogWarning("Email already registered: {Email}", request.Email);	
				return Result.Failure("Email already registered.");
			}

			if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
			{
				_logger.LogWarning("Invalid role provided: {Role} for email: {Email}", request.Role, request.Email);
				return Result.Failure("Invalid role.");
			}

			var passwordHash = _passwordHasher.Hash(request.Password);
			var user = new User(
				request.Email,
				passwordHash,
				role
			);

			await _userRepository.AddAsync(user, cancellationToken);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("User with email: {Email} registered successfully", request.Email);
			return Result.Success("User registered successfully.");
		}
		catch (Exception ex) 
		{ 
			throw new InfrastructureException("An error occurred while registering the user.", ex);
		}
	}
}
