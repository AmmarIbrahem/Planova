using MediatR;
using Planova.Application.Common;
using Planova.Application.Common.Interfaces;
using Planova.Infrastructure.Exceptions;

namespace Planova.Application.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result>
{
	private readonly IUserRepository _userRepository;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IJwtProvider _jwtProvider;
	private readonly ILogger<LoginHandler> _logger;

	public LoginHandler(
		IUserRepository userRepository,
		IPasswordHasher passwordHasher,
		IJwtProvider jwtProvider,
		ILogger<LoginHandler> logger)
	{
		_userRepository = userRepository;
		_passwordHasher = passwordHasher;
		_jwtProvider = jwtProvider;
		_logger = logger;
	}

	public async Task<Result> Handle(LoginCommand command, CancellationToken cancellationToken)
	{
		try
		{
			var user = await _userRepository.GetByEmailAsync(command.Email.ToLowerInvariant(), cancellationToken);

			if (user is null || string.IsNullOrEmpty(user.PasswordHash))
			{
				_logger.LogWarning("Login failed for email: {Email} - user not found or password hash missing", command.Email);
				return Result.Failure("Invalid credentials.");
			}

			var isValid = _passwordHasher.Verify(command.Password, user.PasswordHash);

			if (!isValid)
				return Result.Failure("Invalid credentials.");

			var token = _jwtProvider.Generate(
				user.Id,
				user.Email,
				user.Role.ToString());
			_logger.LogInformation("User with email: {Email} logged in successfully", command.Email);
			return Result.Success(token);
		}
		catch (Exception ex)
		{
			throw new InfrastructureException("An error occurred while processing the login request.", ex);
		}
	}

}
