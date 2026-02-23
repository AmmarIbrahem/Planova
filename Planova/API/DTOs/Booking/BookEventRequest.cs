using Planova.Application.BookingManagment.BookEvent;

namespace Planova.API.DTOs.Booking
{
	public sealed record BookEventRequest(
		 string Email,
		 string Name,
		 string PhoneNumber
		);
}
