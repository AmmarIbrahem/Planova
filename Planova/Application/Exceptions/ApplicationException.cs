namespace Planova.Application.Exceptions
{
	public class ApplicationException : Exception
	{
		public ApplicationException(string message, Exception ex) : base(message, ex)
		{
		}
	}
}
