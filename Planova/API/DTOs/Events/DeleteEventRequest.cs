namespace Planova.API.DTOs.Events
{
	public sealed record DeleteEventRequest(
		Guid EventId,
		Guid CreatorId
	);
}