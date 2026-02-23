using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Planova.API.Pages.EventManagment.Events;

public class IndexModel : PageModel
{
	private readonly ApiClient _apiClient;

	public List<EventListItemUi> Events { get; set; } = [];
	public string? Error { get; set; }

	public IndexModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	[Authorize(Roles = "Admin, EventCreator")]
	public async Task<IActionResult> OnGetAsync()
	{
		if (!User.Identity.IsAuthenticated)
		{
			await HttpContext.SignOutAsync();
			return RedirectToPage("/User/Login");
		}

		var jwt = User.FindFirst("jwt")?.Value;
		var items = await _apiClient.GetJsonAsync<List<EventListItemUi>>("/api/events/ownedEvents", jwt);
		if (items == null)
		{
			Error = "Failed to load events.";
			return Page();
		}
		Events = items;

		return Page();
	}

	[Authorize(Roles = "Admin, EventCreator")]
	public async Task<IActionResult> OnPostDeleteAsync(Guid? id)
	{

		var jwt = User.FindFirst("jwt")?.Value;
		if (string.IsNullOrEmpty(jwt))
			return RedirectToPage("/User/Login");
		if (id == null)
			return RedirectToPage();

		var response = await _apiClient.DeleteAsync($"/api/events/{id}", jwt);
		if (!response.IsSuccessStatusCode)
		{
			var json = await response.Content.ReadAsStringAsync();
			Error = TryParseError(json) ?? "Failed to delete event.";
		}
		return await OnGetAsync();
	}

	private static string? TryParseError(string json)
	{
		try
		{
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			var root = doc.RootElement;
			if (root.TryGetProperty("error", out var e))
				return e.GetString();
		}
		catch { }
		return null;
	}
}
