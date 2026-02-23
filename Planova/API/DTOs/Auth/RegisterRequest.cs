namespace Planova.API.DTOs.Auth
{
	public sealed record RegisterRequest(
		string Name,
		string Email,
		string PhoneNumber,
		string Password,
		string Role
	);
}
