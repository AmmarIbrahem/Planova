using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Planova.API.Pages.PublicDiscovery;

public class DetailsModel : PageModel
{
	private readonly ApiClient _apiClient;

	public EventDetailsUi? Event { get; set; }

	[BindProperty]
	[Required(ErrorMessage = "Full name is required.")]
	[StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
	public string BookingName { get; set; } = string.Empty;

	[BindProperty]
	[Required(ErrorMessage = "Email is required.")]
	[EmailAddress(ErrorMessage = "Invalid email address.")]
	[StringLength(200)]
	public string BookingEmail { get; set; } = string.Empty;

	[BindProperty]
	[Required(ErrorMessage = "Phone number is required.")]
	[Phone(ErrorMessage = "Invalid phone number.")]
	[StringLength(50)]
	public string BookingPhoneNumber { get; set; } = string.Empty;

	// Display notification to the user
	public string? BookingMessage { get; set; }
	public bool BookingSuccess { get; set; }

	public DetailsModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IActionResult> OnGetAsync(Guid id)
	{
		var response = await _apiClient.GetAsync($"/api/events/{id}");

		if (!response.IsSuccessStatusCode)
			return RedirectToPage("Index");

		var json = await response.Content.ReadAsStringAsync();

		Event = JsonSerializer.Deserialize<EventDetailsUi>(
			json,
			JsonOptions
		);

		if (Event == null)
			return RedirectToPage("Index");

		return Page();
	}

	public async Task<IActionResult> OnPostAsync(Guid id)
	{
		if (!ModelState.IsValid)
		{
			await ReloadEvent(id);
			return Page();
		}

		var request = new
		{
			EventId = id,
			Name = BookingName,
			Email = BookingEmail,
			PhoneNumber = BookingPhoneNumber
		};

		var response = await _apiClient.PostJsonAsync($"/api/bookings/{id}/book", request);

		if (response.IsSuccessStatusCode)
		{
			BookingMessage = "Registration successful! Your booking is confirmed.";
			BookingSuccess = true;
		}
		else
		{
			try
			{
				var json = await response.Content.ReadAsStringAsync();
				var error = JsonSerializer.Deserialize<JsonElement>(json);

				if (error.TryGetProperty("errorMessage", out var msg))
					BookingMessage = msg.GetString();
				else
					BookingMessage = "Booking failed.";
			}
			catch
			{
				BookingMessage = "Booking failed due to an unexpected error.";
			}

			BookingSuccess = false;
		}

		await ReloadEvent(id);
		return Page();
	}

	private async Task ReloadEvent(Guid id)
	{
		var response = await _apiClient.GetAsync($"/api/events/{id}");

		if (!response.IsSuccessStatusCode)
			return;

		var json = await response.Content.ReadAsStringAsync();

		Event = JsonSerializer.Deserialize<EventDetailsUi>(
			json,
			JsonOptions
		);
	}

	private static readonly JsonSerializerOptions JsonOptions =
		new() { PropertyNameCaseInsensitive = true };
}

public class EventDetailsUi
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public string Location { get; set; } = string.Empty;
	public int Capacity { get; set; }
	public int AvailableSlots { get; set; }
}

public class BookEventResponseUi
{
	public string CancellationToken { get; set; } = string.Empty;
}