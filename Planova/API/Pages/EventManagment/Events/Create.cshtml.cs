using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Planova.API.Pages.EventManagment.Events;

public class CreateModel : PageModel
{
	private readonly ApiClient _apiClient;

	public string? Error { get; set; }

	[BindProperty]
	public string Name { get; set; } = "";

	[BindProperty]
	public string Description { get; set; } = "";

	[BindProperty]
	public string Location { get; set; } = "";

	[BindProperty]
	public DateTime StartTime { get; set; } = DateTime.UtcNow.Date.AddHours(9);

	[BindProperty]
	public DateTime EndTime { get; set; } = DateTime.UtcNow.Date.AddHours(17);

	[BindProperty]
	public int Capacity { get; set; } = 10;

	public CreateModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public IActionResult OnGet()
	{
		if (!User.Identity.IsAuthenticated)
			return RedirectToPage("/User/Login");
		
		return Page();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (!User.Identity.IsAuthenticated)
			return RedirectToPage("/User/Login");

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
		var response = await _apiClient.PostJsonAsync("/api/events", body, jwt);
		var json = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
			return RedirectToPage("/EventManagment/Events/Index");

		var err = JsonSerializer.Deserialize<JsonElement>(json);

		Error = err.TryGetProperty("errorMessage", out var e)
			? e.GetString() ?? "Failed to create event."
			: "Failed to create event.";
		return Page();
	}

}
