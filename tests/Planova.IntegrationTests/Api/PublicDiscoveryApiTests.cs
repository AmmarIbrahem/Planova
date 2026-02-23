using Planova.IntegrationTests.Testing;

namespace Planova.IntegrationTests.Api;

public class PublicDiscoveryApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;
	private TestData.BaselineIds _ids = null!;

	public PublicDiscoveryApiTests(CustomWebApplicationFactory factory)
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
	public async Task GetEvents_Returns200WithList()
	{
		var response = await _client.GetAsync("/api/events");
		response.EnsureSuccessStatusCode();
		var events = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
		Assert.NotNull(events);
		Assert.NotEmpty(events);
	}

	[Fact]
	public async Task GetEventById_Returns200WithCorrectId()
	{
		var response = await _client.GetAsync($"/api/events/{_ids.Event1Id}");
		response.EnsureSuccessStatusCode();
		var ev = await response.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal(_ids.Event1Id.ToString(), ev.GetProperty("id").GetString());
	}
}
