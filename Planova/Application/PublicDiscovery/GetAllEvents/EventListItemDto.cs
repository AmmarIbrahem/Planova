namespace Planova.Application.PublicDiscovery.GetAllEvents;

public sealed record EventListItemDto(
	Guid Id,
	string Name,
	string Location,
	DateTime StartTime,
	DateTime EndTime,
	int Capacity);
