using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Planova.API.Pages.EventManagment.Events;

public class EditModel : PageModel
{
	private readonly ApiClient _apiClient;

	public EventDetailsUi? Event { get; set; }
	public string? Error { get; set; }

	[BindProperty]
	public string Name { get; set; } = "";

	[BindProperty]
	public string Description { get; set; } = "";

	[BindProperty]
	public string Location { get; set; } = "";

	[BindProperty]
	public DateTime StartTime { get; set; }

	[BindProperty]
	public DateTime EndTime { get; set; }

	[BindProperty]
	public int Capacity { get; set; }

	public EditModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}
	[Authorize(Policy = "EventManagerOnly")]

	public async Task<IActionResult> OnGetAsync(Guid? id)
	{
		if (!User.Identity.IsAuthenticated)
		{
			await HttpContext.SignOutAsync();
			return RedirectToPage("/User/Login");
		}

		var jwt = User.FindFirst("jwt")?.Value;
		if (string.IsNullOrEmpty(jwt))
			return RedirectToPage("/EventManagment/Login");
		if (id == null)
			return RedirectToPage("/EventManagment/PublicDiscovery/Index");

		Event = await _apiClient.GetJsonAsync<EventDetailsUi>($"/api/events/{id}");
		if (Event == null)
			return RedirectToPage("/EventManagment/PublicDiscovery/Index");

		Name = Event.Name;
		Description = Event.Description ?? "";
		Location = Event.Location;
		StartTime = Event.StartTime;
		EndTime = Event.EndTime;
		Capacity = Event.Capacity;
		return Page();
	}

	public async Task<IActionResult> OnPostAsync(Guid? id)
	{
		if (!User.Identity.IsAuthenticated)
			return RedirectToPage("/User/Login");
		if (id == null)
			return RedirectToPage("/EventManagment/PublicDiscovery/Index");

		var body = new
		{
			Name,
			Description,
			Location,
			StartTime,
			EndTime,
			Capacity,
			CreatorId = Guid.Empty
		};
		var jwt = User.FindFirst("jwt")?.Value;
		var response = await _apiClient.PutJsonAsync($"/api/events/{id}", body, jwt);
		var json = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
			return RedirectToPage("/EventManagment/Events/Index");

		var err = JsonSerializer.Deserialize<JsonElement>(json);

		Error = err.TryGetProperty("errorMessage", out var e)
			? e.GetString() ?? "Failed to edit event."
			: "Failed to edit event.";
		Event = await _apiClient.GetJsonAsync<EventDetailsUi>($"/api/events/{id}");
		return Page();
	}
}
