# FocusTracker API

An ASP.NET Core Web API built with .NET 8 and Entity Framework Core for tracking focus sessions and productivity. The system enforces session lifecycle rules, automatic pause-limit expiration, and daily analytics, following Clean Architecture with a strict separation between the Domain, Application, Infrastructure, and Api layers.

---

## Key Features & Business Rules

- **Session Lifecycle Management** — sessions move through a controlled state machine: `Pending → Running → Paused → Finished`.
- **Automated Extended-Pause Enforcement** — if a session's accumulated pause time exceeds its configured `MaxAllowedPauseInMinutes`, it is automatically transitioned to `Finished`, both lazily (on access) and in bulk (on listing/summary queries) using `EF.Functions.DateDiffMinute` inside a single `ExecuteUpdateAsync` call, so the calculation happens at the database level rather than in application memory.
- **Accurate Focus-Time Calculation** — focus time is computed dynamically for running sessions (elapsed time since the last state change is added on read) so reported totals never drift, even for sessions that are still in progress.
- **Daily Summary & Analytics** — aggregates total focus minutes and session activity for a given date, applying the same auto-finish evaluation before summarizing.
- **JWT Authentication with Refresh Tokens** — access + refresh token issuance, strongly-typed `JwtOptions`, and a startup guard that rejects any signing key shorter than 32 bytes (256 bits).
- **Scoped Multi-Tenancy** — categories and sessions are always filtered by the authenticated user's id at the query level; there is no cross-user data access.
- **Centralized Request Validation** — all incoming DTOs are validated through FluentValidation, executed automatically via a custom `IAsyncActionFilter`.
- **Global Exception Handling** — a single middleware maps domain/validation exceptions to consistent JSON error responses with the correct HTTP status codes.
- **Rate Limiting** — a stricter fixed-window policy on authentication endpoints and a per-user (or per-IP) policy on the rest of the API.
- **Structured Logging** — Serilog with console output and daily rolling JSON log files.

---

## Project Architecture & Layering

The solution follows Clean Architecture to keep business logic independent of frameworks and infrastructure concerns:

```
TaskManager.Domain            Core entities, enums, and business rules. No outward dependencies.
TaskManager.Application       DTOs, service interfaces, FluentValidation validators, strongly-typed options.
TaskManager.Infrastructure     EF Core DbContext, migrations, and service implementations (business logic + data access).
TaskManager.Api                 Controllers, middleware, action filters, DI composition, and Swagger/OpenAPI configuration.
```

Dependency direction: `Api → Infrastructure → Application → Domain`.

---

## Performance & Database Optimizations

1. **`AsNoTracking` read operations** — applied to all read-only query paths (`GetUserSessionsAsync`, `GetDaySummaryAsync`) to remove EF Core change-tracking overhead on responses that are never persisted back.
2. **`ExecuteUpdateAsync` batch updates** — the auto-finish check for expired paused sessions runs as a single `UPDATE` statement evaluated entirely by the database engine (via `EF.Functions.DateDiffMinute`), rather than loading entities into memory, mutating them, and calling `SaveChangesAsync`. This avoids N+1 patterns and keeps the operation O(1) round trips regardless of how many sessions qualify.
3. **Two-phase paginated queries** — session listings first select only the page's `SessionId`s (with `Skip`/`Take`), run the bulk auto-finish check against just that id set, and only then load the full entities with `Include(s => s.Category)` — so navigation properties are never materialized for rows outside the current page.

---

## Data Model

| Entity          | Description                                                                                          |
|------------------|--------------------------------------------------------------------------------------------------------|
| **User**         | Account with `Name`, `Email`, a BCrypt password hash, and a `Role` (`User` / `Admin`).                |
| **Category**     | A named grouping of sessions, owned by a single user.                                                 |
| **Session**      | A focus session with `Status`, timestamps, pause count, `TotalPausedInMinutes`, `MaxAllowedPauseInMinutes`, and computed `FocusTimeInMinutes`. |
| **RefreshToken** | Issued per login/registration; tracks expiry (`IsExpired`) and revocation (`IsRevoked`) independently, combined into `IsActive`. |

---

## Tech Stack & Tools

| Layer          | Technology                                          |
|----------------|-------------------------------------------------------|
| Framework      | ASP.NET Core Web API (.NET 8)                        |
| ORM            | Entity Framework Core 8                              |
| Database       | MySQL 8.0 (via `Pomelo.EntityFrameworkCore.MySql`)  |
| Authentication | JWT Bearer tokens (HMAC-SHA256) + refresh tokens     |
| Password Hash  | BCrypt.Net                                            |
| Validation     | FluentValidation                                      |
| Logging        | Serilog (Console + rolling daily file sinks)          |
| Rate Limiting  | `Microsoft.AspNetCore.RateLimiting`                  |
| Documentation  | Swagger / OpenAPI (Swashbuckle)                       |

---

## API Endpoints

### Authentication — `/api/auth` (rate limited: 5 requests/minute per IP)

| Method | Endpoint          | Auth required | Description                                  |
|--------|--------------------|:--------------:|------------------------------------------------|
| POST   | `/register`       | No             | Create a new user account                     |
| POST   | `/login`           | No             | Authenticate and receive access + refresh tokens |
| POST   | `/refreshToken`    | No             | Exchange a valid refresh token for a new token pair |
| POST   | `/revokeToken`     | Yes            | Revoke a refresh token                         |

