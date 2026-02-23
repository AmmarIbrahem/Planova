namespace Planova.Infrastructure.Exceptions
{
	public class InfrastructureException : Exception
	{
		public InfrastructureException(string message, Exception ex) : base(message, ex)
		{
		}
	}
}
