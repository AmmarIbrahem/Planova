using Planova.Domain.Common;

namespace Planova.Domain.Entities;

public class Booking : Entity
{
	public Guid? BookerUserId{ get; private set; }
	public Guid EventId { get; private set; }
	public string Name { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string PhoneNumber { get; set; } = default!;

	private Booking() { } // EF

	public Booking(Guid eventId,
		string name,
		string email,
		string phoneNumber,
		Guid? bookerUserId = null)
	{
		EventId = eventId;
		Name = name;
		Email = email;
		PhoneNumber = phoneNumber;
		BookerUserId = bookerUserId;
	}
}
