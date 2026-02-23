using Planova.Domain.Common;
using Planova.Domain.Enums;

public sealed class User: Entity
{
	public string Email { get; private set; } = default!;
	public string PasswordHash { get; private set; }

	public UserRole Role { get; private set; }

	private User() { } // EF

	public User(string email, string passwordHash, UserRole role)
	{
		Email = email.ToLowerInvariant();
		PasswordHash = passwordHash;	
		Role = role;
	}

	public void ChangeRole(UserRole role)
		=> Role = role;

}
