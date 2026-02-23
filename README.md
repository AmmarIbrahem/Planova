# Planova — Event Registration (Interview Task)

Planova is a small **ASP.NET Core (.NET 8)** application that implements a simple event-registration system with three user roles:

- **Admin** (authenticated): manage events and view registrations for their all events
- **Event Creator** (authenticated): manage events and view registrations for their own events
- **Participant** (public/anonymous booking): browse events and register for an event

The solution follows a **Clean Architecture / DDD-lite** structure (API → Application → Domain → Infrastructure) and includes **integration tests** for API + Razor Pages.

---

## Features

### Public (Participant)
- Browse all events
- View event details
- Register (book) for an event *(no login required)*

### Authenticated (Event Creator / Admin)
- Register / login (JWT-based)
- Create / update / delete events (JWT required, ownership enforced for event creators)
- View registrations for an event (JWT required, ownership enforced for event creators)
- View owned events (JWT required)
- Admin can access registrations across events

### Cross-cutting
- EF Core with SQL Server + migrations
- Output caching on read endpoints
- Fixed-window rate limiting on selected endpoints
- Global exception middleware
- Health checks

---

## Tech Stack

- **.NET 8** / ASP.NET Core
- **EF Core** (SQL Server provider)
- **MediatR** (CQRS-style handlers)
- **JWT Bearer auth** (API) + **Cookie auth** (Razor Pages UI)
- **Swagger** (Development)
- **xUnit** integration tests + `WebApplicationFactory`

---

## Repository Layout

```
.
├─ Planova.sln
├─ Planova/
│  ├─ API/                 # Controllers + Razor Pages UI
│  ├─ Application/         # CQRS handlers, interfaces, Result
│  ├─ Domain/              # Entities, enums, domain rules/exceptions
│  └─ Infrastructure/      # EF Core DbContext, repos, security, services, migrations, dev seed
└─ tests/
   └─ Planova.IntegrationTests/
```

Key paths:
- `Planova/API/Program.cs` — middleware, DI, auth, caching, rate limiting, seeding
- `Planova/Infrastructure/Persistence/AppDbContext.cs` — EF Core DbContext
- `Planova/Infrastructure/Persistence/DevSeed/DevUserSeeder.cs` — development seed users/events/bookings
- `tests/Planova.IntegrationTests` — API + Razor Pages integration tests

---

## Quick Start (Recommended: Docker)

Prereqs:
- Docker Desktop

1) Create/update `.env` at repo root (already present in this repo):
```bash
SA_PASSWORD=MyStrongP@ssword123
JWT_KEY=SuperSecretDevelopmentKey_ChangeInProduction
```

2) Start SQL Server + API:
```bash
docker compose up --build
```

3) Open:
- API base: `http://localhost:8080`
- Swagger (Development): `http://localhost:8080/swagger`

> The API container runs migrations on startup and seeds development data when `ASPNETCORE_ENVIRONMENT=Development`.

---

## Run Locally (Without Docker)

Prereqs:
- .NET 8 SDK
- SQL Server (or LocalDB on Windows)

1) Configure connection string + JWT settings in `Planova/appsettings.json`
   - `ConnectionStrings:DefaultConnection`
   - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`

2) Run:
```bash
dotnet restore
dotnet run --project Planova/Planova.csproj
```

---

## Seeded Development Users & Data

When running in **Development**, seeding is performed in:
`Planova/Infrastructure/Persistence/DevSeed/DevUserSeeder.cs`

Default password for seeded users:
- **Password:** `123456`

Seeded users:
- `admin@planova.com` (Admin)
- `creator@planova.com` (EventCreator)
- `participant1@planova.com` (Participant)
- `participant2@planova.com` (Participant)

Seeded events:
- **Planova Kickoff** (capacity 50)
- **Planova Workshop** (capacity 10)

Seeded bookings:
- Event 1: 2 bookings
- Event 2: 7 bookings

---

## API Endpoints (Summary)

Base: `/api`

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`

### Events
- `GET /api/events` *(public, cached)*
- `GET /api/events/{id}` *(public, cached)*
- `GET /api/events/ownedEvents` *(JWT)*
- `GET /api/events/{eventId}/registrations` *(JWT)*
- `POST /api/events` *(JWT, rate-limited)*
- `PUT /api/events/{id}` *(JWT, rate-limited)*
- `DELETE /api/events/{id}` *(JWT, rate-limited)*

### Bookings
- `POST /api/bookings/{eventId}/book` *(public, rate-limited)*

### Health
- `GET /health`
- `GET /health/live`
- `GET /health/ready`

> JWT is configured under `Jwt:*` settings. Protected endpoints use
> `Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)`.

---

## Razor Pages UI

Razor pages are hosted from:
`Planova/API/Pages`

Notable routes:
- `/` (Index)
- `/PublicDiscovery/*` (browse events)
- `/User/Login` (login)
- `/EventManagment/*` (creator management pages)

Cookie auth config:
- Cookie name: `Planova.Auth`
- Login path: `/User/Login`

---

## Running Tests

Prereqs:
- .NET 8 SDK

Run:
```bash
dotnet test
```

Integration tests live at:
`tests/Planova.IntegrationTests`

Notes:
- Tests use `WebApplicationFactory` with an in-memory database strategy for deterministic runs.
- Parallelization is disabled to avoid rate-limiter interference.

---

## Configuration Notes

### Rate Limiting
Fixed-window policy in `Program.cs`:
- Permit: 10 requests / 10 seconds
- Queue: 2

### Output Caching
Read endpoints use output cache policy `"ReadCache"`.

---

## Troubleshooting

### SQL Server password issues (Docker)
Ensure `SA_PASSWORD` meets SQL Server complexity requirements.

### Migrations / DB errors
- Docker: delete volume if you want a clean DB:
  ```bash
  docker compose down -v
  ```
- Local: ensure your connection string points to a running SQL Server instance.

### 429 Too Many Requests
The app uses rate limiting. Slow down requests or rerun tests (tests already disable parallelization).

---

## License
Interview project / sample code.
