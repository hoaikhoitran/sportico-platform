# 02 — Architecture

Sportico follows **Clean Architecture**: business logic is isolated from frameworks, the database, and external services. Dependencies always point inward.

## Solution Structure

```
sportico-platform/
├── SporticoApp.Api.sln
├── SporticoApp.Shared/                 # Cross-cutting building blocks
└── src/
    ├── SporticoApp.Api/                # HTTP entry point
    ├── SporticoApp.Application/        # Use cases / services / interfaces / DTOs / validators
    ├── SporticoApp.Core/               # Domain entities + enums
    └── SporticoApp.Infrastructure/     # EF Core, repositories, JWT, email, PayOS
```

| Project | Responsibility | Examples |
|---|---|---|
| **SporticoApp.Core** | Domain entities and enums. No framework dependencies. | `User`, `Booking`, `TrainingPackage`, `CoachWallet` |
| **SporticoApp.Application** | Use cases (services), repository/service interfaces, DTOs, FluentValidation validators, mapping extensions. | `BookingService`, `IBookingRepository`, `CreateTrainingPackageRequest` |
| **SporticoApp.Infrastructure** | Implementations of Application interfaces: `AppDbContext`, EF configurations, repositories, `JwtService`, `EmailService`, `PayOsService`. | `BookingRepository`, `PayOsService` |
| **SporticoApp.Api** | Controllers, middleware, Swagger, DI composition, JWT auth setup. | `BookingsController`, `ExceptionMiddleware`, `Program.cs` |
| **SporticoApp.Shared** | Result/error envelopes, exceptions, constants, helpers. | `Result<T>`, `PagedResult<T>`, `AppException`, `ErrorCodes` |

## Dependency Direction

```
Api ──▶ Application ──▶ Core
                ▲
Infrastructure ─┘  (implements Application interfaces; references Core)
```

- `Core` depends on nothing.
- `Application` depends on `Core` and `Shared`.
- `Infrastructure` depends on `Application` (to implement its interfaces) and `Core`.
- `Api` depends on `Application` and `Infrastructure` only to compose DI in `Program.cs`.

## Request Flow

```
HTTP Request
  → Controller (SporticoApp.Api)         thin: reads claims, calls a service
  → Service (SporticoApp.Application)     business logic, validation, orchestration
  → Repository (SporticoApp.Infrastructure)  persistence via EF Core
  → AppDbContext → PostgreSQL
  → Result<T> / PagedResult<T> back up the chain
  → JSON response (camelCase, enums as strings)
```

## Layer Responsibilities (rules)

- **Controllers stay thin** — extract the user id from claims (`User.GetUserId()`), call one service method, return `Ok(result)`. No business logic.
- **Services own business logic** — validation, authorization checks, status transitions, money math, notification creation.
- **Repositories own persistence** — EF Core queries. Read queries should use `AsNoTracking()`; write queries keep tracking. Several repositories expose `...ForUpdateAsync` (tracked) and `AddWithoutSaveAsync` / `SaveChangesAsync` so a service can compose a single transaction.
- **Core owns entities** — POCO entities with navigation properties; no persistence concerns.
- **Shared owns envelopes/constants/errors** — `Result<T>`, `Error`, `ErrorCodes`, status constants.

## DI Registration

Each layer ships a `DependencyInjection.cs` extension, composed in [Program.cs](../src/SporticoApp.Api/Program.cs):

```csharp
builder.Services.AddApplicationDI();                    // services + validators
builder.Services.AddInfrastructureDI(builder.Configuration); // DbContext, repositories, JWT, email, PayOS
```

- **Application** ([DependencyInjection.cs](../src/SporticoApp.Application/DependencyInjection.cs)) registers all `I*Service` → `*Service` as scoped, and registers FluentValidation validators from the assembly.
- **Infrastructure** ([DependencyInjection.cs](../src/SporticoApp.Infrastructure/DependencyInjection.cs)) registers `AppDbContext` (Npgsql), all `I*Repository` → `*Repository` as scoped, `JwtService`, `RefreshTokenService`, `EmailService`, `SlugGenerator` (singleton), and a typed `HttpClient` for `PayOsService`. It also binds `EmailSettings` and `PayOsSettings` from configuration.

## Validation

Validation uses two mechanisms:

1. **Data annotations + automatic model state** — `Program.cs` overrides `InvalidModelStateResponseFactory` so model-binding/annotation failures return a `Result<object>` failure with `ErrorCodes.ValidationError` and the messages in `error.details`. Used by e.g. `LoginRequest`, `RegisterRequest`.
2. **FluentValidation** — most services inject `IValidator<TRequest>` and call `ValidateAsync` at the top of the method. On failure they throw a `ValidationException` (`ErrorType.Validation`) with the messages as `details`.

Both paths produce the same response envelope shape.

## Error Handling

`ExceptionMiddleware` ([source](../src/SporticoApp.Api/Middlewares/ExceptionMiddleware.cs)) wraps the pipeline:

- Catches `AppException` subclasses and maps `ErrorType` → HTTP status, returning a `Result<object>` failure:

  | ErrorType | HTTP |
  |---|---|
  | `Validation` | 400 |
  | `Unauthorized` | 401 |
  | `Forbidden` | 403 |
  | `NotFound` | 404 |
  | `Conflict` | 409 |
  | `Failure` (default) | 500 |

- Catches any other exception → HTTP 500 with `ErrorCodes.InternalServerError`. The message/stack trace is only included in `details` when running in the Development environment.

Exception types live in `SporticoApp.Shared.Exceptions`: `ValidationException`, `UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `ConflictException`, `FailureException` — all derive from `AppException` and carry a `Code`, `Message`, `ErrorType`, and optional `Details`.

## Response Envelopes

All endpoints return one of:

- `Result<T>` — `{ isSuccess, data, error }`. `error` is `null` on success; `data` is `null` on failure.
- `Result` (non-generic) — `{ isSuccess, message }`. Used by register/verify-email.
- `PagedResult<T>` (always wrapped inside `Result<PagedResult<T>>`) — `{ items, pageNumber, pageSize, totalCount, totalPages, hasPrevious, hasNext }`.

See [frontend/api-contracts.md](frontend/api-contracts.md) for exact shapes.

## Mapping

Each module has a `*MappingExtensions` class in `SporticoApp.Application/Mappings` providing `ToEntity(...)` and `ToResponse()` extension methods (hand-written, no AutoMapper). Services call these to convert between DTOs and entities.

> NOTE: A subtle EF Core pattern is used when building a `Booking` from a no-tracking `TrainingPackage`: the navigation property is **not** attached before `Add`, because EF would otherwise mark the existing package/sport as `Added` and emit spurious INSERTs. The FK id is set instead, and the navigation is assigned only after save for response mapping. See [16 — Troubleshooting](16-troubleshooting.md#duplicate-insert-from-no-tracking-navigation).
