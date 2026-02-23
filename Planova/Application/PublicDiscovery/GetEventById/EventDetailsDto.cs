namespace Planova.Application.PublicDiscovery.GetEventById;

public sealed class EventDetailsDto
{
	public Guid Id { get; init; }
	public string Name { get; init; } = default!;
	public string Description { get; init; } = default!;
	public string Location { get; init; } = default!;
	public DateTime StartTime { get; init; }
	public DateTime EndTime { get; init; }
	public int Capacity { get; init; }
	public int CurrentParticipants { get; set; }
	public int AvailableSlots => Capacity - CurrentParticipants;

	public CreatorDto Creator { get; init; } = default!;
}

public sealed class CreatorDto
{
	public Guid Id { get; init; }
	public string Email { get; init; } = default!;
}
