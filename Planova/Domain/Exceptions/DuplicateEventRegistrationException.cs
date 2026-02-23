namespace Planova.Domain.Exceptions
{
	public sealed class DuplicateEventRegistrationException : DomainException
	{
		public DuplicateEventRegistrationException(DomainException ex) : base("Email already registered for this event.", ex)
		{
		}
	}
}
