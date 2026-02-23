using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Planova.IntegrationTests.Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
	private readonly SqliteConnection _connection = new("DataSource=:memory:");

	public CustomWebApplicationFactory()
	{
		_connection.Open();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureServices(services =>
		{
			var descriptor = services.SingleOrDefault(
				d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
			if (descriptor != null)
				services.Remove(descriptor);

			services.AddDbContext<AppDbContext>(options =>
				options.UseSqlite(_connection));

			services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
			{
				options.ForwardDefaultSelector = ctx =>
					ctx.Request.Headers.Authorization.FirstOrDefault()?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
						? JwtBearerDefaults.AuthenticationScheme
						: null;
			});

			services.AddHttpClient("")
				.ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
		});
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_connection.Dispose();
		base.Dispose(disposing);
	}

	public async Task InitializeAsync()
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.EnsureDeletedAsync();
		await db.Database.EnsureCreatedAsync();
		await TestData.SeedBaselineAsync(scope.ServiceProvider);
	}

	public HttpClient CreateClientWithOptions()
	{
		var client = CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost"),
			AllowAutoRedirect = false,
			HandleCookies = true
		});
		return client;
	}
}
