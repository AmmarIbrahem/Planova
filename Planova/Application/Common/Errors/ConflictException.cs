namespace Planova.Application.Common.Errors;

public sealed class ConflictException : Exception
{
	public ConflictException(string message, Exception ex) : base(message, ex)
	{
	}
}
