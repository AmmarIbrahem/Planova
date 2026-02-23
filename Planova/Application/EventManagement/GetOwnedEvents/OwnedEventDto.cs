namespace Planova.Application.EventManagement.GetOwnedEvents
{
	public class OwnedEventDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = default!;
		public string Description { get; set; } = default!;
		public string Location { get; set; } = default!;
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public int Capacity { get; set; }
		public int TotalParticipants { get; set; }
		public List<BookingDto> Bookings { get; set; } = new();
	}
}
