using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Planova.API.Middleware;
using Planova.API.Pages;
using Planova.Application.Auth.Register;
using Planova.Application.Common.Interfaces;
using Planova.Application.EventManagement.CreateEvent;
using Planova.Infrastructure.Persistence;
using Planova.Infrastructure.Persistence.DevSeed;
using Planova.Infrastructure.Persistence.Repositories;
using Planova.Infrastructure.Security;
using Planova.Infrastructure.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#region Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(connectionString));

#endregion

#region Repositories & Infrastructure

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiClient>();

#endregion

#region MediatR

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssembly(typeof(CreateEventCommand).Assembly));

builder.Services.AddScoped<RegisterCommandHandler>();
builder.Services.AddScoped<CreateEventCommandHandler>();

#endregion

#region Authentication & Authorization (Role-Based)

builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
	options.LoginPath = "/User/Login";
	options.LogoutPath = "/Logout";
	options.AccessDeniedPath = "/User/AccessDenied";
	options.Cookie.Name = "Planova.Auth";
	options.Cookie.HttpOnly = true;
	options.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
	var key = builder.Configuration["Jwt:Key"]
		?? throw new InvalidOperationException("JWT key not configured.");

	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
		RoleClaimType = ClaimTypes.Role,
		NameClaimType = ClaimTypes.NameIdentifier
	};
});

builder.Services.AddAuthorization();

#endregion

#region Rate Limiting

const string FixedPolicy = "fixed";

builder.Services.AddRateLimiter(options =>
{
	options.AddPolicy(FixedPolicy, context =>
		RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: context.User?.Identity?.Name
			  ?? context.Connection.RemoteIpAddress?.ToString()
			  ?? "anonymous",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromSeconds(10),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 2
			}));

	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

#endregion

#region Caching
builder.Services.AddOutputCache(options =>
{
	options.AddPolicy("ReadCache", policy =>
		policy.Expire(TimeSpan.FromSeconds(5))
			  .SetVaryByQuery("*"));
});
#endregion

#region MVC + Razor + Session

builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
	options.RootDirectory = "/API/Pages");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme.",
		Name = "Authorization",
		In = Microsoft.OpenApi.ParameterLocation.Header,
		Type = Microsoft.OpenApi.SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT"
	});

	c.AddSecurityRequirement((document) => new Microsoft.OpenApi.OpenApiSecurityRequirement
	{
		[new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
	});
});

#endregion

#region CORS

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .AllowAnyMethod());
});

#endregion

#region Health Checks

builder.Services.AddHealthChecks()
	.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
	.AddDbContextCheck<AppDbContext>(tags: new[] { "ready" });

#endregion

var app = builder.Build();

#region Development Seeding

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	if (app.Environment.EnvironmentName != "Testing")
		db.Database.Migrate();
	if (app.Environment.IsDevelopment())
	{
		await DevUserSeeder.SeedAsync(scope.ServiceProvider);
		app.UseSwagger();
		app.UseSwaggerUI(c =>
		{
			c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planova API v1");
			c.RoutePrefix = "swagger";
		});
	}
	else
	{
		app.UseHsts();
	}
}

#endregion

#region Middleware Pipeline (Correct Order)

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseOutputCache();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
#endregion

#region Endpoints

app.MapControllers();

app.MapRazorPages();

app.MapHealthChecks("/health", new HealthCheckOptions
{
	ResponseWriter = async (context, report) =>
	{
		context.Response.ContentType = "application/json";

		var result = JsonSerializer.Serialize(new
		{
			status = report.Status.ToString(),
			checks = report.Entries.Select(e => new
			{
				name = e.Key,
				status = e.Value.Status.ToString(),
				error = e.Value.Exception?.Message,
				duration = e.Value.Duration.ToString()
			})
		});

		await context.Response.WriteAsync(result);
	}
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("ready")
});

#endregion

app.Run();

public partial class Program;