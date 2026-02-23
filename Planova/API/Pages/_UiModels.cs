namespace Planova.API.Pages;

public sealed record EventListItemUi(Guid Id, string Name, string? Description, string Location, DateTime StartTime, DateTime EndTime, int Capacity, int TotalParticipants, int AvailableSlots);
public sealed record EventDetailsUi(Guid Id, string Name, string? Description, string Location, DateTime StartTime, DateTime EndTime, int Capacity, int Registered);
public sealed record RegistrationItemUi(Guid Id, string Name, string PhoneNumber, string Email);
public sealed record LoginResultUi(bool IsSuccess, string? ErrorMessage, object? Data);
public sealed record BookRequestUi(Guid EventId, string Name, string PhoneNumber, string Email);
public sealed record BookEventResponseUi(string CancellationToken);
