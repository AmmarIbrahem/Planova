using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Planova.API.Pages.EventManagment;

public class EventBookingsModel : PageModel
{
	private readonly ApiClient _apiClient;

	public List<RegistrationItemUi> Registrations { get; set; } = [];
	public string? Error { get; set; }
	public Guid? EventId { get; set; }

	public EventBookingsModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IActionResult> OnGetAsync(Guid? eventId)
	{
		if (!User.Identity.IsAuthenticated)
		{
			await HttpContext.SignOutAsync();
			return RedirectToPage("/User/Login");
		}

		var jwt = User.FindFirst("jwt")?.Value;
		
		if (string.IsNullOrEmpty(jwt))
			return RedirectToPage("/User/Login");
		if (eventId == null)
		{
			Error = "Event ID is required.";
			return Page();
		}
		EventId = eventId;
		var items = await _apiClient.GetJsonAsync<List<RegistrationItemUi>>($"/api/events/{eventId}/registrations", jwt);
		if (items == null)
		{
			Error = "Failed to load registrations.";
			return Page();
		}
		Registrations = items;
		return Page();
	}
}
