using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Planova.IntegrationTests.Testing;

public static class TestAuth
{
	public static async Task RegisterAsync(
		HttpClient client,
		string email,
		string role,
		string password = "123456")
	{
		var body = new
		{
			Name = "Test User",
			Email = email,
			PhoneNumber = "+1234567890",
			Password = password,
			Role = role
		};
		await client.PostAsJsonAsync("/api/auth/register", body);
	}

	public static async Task<string?> LoginAndGetJwtAsync(
		HttpClient client,
		string email,
		string password = "123456")
	{
		var body = new { Email = email, Password = password };
		var response = await client.PostAsJsonAsync("/api/auth/login", body);
		var json = await response.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;
		if (root.TryGetProperty("isSuccess", out var success) && success.GetBoolean()
			&& root.TryGetProperty("data", out var data))
		{
			var token = data.ValueKind == JsonValueKind.String
				? data.GetString()
				: data.GetRawText().Trim('"');
			return token;
		}
		return null;
	}

	public static HttpClient CreateAuthorizedClient(CustomWebApplicationFactory factory, string jwt)
	{
		var client = factory.CreateClientWithOptions();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
		return client;
	}
}
