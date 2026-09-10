---
project: SuperFood
document: Technical Architecture Specification
version: 1.0
last_updated: 2026-09-10
status: draft — approved for planning, not yet implemented
depends_on: docs/user-stories.md
---

# Purpose & How to Use This Document

This document defines **how** SuperFood is built, complementing
`docs/user-stories.md` (which defines **what** to build, tech-agnostic).

Conventions for any human or AI agent implementing from this file:

- Every architecture decision is recorded as an **ADR** (`ADR-xx`) in
  Section 13, with a rationale and the alternative considered. If a decision
  changes during implementation, add a new ADR that supersedes the old one —
  do not silently edit history.
- Section 14 maps each epic in `docs/user-stories.md` to the components that
  implement it — use it to find where a given user story should live in code.
- Follow the file/folder conventions in Section 2 exactly so multiple
  contributors (human or AI) produce a consistent codebase.
- Where a choice below is marked `(future)`, do not build it yet — it's noted
  so the design doesn't block adding it later.

---

## 1. Overview & Guiding Principles

- **Vertical Slice Architecture (VSA)**: code is organized by feature, not by
  technical layer. Each user story (or small group of related stories) is
  implemented as one self-contained "slice" — its endpoint, request/response
  contracts, validation, and handling logic live together. This avoids a
  generic `Services`/`Repositories` layer shared by unrelated features,
  which tends to accumulate incidental coupling as a system grows.
- **Minimal API**: .NET 10's Minimal API is used as the HTTP layer — no MVC
  controllers. Endpoints are plain functions/classes mapped to routes,
  which fits VSA's "one file, one feature" style better than controller
  classes that aggregate unrelated actions.
- **PostgreSQL**: relational database, accessed through EF Core. Relational
  fits the domain (restaurants, orders, order items) which is naturally
  normalized and needs transactional consistency (e.g. placing an order).
- **Blazor WebAssembly**: a single C# codebase for the frontend, sharing
  DTOs/contracts with the backend (`SuperFood.Contracts`), removing an
  entire class of request/response mismatch bugs between frontend and
  backend.
- Shared principle across both: **prefer explicit, colocated code over
  premature shared abstractions**. Introduce a shared helper only after the
  same logic appears in 3+ slices, not before.

---

## 2. Solution & Project Structure

```
SuperFood.sln
docker-compose.yml
.github/workflows/ci.yml
src/
  SuperFood.Api/                  Minimal API host
    Features/
      Restaurants/                EPIC-01, EPIC-02
      Users/                      EPIC-03
      Menu/
        Categories/               EPIC-04
        Products/                 EPIC-05
      Tables/                     EPIC-06
      Orders/
        DineIn/                   EPIC-07
        Delivery/                 EPIC-08
        Kitchen/                  EPIC-09
      Catalog/                    EPIC-10 (public/customer-facing menu read endpoints)
    Program.cs                    composition root: endpoint auto-registration, DI, middleware
  SuperFood.Domain/                Entities & value objects (Section 3 of user-stories.md)
  SuperFood.Infrastructure/
    Persistence/                  SuperFoodDbContext, EF configurations, migrations
    Identity/                     ApplicationUser, Identity setup
    RealTime/                     SignalR hubs
  SuperFood.Contracts/            DTOs shared between Api and Client
  SuperFood.Client/                Blazor WebAssembly host app (shell, routing, MudBlazor setup)
    Program.cs
  SuperFood.Client.Shared/        Razor Class Library — auth plumbing + PublicLayout, referenced by
                                   the host AND every module (see ADR-10; keeps modules from having
                                   to reference the host, which would create a reference cycle)
  SuperFood.Client.Modules.Restaurants/    Razor Class Library — EPIC-01/EPIC-02, lazy-loaded
  SuperFood.Client.Modules.Users/          Razor Class Library — EPIC-03, lazy-loaded
  SuperFood.Client.Modules.Menu/           Razor Class Library — EPIC-04/EPIC-05, lazy-loaded
  SuperFood.Client.Modules.Tables/         Razor Class Library — EPIC-06, lazy-loaded
  SuperFood.Client.Modules.Orders/         Razor Class Library — EPIC-07/EPIC-08/EPIC-09, lazy-loaded
  SuperFood.Client.Modules.Catalog/        Razor Class Library — EPIC-10, eagerly loaded (customer-facing landing experience)
tests/
  SuperFood.UnitTests/            mirrors Api/Features folder names
  SuperFood.IntegrationTests/     Testcontainers-based, one test class per slice
```

