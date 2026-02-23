using System.Net;
using System.Text.RegularExpressions;
using Planova.IntegrationTests.Testing;

namespace Planova.IntegrationTests.RazorPages;

public class RazorAuthAndEventManagementTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;

	public RazorAuthAndEventManagementTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClientWithOptions();
	}

	public Task InitializeAsync() => _factory.InitializeAsync();
	public Task DisposeAsync() => Task.CompletedTask;

	private async Task<(string Email, string Password, string? Token)> GetLoginFormDataAsync()
	{
		var getResponse = await _client.GetAsync("/User/Login");
		getResponse.EnsureSuccessStatusCode();
		var html = await getResponse.Content.ReadAsStringAsync();
		var tokenMatch = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
		var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : null;
		return ("creator@planova.com", "123456", token);
	}

	[Fact]
	public async Task GetLoginPage_Returns200()
	{
		var response = await _client.GetAsync("/User/Login");
		response.EnsureSuccessStatusCode();
	}

	[Fact]
	public async Task PostLogin_WithCreatorCredentials_RedirectsToPublicDiscovery()
	{
		var (email, password, token) = await GetLoginFormDataAsync();
		var formData = new List<KeyValuePair<string, string>>
		{
			new("Email", email),
			new("Password", password)
		};
		if (token != null)
			formData.Add(new("__RequestVerificationToken", token));
		var content = new FormUrlEncodedContent(formData);
		var response = await _client.PostAsync("/User/Login", content);
		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.Contains("/PublicDiscovery", response.Headers.Location?.ToString());
	}

	[Fact]
	public async Task AfterLogin_EventManagementPage_Returns200()
	{
		var (email, password, token) = await GetLoginFormDataAsync();
		var formData = new List<KeyValuePair<string, string>>
		{
			new("Email", email),
			new("Password", password)
		};
		if (token != null)
			formData.Add(new("__RequestVerificationToken", token));
		await _client.PostAsync("/User/Login", new FormUrlEncodedContent(formData));

		var response = await _client.GetAsync("/EventManagment/Events/Index");
		response.EnsureSuccessStatusCode();
		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("Event Managment", html);
	}

	[Fact]
	public async Task Unauthenticated_EventManagement_RedirectsToLogin()
	{
		var client = _factory.CreateClientWithOptions();
		var response = await client.GetAsync("/EventManagment/Events/Index");
		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.Contains("/User/Login", response.Headers.Location?.ToString());
	}
}
