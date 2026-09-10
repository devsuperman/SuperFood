# SuperFood

A multi-tenant restaurant management SaaS. See:

- [`docs/user-stories.md`](docs/user-stories.md) — product scope: roles, domain entities, epics and user stories.
- [`docs/tech-stack.md`](docs/tech-stack.md) — architecture: .NET 10 Minimal API (Vertical Slice Architecture) + PostgreSQL backend, Blazor WebAssembly + MudBlazor frontend.

## Running locally

Prerequisites: .NET 10 SDK, a PostgreSQL 16 instance (`docker compose up -d` starts one).

```bash
# 1. Start Postgres
docker compose up -d

# 2. Configure secrets (once)
cd src/SuperFood.Api
dotnet user-secrets set "Jwt:SigningKey" "<a long random string>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=superfood;Username=postgres;Password=postgres"
dotnet user-secrets set "PlatformAdmin:Email" "admin@superfood.dev"
dotnet user-secrets set "PlatformAdmin:Password" "<a strong password>"

# 3. Run the API (applies EF Core migrations and seeds the platform admin on startup)
dotnet run --project src/SuperFood.Api

# 4. Run the Blazor client (in another terminal)
dotnet run --project src/SuperFood.Client
```

Sign in at the client's `/login` with the seeded platform admin to create your first restaurant.

## Tests

```bash
dotnet test tests/SuperFood.UnitTests
dotnet test tests/SuperFood.IntegrationTests   # requires Docker (Testcontainers spins up PostgreSQL)
```
