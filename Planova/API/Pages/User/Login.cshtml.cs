using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planova.API.Pages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Planova.API.Pages.User;

public class LoginModel : PageModel
{
	private readonly ApiClient _apiClient;

	public string? Error { get; set; }

	[BindProperty]
	public string Email { get; set; } = "";

	[BindProperty]
	public string Password { get; set; } = "";

	public LoginModel(ApiClient apiClient)
	{
		_apiClient = apiClient;
	}
	public IActionResult OnGet()
	{
		if (User?.Identity?.IsAuthenticated ?? false)
			return RedirectToPage("/PublicDiscovery/Index");
		return Page();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		var body = new { Email, Password };
		var response = await _apiClient.PostJsonAsync("/api/auth/login", body);
		var json = await response.Content.ReadAsStringAsync();
		if (response.IsSuccessStatusCode)
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (root.TryGetProperty("isSuccess", out var success)
				&& success.GetBoolean()
				&& root.TryGetProperty("data", out var data))
			{
				var token = data.ValueKind == JsonValueKind.String
					? data.GetString()
					: data.GetRawText().Trim('"');

				if (!string.IsNullOrEmpty(token))
				{
					// Store token if needed for API calls
					HttpContext.Session.SetString("jwt", token);

					// Parse JWT
					var handler = new JwtSecurityTokenHandler();
					var jwt = handler.ReadJwtToken(token);

					var claims = new List<Claim>(jwt.Claims);

					// Optional: store token as claim
					claims.Add(new Claim("jwt", token));

					var identity = new ClaimsIdentity(
						claims,
						CookieAuthenticationDefaults.AuthenticationScheme
					);

					var principal = new ClaimsPrincipal(identity);

					await HttpContext.SignInAsync(
						CookieAuthenticationDefaults.AuthenticationScheme,
						principal,
						new AuthenticationProperties
						{
							IsPersistent = true,
							ExpiresUtc = jwt.ValidTo
						});

					return RedirectToPage("/PublicDiscovery/Index");
				}
			}
		}
		else
		{
			var err = JsonSerializer.Deserialize<JsonElement>(json);

			Error = err.TryGetProperty("errorMessage", out var e)
				? e.GetString() ?? "Login failed."
				: "Login failed.";
		}

		return Page();
	}
}
