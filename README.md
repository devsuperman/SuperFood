# SuperFood

A multi-tenant restaurant management SaaS. See:

- [`docs/user-stories.md`](docs/user-stories.md) — product scope: roles, domain entities, epics and user stories.
- [`docs/tech-stack.md`](docs/tech-stack.md) — architecture: .NET 10 Minimal API (Vertical Slice Architecture) + PostgreSQL backend, Blazor WebAssembly + MudBlazor frontend.

## Running locally

### Option A — Docker Compose (everything, one command)

Prerequisites: Docker.

```bash
docker compose up --build
```

This builds and starts three containers: PostgreSQL, the API (applies EF Core
migrations and seeds the platform admin automatically), and the Blazor
client served by nginx.

- Client: http://localhost:8080
- API: http://localhost:5080
- Postgres: localhost:5432

Sign in at http://localhost:8080/login with `admin@superfood.dev` /
`Passw0rd!Admin` (the seeded platform admin) to create your first restaurant.
Override those, or the JWT signing key, by copying [`.env.example`](.env.example)
to `.env` before starting — see that file for the variables.

### Option B — .NET SDK directly (faster edit/run loop while developing)

Prerequisites: .NET 10 SDK, a PostgreSQL 16 instance (`docker compose up -d postgres` starts just that one).

```bash
# 1. Start Postgres
docker compose up -d postgres

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
