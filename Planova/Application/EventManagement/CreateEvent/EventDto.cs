
namespace Planova.Application.EventManagement.CreateEvent
{
	internal class EventDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Location { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public int Capacity { get; set; }
	}
}