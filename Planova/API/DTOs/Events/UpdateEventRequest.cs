namespace Planova.API.DTOs.Events
{
	public sealed record UpdateEventRequest(
		string Name,
		string Description,
		string Location,
		DateTime StartTime,
		DateTime EndTime,
		int Capacity,
		Guid CreatorId
	);
}
