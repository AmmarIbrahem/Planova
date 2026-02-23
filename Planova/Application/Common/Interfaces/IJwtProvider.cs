namespace Planova.Application.Common.Interfaces
{
	public interface IJwtProvider
	{
		string Generate(Guid userId, string email, string role);
	}
}
