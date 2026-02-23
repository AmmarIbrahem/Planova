using Planova.IntegrationTests.Testing;

namespace Planova.IntegrationTests.Api;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;

	public AuthApiTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClientWithOptions();
	}

	public Task InitializeAsync() => _factory.InitializeAsync();
	public Task DisposeAsync() => Task.CompletedTask;

	[Fact]
	public async Task Register_Success_Returns200()
	{
		var body = new
		{
			Name = "New User",
			Email = "newuser@planova.test",
			PhoneNumber = "+1111111111",
			Password = "123456",
			Role = "EventCreator"
		};
		var response = await _client.PostAsJsonAsync("/api/auth/register", body);
		Assert.True(response.IsSuccessStatusCode);
		var json = await response.Content.ReadAsStringAsync();
		using var doc = System.Text.Json.JsonDocument.Parse(json);
		Assert.True(doc.RootElement.GetProperty("isSuccess").GetBoolean());
	}

	[Fact]
	public async Task Register_DuplicateEmail_Returns400()
	{
		await TestAuth.RegisterAsync(_client, "dup@planova.test", "EventCreator");
		var body = new
		{
			Name = "Dup",
			Email = "dup@planova.test",
			PhoneNumber = "+1111111111",
			Password = "123456",
			Role = "EventCreator"
		};
		var response = await _client.PostAsJsonAsync("/api/auth/register", body);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var json = await response.Content.ReadAsStringAsync();
		Assert.Contains("already", json, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Login_ValidCredentials_Returns200WithToken()
	{
		var body = new { Email = "creator@planova.com", Password = "123456" };
		var response = await _client.PostAsJsonAsync("/api/auth/login", body);
		response.EnsureSuccessStatusCode();
		var token = await TestAuth.LoginAndGetJwtAsync(_client, "creator@planova.com", "123456");
		Assert.NotNull(token);
		Assert.NotEmpty(token);
	}

	[Fact]
	public async Task Login_InvalidPassword_Returns401()
	{
		var body = new { Email = "creator@planova.com", Password = "wrongpassword" };
		var response = await _client.PostAsJsonAsync("/api/auth/login", body);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