**Rule**: a folder name under `Features/` in `SuperFood.Api` and `tests/*`
should always match the corresponding `SuperFood.Client.Modules.<Area>`
project name — this is how an implementer locates every layer of one
feature without a lookup table.

---

## 3. Vertical Slice Conventions

Anatomy of one slice (example: `Features/Menu/Products/CreateProduct.cs`,
implementing US-0501):

```
CreateProduct.cs
├── record CreateProductRequest(...)      — from SuperFood.Contracts
├── record CreateProductResponse(...)     — from SuperFood.Contracts
├── class CreateProductValidator : AbstractValidator<CreateProductRequest>
├── class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResponse>
└── class CreateProductEndpoint : IEndpoint
        MapPost("/api/restaurants/{restaurantId}/products", ...)
```

- **`IEndpoint` auto-registration**: every slice's endpoint class implements
  a shared `IEndpoint` interface (`void MapEndpoint(IEndpointRouteBuilder app)`).
  `Program.cs` uses reflection at startup to find and register all
  `IEndpoint` implementations — adding a new slice never requires touching
  `Program.cs`.
- **MediatR** is used for command/query dispatch (`IRequestHandler`), with a
  **pipeline behavior** for FluentValidation (validates before the handler
  runs) and one for logging. This keeps cross-cutting concerns out of each
  handler's business logic without introducing a shared base service class.
- Handlers talk to `SuperFoodDbContext` directly — no repository/unit-of-work
  abstraction over EF Core. EF Core's `DbContext` already is a unit of work;
  wrapping it adds a layer with no behavior of its own.
- A slice may share domain entities and validation logic with other slices
  in the same feature folder, but never reaches into another feature
  folder's internals — cross-feature communication goes through published
  domain events (e.g. `OrderReadyEvent`) or a direct call to the other
  slice's handler via MediatR `Send`, never a shared mutable service.

---

## 4. Multi-Tenancy Strategy

- **Single database, shared schema.** All tenant-scoped tables
  (`Users`, `Categories`, `Products`, `Tables`, `Orders`, …) carry a
  `RestaurantId` column.
- An EF Core **global query filter** is applied to every tenant-scoped
  entity, scoped to the current request's `restaurant_id` claim (resolved
  via a scoped `ICurrentTenantProvider`). This makes cross-tenant data leaks
  a framework-level default rather than something each slice must remember.
