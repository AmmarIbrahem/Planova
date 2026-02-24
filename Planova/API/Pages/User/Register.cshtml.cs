using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planova.API.Pages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Planova.API.Pages.User;

public class RegisterModel : PageModel
{
	private readonly ApiClient _apiClient;

	public string? Error { get; set; }

	[BindProperty]
	public string Name { get; set; } = "";

	[BindProperty]
	public string Email { get; set; } = "";

	[BindProperty]
	public string PhoneNumber { get; set; } = "";

	[BindProperty]
	public string Password { get; set; } = "";

	[BindProperty]
	public string Role { get; set; }

	public RegisterModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IActionResult> OnPostAsync()
	{
		var body = new { Name, Email, PhoneNumber, Password, Role };
		var response = await _apiClient.PostJsonAsync("/api/auth/register", body);
		var json = await response.Content.ReadAsStringAsync();
		if (response.IsSuccessStatusCode)
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			if (root.TryGetProperty("isSuccess", out var success) && success.GetBoolean())
				return RedirectToPage("/User/Login");
		}
		else
		{
			var err = JsonSerializer.Deserialize<JsonElement>(json);
			Error = err.TryGetProperty("errorMessage", out var e)
				? e.GetString() ?? "Registration failed." 
				: "Registration failed.";
		}
		return Page();
	}
}
