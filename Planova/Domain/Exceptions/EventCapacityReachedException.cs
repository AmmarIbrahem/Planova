namespace Planova.Domain.Exceptions
{
	public sealed class EventCapacityReachedException : DomainException
	{
		public EventCapacityReachedException(DomainException ex) : base("Event capacity reached.", ex)
		{
		}
	}
}