- `platform_admin` requests bypass the filter (via
  `IgnoreQueryFilters()` in the specific EPIC-01 slices that legitimately
  need cross-tenant visibility, e.g. US-0102's restaurant list).
- Chosen over database-per-tenant or schema-per-tenant because the expected
  tenant count and per-tenant data volume don't yet justify the operational
  overhead of managing many databases/schemas (see ADR-04).

---

## 5. Authentication & Authorization

- **ASP.NET Core Identity** stores users in PostgreSQL via EF Core.
  `ApplicationUser` extends `IdentityUser` with a nullable `RestaurantId`
  (null for `platform_admin`; set for all restaurant staff roles).
- **JWT** issued on login (`/api/auth/login`), containing claims:
  `sub` (user id), `restaurant_id` (if any), and one claim per granted
  permission (not per role — see below). Refresh tokens stored server-side
  for revocation support.
- **Dynamic per-restaurant roles** (US-0302/US-0305 let a `restaurant_owner`
  define custom roles with custom permissions), so static
  `[Authorize(Roles = "...")]` attributes don't fit — a role name isn't a
  fixed enum. Instead:
  - Permissions are a fixed, code-defined set (e.g. `menu:write`,
    `orders:manage`, `tables:manage`, `users:manage`).
  - A restaurant's custom `Role` grants a subset of these permissions.
  - On login, the user's role's permissions are resolved into claims.
  - Endpoints declare required permissions via a `RequirePermission("orders:manage")`
    extension (`Api/Common/RouteHandlerBuilderExtensions.cs`) — implemented as
    `RequireAuthorization(permission)` against one ASP.NET Core policy per
    fixed permission (`RequireClaim("permission", permission)`), registered
    once in `Program.cs`. No custom `IAuthorizationHandler` was needed since a
    single claim check is all any permission requires.
  - **Client-side mirror**: Blazor's `[Authorize(Policy=...)]`/`AuthorizeView`
    run against the client's own `AddAuthorizationCore` policy set — it does
    not share the server's policy registrations. `SuperFood.Client/Program.cs`
    registers the identical `platform_admin` + per-permission policies purely
    for UI gating (show/hide nav links, redirect from a page the user
    shouldn't see); the API re-checks every permission server-side regardless,
    so this duplication is a UX nicety, not the security boundary.
- **Customers remain unauthenticated** for browsing and ordering (US-0701,
  US-0801) — no account required. A lightweight anonymous session id
  (browser-stored, not tied to `ApplicationUser`) correlates a customer's
  cart/order status lookup (US-0806), per the "no customer accounts" scope
  boundary in `docs/user-stories.md` §4.
- Blazor WASM stores the JWT in browser storage via a custom
  `AuthenticationStateProvider`; a `DelegatingHandler` attaches the
  `Authorization` header to every API call.

---

## 6. Data Access

- **EF Core** with the **Npgsql** provider (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Snake_case naming convention** via `EFCore.NamingConventions`, so
  PostgreSQL tables/columns follow Postgres convention while C# stays
  PascalCase.
- One `SuperFoodDbContext` in `SuperFood.Infrastructure/Persistence`, with
  entity configurations (`IEntityTypeConfiguration<T>`) colocated per entity,
  not one giant `OnModelCreating`.
- **Migrations** live in `SuperFood.Infrastructure`, applied automatically on
  startup in `Development`, and via an explicit `dotnet ef database update`
  step in deployment (future).

---

## 7. Real-Time Communication

- **SignalR** hub `OrdersHub` in `SuperFood.Infrastructure/RealTime`.
- Clients join a group named after their `restaurant_id` on connect — this
  is the tenant isolation boundary for real-time messages, mirroring the
  EF Core query filter approach in Section 4.
- Slices publish hub events **after** their command handler commits
  successfully (never speculatively before persistence):
  - `OrderCreated` → kitchen queue view (US-0901).
  - `OrderItemStatusChanged` → waiter view, customer status view (US-0902, US-0903, US-0806).
- Chosen over polling for lower latency and lower request volume once many
  tables/kitchen displays are active simultaneously (see ADR-03).

---

## 8. Cross-Cutting Concerns

- **Validation**: FluentValidation, run via the MediatR pipeline behavior
  (Section 3) — a request never reaches a handler unvalidated.
- **Error handling**: global exception-handling middleware maps exceptions
  to [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457)
  responses; domain-specific errors (e.g. "table already occupied") are
  modeled as typed results rather than exceptions where they're an expected
  outcome, not a bug.
- **Logging**: structured logging via `Microsoft.Extensions.Logging`, one
  log scope per request correlating to a request id.
- **API versioning**: none yet — single unversioned `/api/...` surface.
  Revisit if a breaking contract change is needed after the Blazor client
  and API are decoupled in deployment (future).

---

## 9. Frontend (Blazor WebAssembly)

### 9.1 Component library — MudBlazor

- **MudBlazor** is the UI component library for the entire client (layout,
  forms, data grids, dialogs, navigation, snackbar notifications). No custom
  CSS component framework is introduced alongside it — MudBlazor's Material
  Design components cover the admin/staff back-office surfaces (tables,
  forms, role/permission editors) and the customer-facing menu/cart/order
  screens.
- Theming (colors, typography) is centralized in one `MudTheme` instance in
  `SuperFood.Client`, shared by every module — a module never defines its
  own ad-hoc styles for things MudBlazor already themes (buttons, inputs,
  cards, dialogs).

### 9.2 Modular architecture

- Each feature area is its own **Razor Class Library (RCL)** project
  (`SuperFood.Client.Modules.<Area>`, Section 2), containing that area's
  pages, components, and feature-scoped state services. `SuperFood.Client`
  itself is a thin host: shell layout, routing table, auth bootstrapping,
  and MudBlazor registration — it contains no feature logic of its own.
- A module project depends only on `SuperFood.Contracts` and shared
  UI/auth infrastructure exposed by the host — never on another module
  project directly. Cross-module navigation happens through routed URLs,
  not direct component/service references, so modules stay independently
  buildable and (see below) independently loadable.
- This mirrors the backend's Vertical Slice boundaries (Section 3) on the
  client side: a contributor adding a feature touches one module project,
  not the shared host.

### 9.3 Lazy loading

- Each `SuperFood.Client.Modules.<Area>` assembly (and its dependencies) is
  marked as a `BlazorWebAssemblyLazyLoad` item in the host's `.csproj`, so
  it is **not** downloaded on first page load.
- The host's `Router` uses `OnNavigateAsync` to resolve which module
  assembly a requested route belongs to and calls
  `LazyAssemblyLoader.LoadAssembliesAsync(...)` before rendering, showing a
  MudBlazor loading indicator while the module downloads.
- `SuperFood.Client.Modules.Catalog` (EPIC-10, the public customer-facing
  menu/ordering flow — the highest-traffic, first-impression surface) is
  the one module loaded eagerly with the host, so a customer scanning a
  table QR code doesn't wait on an extra assembly fetch. All staff/admin
  modules (Restaurants, Users, Menu management, Tables, Orders/Kitchen) are
  lazy-loaded, since a given staff member typically only visits the modules
  relevant to their role (Section 5) — this keeps their initial download
  small and scales as more feature modules are added later.

### 9.4 Contracts, auth, and real-time

- **`SuperFood.Contracts`** is referenced directly by every module project —
  the same `CreateProductRequest`/`Response` records used by the API are
  used by the client, eliminating manual DTO duplication and drift.
- **Auth**: custom `AuthenticationStateProvider` (in the host) reads the JWT
  from browser local storage; a typed `HttpClient` per feature area
  (e.g. `IProductsApiClient`, defined in its own module) is registered with
  a `DelegatingHandler` that attaches the bearer token. A module can only
  call its own typed clients — it never reaches into another module's
  HTTP client.
- **Real-time**: `Microsoft.AspNetCore.SignalR.Client` connects to
  `OrdersHub` for the Kitchen and Waiter views inside
  `SuperFood.Client.Modules.Orders`; the public customer-facing
  menu/ordering pages in `Modules.Catalog` do not need a live connection
  except for order-status polling/updates (US-0806).
- **State management**: scoped DI services hold in-memory state local to
  their own module (e.g. `CartState` in `Modules.Catalog`). No external
  state management library is introduced until a concrete need for
  cross-module shared state appears — and even then, state is passed via
  the URL/query string or re-fetched from the API rather than through a
  direct module-to-module reference, to preserve the lazy-loading boundary.

---

## 10. Testing Strategy

- **Unit tests** (`SuperFood.UnitTests`, xUnit + FluentAssertions): one test
  class per handler, testing validation and business logic in isolation
  with an in-memory/mocked `DbContext` or SQLite in-memory provider.
- **Integration tests** (`SuperFood.IntegrationTests`, xUnit +
  `WebApplicationFactory` + `Testcontainers.PostgreSql`): spin up a real
  PostgreSQL container per test run, exercise full slices through HTTP,
  including multi-tenancy query filters and authorization.
- **Component tests** (`bUnit`, `Should` priority): key Blazor components
  (cart, order-status, kitchen queue) — not required to block initial
  delivery, added as those components stabilize.
- Every test class should reference the user story ID(s) it verifies in its
  name or a comment (e.g. `CreateProductTests` → "verifies US-0501").

---

## 11. Local Development Environment

- **`docker-compose.yml`** at repo root with a single `postgres` service
  (official `postgres` image, a fixed local port, a named volume for
  persistence across restarts).
- **`dotnet user-secrets`** for local connection strings/JWT signing key —
  never committed to the repo.
- On `Development` startup, `SuperFood.Api` applies pending EF Core
  migrations automatically against the Docker Compose Postgres instance.

---

## 12. CI Pipeline (lightweight outline)

`.github/workflows/ci.yml`, triggered on pull requests and pushes to the
main branch:

1. Checkout, setup .NET 10 SDK.
2. `dotnet restore`.
3. `dotnet build --no-restore`.
4. `dotnet test` for `SuperFood.UnitTests` and `SuperFood.IntegrationTests`
   (a `postgres` service container is declared in the workflow for the
   integration test job).

Publishing/deployment steps are `(future)` — not defined yet.

---

## 13. Architecture Decision Records

| ID | Decision | Alternative considered | Rationale |
|---|---|---|---|
| ADR-01 | Vertical Slice Architecture | Layered/Clean Architecture | Feature-oriented code reduces coupling through shared layers as the number of restaurant-management features grows; each story maps to one slice. |
| ADR-02 | EF Core (Npgsql) for data access | Dapper + raw SQL | Migrations, global query filters (needed for multi-tenancy, Section 4), and change tracking outweigh Dapper's raw-SQL control for this domain's CRUD-heavy, transactional workload. |
| ADR-03 | SignalR for real-time updates | Client polling | Kitchen/waiter views need low-latency updates (new order, item ready) with many concurrent connections (one per table/kitchen display); polling would multiply request volume and latency. |
| ADR-04 | Shared-schema multi-tenancy (`RestaurantId` column + query filters) | Database-per-tenant / schema-per-tenant | Avoids per-tenant infrastructure/migration overhead at current expected scale; EF Core global query filters give strong default isolation without it. |
| ADR-05 | JWT (ASP.NET Core Identity) over server-side sessions | Cookie-based server sessions; external OIDC provider | Blazor WASM is a fully client-side SPA — a bearer token fits its stateless API calls better than server session cookies; owning Identity in-app avoids an external dependency for MVP. |
| ADR-06 | Permission-claims model over static role attributes | `[Authorize(Roles = "...")]` with fixed role enum | Restaurant owners define custom roles at runtime (US-0302); permissions must be checked, not fixed role names. |
| ADR-07 | No repository/unit-of-work abstraction over EF Core | Generic repository pattern | `DbContext` already is a unit of work; an extra abstraction with no distinct behavior adds indirection without benefit. |
| ADR-08 | MudBlazor as the sole UI component library | Custom CSS/Bootstrap; another Blazor component kit | One consistent, themeable, actively-maintained Material Design component set covers both staff back-office (grids, forms, dialogs) and customer-facing screens without maintaining bespoke CSS. |
| ADR-09 | Modular client: one Razor Class Library per feature area, lazy-loaded | Single monolithic Blazor WASM project | Keeps initial download small as features grow (a waiter never downloads the Platform Admin module); mirrors the backend's Vertical Slice feature boundaries so ownership of a feature maps 1:1 on both sides of the stack. |
| ADR-10 | Extract `SuperFood.Client.Shared` (auth plumbing, `PublicLayout`) below both host and modules | Put auth types directly in `SuperFood.Client` | A module referencing the host to reach `ITokenAccessor`/`PublicLayout` would create a project-reference cycle (the host already references every module). A small shared library sitting below both sides resolves it without weakening the "modules don't reference each other" rule. |

---

## 14. Traceability: Epics → Architecture Components

| Epic | Backend location | Frontend location | Key architecture pieces |
|---|---|---|---|
| EPIC-01 Platform Administration | `Api/Features/Restaurants` | `Client.Modules.Restaurants` (lazy) | Query filter bypass (Section 4), `platform_admin` permission set |
| EPIC-02 Restaurant Settings & Onboarding | `Api/Features/Restaurants` | `Client.Modules.Restaurants` (lazy) | Standard tenant-scoped slice |
| EPIC-03 User & Role Management | `Api/Features/Users` | `Client.Modules.Users` (lazy) | Permission-claims model (Section 5, ADR-06) |
| EPIC-04 Menu Categories | `Api/Features/Menu/Categories` | `Client.Modules.Menu` (lazy) | Standard tenant-scoped slice |
| EPIC-05 Menu Products | `Api/Features/Menu/Products` | `Client.Modules.Menu` (lazy) | Standard tenant-scoped slice |
| EPIC-06 Table Management | `Api/Features/Tables` | `Client.Modules.Tables` (lazy) | QR generation, table session state |
| EPIC-07 On-Site Order Management | `Api/Features/Orders/DineIn` | `Client.Modules.Orders` (lazy) | `OrdersHub` (Section 7) |
| EPIC-08 Delivery Order Management | `Api/Features/Orders/Delivery` | `Client.Modules.Orders` (lazy) | `OrdersHub`, anonymous session id (Section 5) |
| EPIC-09 Order Lifecycle & Kitchen Workflow | `Api/Features/Orders/Kitchen` | `Client.Modules.Orders` (lazy) | `OrdersHub` events (Section 7) |
| EPIC-10 Customer Ordering Experience | `Api/Features/Catalog` | `Client.Modules.Catalog` (eager) | Unauthenticated endpoints, `CartState`, MudBlazor (Section 9) |
