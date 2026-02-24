using Planova.Domain.Common;
using Planova.Domain.Entities;

public class Event : Entity
{

	public string Name { get; private set; } = default!;
	public string Description { get; private set; } = default!;
	public string Location { get; private set; } = default!;

	public DateTime StartTime { get; private set; }
	public DateTime EndTime { get; private set; }
	public int Capacity { get; private set; }
	public Guid CreatorId { get; private set; }

	private readonly List<Booking> _bookings = new();
	public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

	private Event() { } // EF Core

	public Event(
		string name,
		string description,
		string location,
		DateTime startTime,
		DateTime endTime,
		int capacity,
		Guid creatorId)
	{
		Name = name;
		Description = description;
		Location = location;
		StartTime = startTime;
		EndTime = endTime;
		Capacity = capacity;
		CreatorId = creatorId;
	}


	public void Update(
		string name,
		string description,
		string location,
		DateTime startTime,
		DateTime endTime,
		int capacity)
	{
		Name = name;
		Description = description;
		Location = location;
		StartTime = startTime;
		EndTime = endTime;
		Capacity = capacity;
	}
}
