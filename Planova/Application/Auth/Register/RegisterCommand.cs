using MediatR;
using Planova.Application.Common;

namespace Planova.Application.Auth.Register
{
	public sealed record RegisterCommand(
	string Name,
	string Email,
	string PhoneNumber,
	string Password,
	string Role) : IRequest<Result>;
}