### Categories — `/api/categories` (requires auth)

| Method | Endpoint  | Description                          |
|--------|-----------|----------------------------------------|
| POST   | `/`       | Create a category                     |
| GET    | `/`       | List the authenticated user's categories (paginated) |
| GET    | `/{id}`   | Retrieve a category by id             |
| PUT    | `/{id}`   | Update a category                     |
| DELETE | `/{id}`   | Delete a category                     |

### Sessions — `/api/sessions` (requires auth)

| Method | Endpoint             | Description                                                        |
|--------|-----------------------|----------------------------------------------------------------------|
| POST   | `/`                   | Create a new session in `Pending` status                            |
| GET    | `/`                   | List the authenticated user's sessions (paginated, auto-finish evaluated) |
| GET    | `/{id}`               | Retrieve a session by id (auto-finish evaluated on access)          |
| PUT    | `/{id}`               | Update a session's title, summary, or category                     |
| DELETE | `/{id}`               | Delete a session                                                    |
| PATCH  | `/{id}/run`           | Start (from `Pending`) or resume (from `Paused`) a session          |
| PATCH  | `/{id}/pause`         | Pause a running session, rejected once the pause limit is reached  |
| PATCH  | `/{id}/finish`        | Finish a session, with an optional closing summary                 |
| GET    | `/day-summary?date=`  | Aggregated focus time and session list for a given date            |

All protected endpoints require an `Authorization: Bearer <accessToken>` header.

---

## Authentication Flow

1. `POST /api/auth/register` or `/login` returns an `accessToken` and a `refreshToken`.
2. The `accessToken` is sent as a Bearer token on subsequent requests.
3. On expiry, `POST /api/auth/refreshToken` exchanges the `refreshToken` for a new token pair.
4. `POST /api/auth/revokeToken` invalidates a refresh token (for example, on logout).

Passwords are hashed with BCrypt. Access tokens are signed JWTs validated on issuer, audience, lifetime, and signing key at request time; the application itself refuses to start if the configured signing key is not at least 256 bits.

---

## Error Handling

All unhandled and validation exceptions are caught by a global middleware and translated into a consistent JSON error shape:

```json
{
  "status": 400,
  "message": "Email is already registered.",
  "timestamp": "2026-09-25T12:00:00Z"
}
```

| Exception type              | Status Code |
|-------------------------------|:------------:|
| `FluentValidation.ValidationException` | 400 |
| `KeyNotFoundException`        | 404          |
| `ArgumentException`           | 400          |
| `InvalidOperationException`   | 400          |
| Unhandled                     | 500          |

---

## Rate Limiting

| Policy           | Applies to      | Limit                                                    |
|-------------------|-----------------|-------------------------------------------------------------|
| `AuthPolicy`      | `/api/auth/*`   | 5 requests per minute, partitioned by client IP             |
| `StandardPolicy`  | All other endpoints | 10 requests per 10 seconds, partitioned by authenticated user id (falls back to IP for anonymous requests) |

Requests exceeding a policy receive `429 Too Many Requests`.

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- MySQL Server 8.0

### 1. Clone the repository

```bash
git clone https://github.com/Abdullah-Yousry/FocusTracker.git
cd FocusTracker
```

### 2. Configure `appsettings.json`

Set the database connection string and JWT options in `TaskManager.Api/appsettings.json` (or via environment variables / `dotnet user-secrets` for anything beyond local development):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=TaskManagerDb;User=root;Password=yourpassword;"
  },
  "Jwt": {
    "Issuer": "TaskManagerApi",
    "Audience": "TaskManagerUsers",
    "ExpiryMinutes": 240,
    "SigningKey": ""
  }
}
```

`SigningKey` must be at least 32 characters (256 bits); the application throws on startup otherwise and the value is intentionally left blank in source control. `ExpiryMinutes: 240` is a relaxed value intended for local testing and should be reduced for production use.

### 3. Apply database migrations

```bash
dotnet ef database update --project TaskManager.Infrastructure --startup-project TaskManager.Api
```

### 4. Run the API

```bash
dotnet run --project TaskManager.Api
```

Swagger UI is available at `https://localhost:<port>/swagger` when running in the Development environment.

---

## Project Structure

```
TaskManager.Api/
    Controllers/            AuthController, CategoriesController, SessionsController
    Filters/                 ValidationFilter (runs FluentValidation automatically)
    Middlewares/              ExceptionHandlingMiddleware
    Program.cs                 Application bootstrap: DI, JWT, rate limiting, Serilog

TaskManager.Application/
    DTOs/
    Interfaces/                 IAuthService, ICategoryService, ISessionService
    Options/                     JwtOptions
    Validators/                   FluentValidation rules

TaskManager.Domain/
    Entities/                      User, Category, Session, RefreshToken
    Enums/                          Status, UserRole

TaskManager.Infrastructure/
    Data/                            AppDbContext
    Migrations/
    Services/                         AuthService, CategoryService, SessionService
```

---

## Contributing

Contributions are welcome. Please open an issue to discuss a proposed change, or submit a pull request directly:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes
4. Push the branch and open a pull request
