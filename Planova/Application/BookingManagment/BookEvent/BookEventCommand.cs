using MediatR;
using Planova.Application.Common;

namespace Planova.Application.BookingManagment.BookEvent
{
	public class BookEventCommand : IRequest<Result>
	{
		public Guid EventId { get; set; }
		public CreateBookingsDto Participant { get; set; } = new();
	}

	public class CreateBookingsDto
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public CreateBookingsDto() { }
	}
}
