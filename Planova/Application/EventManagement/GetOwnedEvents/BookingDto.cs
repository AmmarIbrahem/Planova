namespace Planova.Application.EventManagement.GetOwnedEvents
{
	public class BookingDto
	{
		public Guid BookingId { get; set; }
		public Guid? BookerUserId { get; set; }
		public BookingParticipantDto Participant { get; set; } = new();
	}
}
