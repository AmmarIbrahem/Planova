using Planova.IntegrationTests.Testing;

namespace Planova.IntegrationTests.Api;

public class BookingApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;
	private TestData.BaselineIds _ids = null!;

	public BookingApiTests(CustomWebApplicationFactory factory)
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
	public async Task BookEvent_Anonymous_Success_Returns201()
	{
		var body = new
		{
			Email = "anonymous@test.com",
			Name = "Anonymous User",
			PhoneNumber = "+9999999999"
		};
		var response = await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event1Id}/book", body);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact]
	public async Task BookEvent_DuplicateEmail_Returns400()
	{
		var body = new
		{
			Email = "dupbook@test.com",
			Name = "First Booker",
			PhoneNumber = "+1111111111"
		};
		await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event1Id}/book", body);

		var body2 = new
		{
			Email = "dupbook@test.com",
			Name = "Duplicate Booker",
			PhoneNumber = "+2222222222"
		};
		var response = await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event1Id}/book", body2);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var json = await response.Content.ReadAsStringAsync();
		Assert.Contains("already booked", json, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task BookEvent_CreatorBooksOwnEvent_Returns400()
	{
		var body = new
		{
			Email = "creator@planova.com",
			Name = "Event Creator",
			PhoneNumber = "+1234567890"
		};
		var response = await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event1Id}/book", body);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task BookEvent_CapacityExceeded_Returns400()
	{
		await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event2Id}/book", new
		{
			Email = "cap1@test.com",
			Name = "Cap1",
			PhoneNumber = "+1111111111"
		});
		await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event2Id}/book", new
		{
			Email = "cap2@test.com",
			Name = "Cap2",
			PhoneNumber = "+2222222222"
		});
		await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event2Id}/book", new
		{
			Email = "cap3@test.com",
			Name = "Cap3",
			PhoneNumber = "+3333333333"
		});

		var response = await _client.PostAsJsonAsync($"/api/bookings/{_ids.Event2Id}/book", new
		{
			Email = "cap4@test.com",
			Name = "Cap4",
			PhoneNumber = "+4444444444"
		});
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var json = await response.Content.ReadAsStringAsync();
		Assert.Contains("capacity exceeded", json, StringComparison.OrdinalIgnoreCase);
	}
}
