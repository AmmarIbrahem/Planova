using Planova.IntegrationTests.Testing;

namespace Planova.IntegrationTests.Api;

public class EventManagementApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;
	private TestData.BaselineIds _ids = null!;

	public EventManagementApiTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClientWithOptions();
	}

	public async Task InitializeAsync()
	{
		await _factory.InitializeAsync();
		using var scope = _factory.Services.CreateScope();
		_ids = await TestData.SeedBaselineAsync(scope.ServiceProvider);
	}
	public Task DisposeAsync() => Task.CompletedTask;

	[Fact]
	public async Task CreateEvent_NoToken_Returns401()
	{
		var body = new
		{
			Name = "Test Event",
			Description = "Desc",
			Location = "Loc",
			StartTime = DateTime.UtcNow.AddDays(1),
			EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
			Capacity = 20,
			CreatorId = _ids.CreatorId
		};
		var response = await _client.PostAsJsonAsync("/api/events", body);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task CreateEvent_WithCreatorToken_Returns201AndAppearsInList()
	{
		var jwt = await TestAuth.LoginAndGetJwtAsync(_client, "creator@planova.com");
		Assert.NotNull(jwt);
		var authClient = TestAuth.CreateAuthorizedClient(_factory, jwt!);

		var start = DateTime.UtcNow.AddDays(2);
		var body = new
		{
			Name = "New Event",
			Description = "New desc",
			Location = "Berlin",
			StartTime = start,
			EndTime = start.AddHours(2),
			Capacity = 25,
			CreatorId = _ids.CreatorId
		};
		var createResponse = await authClient.PostAsJsonAsync("/api/events", body);
		createResponse.EnsureSuccessStatusCode();

		var createJson = await createResponse.Content.ReadAsStringAsync();
		using var doc = System.Text.Json.JsonDocument.Parse(createJson);
		var root = doc.RootElement;
		Guid createdId = root.ValueKind == System.Text.Json.JsonValueKind.String
			? Guid.Parse(root.GetString() ?? "")
			: root.TryGetProperty("id", out var idEl)
				? Guid.Parse(idEl.GetString() ?? "")
				: root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idEl2)
					? Guid.Parse(idEl2.GetString() ?? "")
					: Guid.Empty;
		Assert.NotEqual(Guid.Empty, createdId);

		var listResponse = await authClient.GetAsync("/api/events");
		listResponse.EnsureSuccessStatusCode();
		var listJson = await listResponse.Content.ReadAsStringAsync();
		Assert.Contains(createdId.ToString(), listJson);
	}

	[Fact]
	public async Task UpdateEvent_WithCreatorToken_Returns204AndReflectsChanges()
	{
		var jwt = await TestAuth.LoginAndGetJwtAsync(_client, "creator@planova.com");
		Assert.NotNull(jwt);
		var authClient = TestAuth.CreateAuthorizedClient(_factory, jwt!);

		var start = DateTime.UtcNow.AddDays(3);
		var body = new
		{
			Name = "Updated Event Name",
			Description = "Updated desc",
			Location = "Munich",
			StartTime = start,
			EndTime = start.AddHours(3),
			Capacity = 30,
			CreatorId = _ids.CreatorId
		};
		var response = await authClient.PutAsJsonAsync($"/api/events/{_ids.Event1Id}", body);
		response.EnsureSuccessStatusCode();

		var getResponse = await _client.GetAsync($"/api/events/{_ids.Event1Id}");
		getResponse.EnsureSuccessStatusCode();
		var ev = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal("Updated Event Name", ev.GetProperty("name").GetString());
		Assert.Equal("Munich", ev.GetProperty("location").GetString());
	}

	[Fact]
	public async Task DeleteEvent_WithCreatorToken_Returns204AndEventNotFound()
	{
		var jwt = await TestAuth.LoginAndGetJwtAsync(_client, "creator@planova.com");
		Assert.NotNull(jwt);
		var authClient = TestAuth.CreateAuthorizedClient(_factory, jwt!);

		var start = DateTime.UtcNow.AddDays(5);
		var createBody = new
		{
			Name = "To Delete",
			Description = "Desc",
			Location = "Loc",
			StartTime = start,
			EndTime = start.AddHours(1),
			Capacity = 15,
			CreatorId = _ids.CreatorId
		};
		var createRes = await authClient.PostAsJsonAsync("/api/events", createBody);
		createRes.EnsureSuccessStatusCode();
		var createJson = await createRes.Content.ReadAsStringAsync();
		Guid toDeleteId;
		using (var doc = System.Text.Json.JsonDocument.Parse(createJson))
		{
			var root = doc.RootElement;
			if (root.ValueKind == System.Text.Json.JsonValueKind.String)
				toDeleteId = Guid.Parse(root.GetString() ?? "");
			else if (root.TryGetProperty("id", out var idEl))
				toDeleteId = Guid.Parse(idEl.GetString() ?? "");
			else if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idEl2))
				toDeleteId = Guid.Parse(idEl2.GetString() ?? "");
			else
				throw new InvalidOperationException($"Could not parse event id from: {createJson}");
		}

		var deleteResponse = await authClient.DeleteAsync($"/api/events/{toDeleteId}");
		Assert.True(deleteResponse.IsSuccessStatusCode);
	}

	[Fact]
	public async Task DeleteEvent_NonOwner_Returns400()
	{
		await TestAuth.RegisterAsync(_client, "othercreator@planova.test", "EventCreator");
		var otherJwt = await TestAuth.LoginAndGetJwtAsync(_client, "othercreator@planova.test");
		Assert.NotNull(otherJwt);
		var otherClient = TestAuth.CreateAuthorizedClient(_factory, otherJwt!);

		var deleteResponse = await otherClient.DeleteAsync($"/api/events/{_ids.Event1Id}");
		Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

		var getResponse = await _client.GetAsync($"/api/events/{_ids.Event1Id}");
		getResponse.EnsureSuccessStatusCode();
	}

	[Fact]
	public async Task GetRegistrations_NonOwner_Returns400NotAuthorized()
	{
		await TestAuth.RegisterAsync(_client, "othercreator2@planova.test", "EventCreator");
		var otherJwt = await TestAuth.LoginAndGetJwtAsync(_client, "othercreator2@planova.test");
		Assert.NotNull(otherJwt);
		var otherClient = TestAuth.CreateAuthorizedClient(_factory, otherJwt!);

		var response = await otherClient.GetAsync($"/api/events/{_ids.Event1Id}/registrations");
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var json = await response.Content.ReadAsStringAsync();
		Assert.Contains("not authorized", json, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetRegistrations_Admin_Returns200()
	{
		var jwt = await TestAuth.LoginAndGetJwtAsync(_client, "admin@planova.com");
		Assert.NotNull(jwt);
		var adminClient = TestAuth.CreateAuthorizedClient(_factory, jwt!);

		var response = await adminClient.GetAsync($"/api/events/{_ids.Event1Id}/registrations");
		response.EnsureSuccessStatusCode();
	}
}
