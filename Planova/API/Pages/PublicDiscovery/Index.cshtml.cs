using Microsoft.AspNetCore.Mvc.RazorPages;
using Planova.API.Pages;

namespace Planova.API.Pages.PublicDiscovery;

public class IndexModel : PageModel
{
	private readonly ApiClient _apiClient;

	public List<EventListItemUi> Events { get; set; } = [];
	public string? Error { get; set; }

	public IndexModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task OnGetAsync()
	{
		var items = await _apiClient.GetJsonAsync<List<EventListItemUi>>("/api/events");
		if (items == null)
		{
			Error = "Failed to load events.";
			return;
		}
		Events = items;
	}
}
