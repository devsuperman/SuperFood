# CLAUDE.md

Guidance for Claude Code (or any agent) working in this repository.

## What this is

SuperFood is a multi-tenant restaurant management SaaS. A `platform_admin`
manages restaurant tenants; each restaurant independently manages its own
menu, tables, staff/roles, and orders (dine-in + delivery, capture-only, no
courier logistics). Customers order without an account, via a table QR code
(dine-in) or a delivery form — this is a customer-facing product, not just
an internal back-office tool.

**Two spec docs are the source of truth — read them before making product or
architecture decisions, and update them when a decision changes:**

- [`docs/user-stories.md`](docs/user-stories.md) — **what** to build: roles,
  domain entities, 10 epics of user stories with stable IDs (`US-EEss`),
  acceptance criteria, and an explicit out-of-scope list. Tech-agnostic.
- [`docs/tech-stack.md`](docs/tech-stack.md) — **how** it's built: solution
  structure, Vertical Slice conventions, multi-tenancy, auth, and an ADR log
  (`ADR-xx`) recording every architecture decision with its rationale and the
  alternative considered.

Both docs are written to be machine-parseable and self-contained; skim them
in full before assuming how something works. If you make an architecture
decision that isn't in `tech-stack.md` yet, add an ADR rather than leaving it
undocumented — a later session (or a later you) will trust the doc over
guessing from the code.

## Current state

Both backend and frontend are implemented end-to-end and verified working
(login → create restaurant → menu/table setup → anonymous customer order via
QR → live SignalR update to kitchen queue). See `docs/tech-stack.md` §14 for
an epic → code location map.

The Blazor frontend is a **single `SuperFood.Client` project** with plain
`Features/<Area>` folders (ADR-11) — it started as one Razor Class Library
per feature module plus a `Shared` project and lazy loading (ADR-09/ADR-10),
but that turned out to be ceremony this app's size didn't need, so it was
collapsed back down. If you ever see `SuperFood.Client.Modules.*` or
`SuperFood.Client.Shared` mentioned anywhere, that's stale — the ADR log in
`docs/tech-stack.md` §13 has the full history if you need to know why.

## Repo layout

```
src/
  SuperFood.Api/            Minimal API host — Features/<Area>/<Action>.cs vertical slices
  SuperFood.Domain/         Entities & enums, no framework dependencies
  SuperFood.Infrastructure/ EF Core (Npgsql), Identity, multi-tenancy filter, JWT, SignalR hub
  SuperFood.Contracts/      DTOs shared between Api and Client (the only thing Client references from the server side)
  SuperFood.Client/         Blazor WebAssembly frontend
tests/
  SuperFood.UnitTests/      xUnit + FluentAssertions; SQLite in-memory for handler tests needing a DbContext
  SuperFood.IntegrationTests/  WebApplicationFactory + Testcontainers.PostgreSql (needs Docker)
docs/
  user-stories.md           product spec
  tech-stack.md             architecture spec + ADRs
```

## Key conventions (see tech-stack.md for full detail)

- **One file per slice** in `SuperFood.Api/Features/<Area>/<Action>.cs`:
  request/response records, `AbstractValidator`, `IRequestHandler`, and an
  `IEndpoint` implementation that maps its own route — all in that one file.
  Endpoints self-register via reflection (`EndpointExtensions.MapEndpoints`);
  never wire a new endpoint into `Program.cs` by hand.
- **Multi-tenancy is a global EF Core query filter** scoped to the JWT's
  `restaurant_id` claim (`ICurrentTenantProvider`). Don't add manual
  `.Where(x => x.RestaurantId == ...)` filters in normal tenant-scoped
  queries — the filter already does it. Exceptions that legitimately need
  `.IgnoreQueryFilters()`: platform_admin cross-tenant reads, and anything
  running before a tenant claim exists (login, anonymous customer endpoints)
  — those must filter by an explicit `restaurantId` parameter instead.
- **Permissions, not roles, gate endpoints.** Restaurants define their own
  named roles at runtime (US-0302), so authorization checks a fixed
  permission claim (`RequirePermission("orders:manage")`), never a role
  name. The Blazor client mirrors the same policies client-side for UI
  gating only — the API is the real boundary.
- **No repository/service layer, no MediatR-free handlers** — handlers use
  `SuperFoodDbContext` directly. Don't introduce an abstraction until the
  same logic repeats in 3+ slices (tech-stack.md §1).
- Every user story has a stable ID — reference it in commits, tests, and
  code comments when the "why" isn't obvious from the code itself.

## Local development

```bash
# Postgres (either works)
docker compose up -d
# or, in this sandbox where Docker isn't available:
pg_ctlcluster 16 main start   # then create the `superfood` DB/user once if missing

# One-time secrets (from src/SuperFood.Api)
cd src/SuperFood.Api
dotnet user-secrets set "Jwt:SigningKey" "<long random string>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=superfood;Username=postgres;Password=postgres"
dotnet user-secrets set "PlatformAdmin:Email" "admin@superfood.dev"
dotnet user-secrets set "PlatformAdmin:Password" "<strong password>"

# Run (migrations + platform_admin seeding happen automatically in Development)
dotnet run --project src/SuperFood.Api        # http://localhost:5080 in this sandbox
dotnet run --project src/SuperFood.Client     # separate terminal

# Tests
dotnet test tests/SuperFood.UnitTests
dotnet test tests/SuperFood.IntegrationTests  # needs Docker — not runnable in this sandbox
```

## Sandbox gotchas (this environment specifically)

- No Docker daemon here — `docker compose` and the integration tests can't
  actually run; use `pg_ctlcluster` for a local Postgres instead, and treat
  `SuperFoodApiFactory`/Testcontainers-based tests as "correct if it fails
  only on 'can't reach Docker daemon'."
- `dotnet-ef` isn't on `PATH` by default after `dotnet tool install --global
  dotnet-ef`: run `export PATH="$PATH:/root/.dotnet/tools"` first.
- Background `dotnet run` processes die silently between tool calls unless
  launched with `setsid ... &`; plain `nohup ... & disown` isn't reliable
  here.
- The sandbox's egress proxy blocks `fonts.googleapis.com` and similar
  external CDNs — don't add a Google Fonts `<link>` to `index.html` (it
  stalls page loads under network policies like this one); MudBlazor's
  icons are SVG-based and don't need it.
- `dotnet new sln` on the installed .NET 10 SDK produces a `.slnx` (XML)
  file, not `.sln` — that's expected, not a mistake to "fix."
- Client-side Blazor `[Authorize(Policy=...)]` needs the same policies
  registered in `SuperFood.Client/Program.cs`'s `AddAuthorizationCore` as
  the server's — they're two separate registrations that don't share state,
  and skipping the client one throws `AuthorizationPolicy ... was not found`
  at render time, not at compile time.
