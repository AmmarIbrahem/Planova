namespace Planova.Domain.Entities
{
	public class PasswordResetToken
	{
		public Guid Id { get; private set; }
		public Guid UserId { get; private set; }
		public string Token { get; private set; }
		public DateTime Expiry { get; private set; }
	}
}
