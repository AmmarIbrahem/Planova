using MediatR;
using Planova.Application.Common;

namespace Planova.Application.Auth.Login
{
	public sealed record LoginCommand(
		string Email,
		string Password) : IRequest<Result>;
}
