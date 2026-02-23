namespace Planova.Application.EventManagement.GetEventBookings;

public sealed record BookingsListDto{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string PhoneNumber { get; init; }
    public string Email { get; init; }
}
