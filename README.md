# Sportico Platform — Backend API

Sportico is a platform that connects **Coaches (sports trainers)** with **Learners**, supporting posts, follows, messaging, training packages, reviews, and payments.

This repository contains the entire **Backend API** built with **ASP.NET Core 8 (Web API)** following **Clean Architecture**, using **PostgreSQL** as the primary database and deployed to **Azure App Service** via **GitHub Actions CI/CD**.

---

## Table of Contents

1. [Architecture Overview — Clean Architecture](#1-architecture-overview--clean-architecture)
2. [Folder Structure](#2-folder-structure)
3. [Request Pipeline (the life of a request)](#3-request-pipeline-the-life-of-a-request)
4. [Read vs Write — The `AsNoTracking` Convention](#4-read-vs-write--the-asnotracking-convention)
5. [Pagination — Standard Paging](#5-pagination--standard-paging)
6. [Database — PostgreSQL on Render](#6-database--postgresql-on-render)
7. [CI/CD — Deploying to Azure App Service (Student Account)](#7-cicd--deploying-to-azure-app-service-student-account)
8. [Running the Project Locally](#8-running-the-project-locally)
9. [Code Conventions & Contributing](#9-code-conventions--contributing)

---

## 1. Architecture Overview — Clean Architecture

The project is organized following **Clean Architecture**. The goal is to isolate **business logic** from **frameworks, databases, and external services**. As a result:

- Inner layers (Core, Application) **do not depend on** outer layers (Infrastructure, Api).
- We can swap PostgreSQL for SQL Server, or SMTP for SendGrid, **without touching business logic**.
- Unit tests for the Application layer are easy to write because no real database is required.

### Dependency Diagram

```
            +-------------------+
            |   SporticoApp.Api |   <-- HTTP, Controllers, Middlewares
            +---------+---------+
                      |
                      v
            +-------------------+
            |   Application     |   <-- Use cases, Services, Interfaces, DTOs
            +---------+---------+
                      |
                      v
            +-------------------+
            |   Core (Domain)   |   <-- Entities, Enums, Business rules
            +-------------------+
                      ^
                      |
            +-------------------+
            |  Infrastructure   |   <-- EF Core, Repositories, Email, JWT
            +-------------------+
```

> The invariant: **arrows always point inward**. Core knows nothing about Application; Application knows nothing about Infrastructure.

### Responsibilities of Each Layer

| Layer | Responsibility | Examples |
|---|---|---|
| **SporticoApp.Core** | Entities, Enums, pure domain rules | `User`, `Post`, `CoachProfile`, `Role` |
| **SporticoApp.Application** | Use cases, interface definitions (Repository, Service), DTOs, validation | `AuthService`, `IUserRepository`, `LoginRequest` |
| **SporticoApp.Infrastructure** | Implementations of Application interfaces: EF Core, JWT, Email, Repositories | `AppDbContext`, `UserRepository`, `JwtService`, `EmailService` |
| **SporticoApp.Api** | HTTP entry point: Controllers, Middlewares, Swagger setup, DI wiring | `AuthController`, `ExceptionMiddleware`, `Program.cs` |
| **SporticoApp.Shared** | Cross-cutting building blocks | `Result<T>`, `AppException`, `ErrorType`, `PagedResult<T>` |

Dependency Injection is declared per layer in each layer's `DependencyInjection.cs` and composed in `Program.cs`:

```csharp
builder.Services.AddApplicationDI();
builder.Services.AddInfrastructureDI(builder.Configuration);
```

---

## 2. Folder Structure

```
sportico-platform/
├── SporticoApp.Api.sln
├── SporticoApp.Shared/                 # Constants, Enums, Exceptions, Helpers, Responses
│   ├── Exceptions/AppException.cs
│   └── Responses/Result.cs
├── src/
│   ├── SporticoApp.Api/                # Web API entry point
│   │   ├── Controllers/                # AuthController, ...
│   │   ├── Middlewares/                # ExceptionMiddleware
│   │   ├── Program.cs                  # Pipeline + DI configuration
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   ├── SporticoApp.Application/        # Use cases, Interfaces, DTOs
│   │   ├── DTOs/Auth/
│   │   ├── Interfaces/Repositories/
│   │   ├── Interfaces/Services/
│   │   ├── Services/AuthService.cs
│   │   └── DependencyInjection.cs
│   ├── SporticoApp.Core/               # Domain
│   │   ├── Entities/                   # User, Post, CoachProfile, ...
│   │   └── Enums/
│   └── SporticoApp.Infrastructure/     # EF Core + External services
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/         # IEntityTypeConfiguration<T>
│       │   ├── Context/
│       │   └── Repositories/
│       ├── Services/                   # JwtService, EmailService, RefreshTokenService
│       └── DependencyInjection.cs
└── .github/workflows/
    └── main_sportico-api-khoi.yml      # CI/CD to Azure App Service
```

---

## 3. Request Pipeline (the life of a request)

ASP.NET Core processes requests through a **middleware pipeline** — each request flows through a chain of middlewares in the exact order they are registered in `Program.cs`. **Order matters a lot**: getting it wrong silently breaks behavior (e.g. calling `UseAuthorization` before `UseAuthentication` makes policies ineffective).

### Sportico's Current Pipeline

```
HTTP Request
   │
   ▼
[1] UseSwagger / UseSwaggerUI           ← Enabled in ALL environments so it works on Azure too
   │
   ▼
[2] (Development) UseHttpsRedirection   ← Local only
   │
   ▼
[3] UseHttpsRedirection                 ← Force HTTPS
   │
   ▼
[4] UseAuthorization                    ← Access control checks
   │
   ▼
[5] UseMiddleware<ExceptionMiddleware>  ← Catches every exception, returns a standardized Result<T>
   │
   ▼
[6] MapControllers                      ← Routes the request to the matching Controller
   │
   ▼
Controller → Service (Application) → Repository (Infrastructure) → DbContext → PostgreSQL
   │
   ▼
Response (JSON, camelCase, enums as strings)
```

### Key Points

**a. Global JSON configuration**

```csharp
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

→ Guarantees every response is `camelCase` and enums are serialized as their string names instead of integer values.

**b. ExceptionMiddleware — Centralized Error Handling**

Exceptions raised anywhere downstream are caught by this middleware and returned in a consistent `Result<T>` envelope:

```json
{
  "isSuccess": false,
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "User does not exist",
    "type": "NotFound",
    "details": null
  }
}
```

Services / Repositories simply `throw new AppException(...)` with the appropriate `ErrorType` — the middleware maps it to the right HTTP status code:

| ErrorType | HTTP Status |
|---|---|
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` (default) | 500 |

**c. Swagger is always on**

Unlike the default template (which only enables Swagger in Development), this project **enables Swagger in every environment**, with `RoutePrefix = string.Empty` so it can be reached directly at the root URL of the Azure App Service:

```
https://sportico-api-khoi.azurewebsites.net/
```

**d. `.env` loading for local dev**

`LoadEnvIfPresent()` walks up to 5 parent directories looking for a `.env` file and loads it into environment variables — handy for local development without modifying `appsettings.Development.json`. On Azure, environment variables are configured via **App Settings**.

---

## 4. Read vs Write — The `AsNoTracking` Convention

### The Problem

By default, EF Core **tracks** every entity it loads: it stores a snapshot in order to detect changes during `SaveChanges()`. This mechanism **wastes RAM and CPU** when you only intend to read data and return it to the client.

### Convention Used in Sportico

| Operation | Query style | Why |
|---|---|---|
| **Read-only** (GET, list, detail, search…) | **MUST** use `.AsNoTracking()` | Faster, lower memory, no change tracking needed |
| **Write** (Create / Update / Delete) | Default tracking query | EF Core needs tracking to detect changes |

### Examples

```csharp
// ✅ Read-only — uses AsNoTracking
public async Task<User?> GetByEmailAsync(string email)
{
    return await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email);
}

// ✅ Write — do NOT use AsNoTracking
public async Task UpdateAsync(User user)
{
    _context.Users.Update(user);
    await _context.SaveChangesAsync();
}
```

> **Heads up**: If you load an entity with `.AsNoTracking()` and later want to `Update` it, you must `Attach` it manually or call `Update()` explicitly. Rule of thumb: queries that **return data to the client** use `AsNoTracking`; queries that **load an entity to modify and save** keep the default tracking behavior.

This rule applies to **every new Repository**. It is one of the mandatory checklist items during PR review.

---

## 5. Pagination — Standard Paging

Every endpoint that returns a **list** **must** support pagination. We never return an unbounded `List<T>` to the client.

### Request format

```
GET /api/posts?pageNumber=1&pageSize=20
```

| Parameter | Default | Limit |
|---|---|---|
| `pageNumber` | 1 | >= 1 |
| `pageSize` | 10 | 1 — 100 (enforced in the Application layer) |

### Response format

```json
{
  "isSuccess": true,
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 137,
    "totalPages": 7,
    "hasPrevious": false,
    "hasNext": true
  }
}
```

### Standard Repository Implementation

```csharp
public async Task<PagedResult<Post>> GetPagedAsync(int pageNumber, int pageSize)
{
    var query = _context.Posts
        .AsNoTracking()
        .OrderByDescending(p => p.CreatedAt);

    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<Post>(items, totalCount, pageNumber, pageSize);
}
```

### Rules

1. **Always `OrderBy` before `Skip/Take`** — without a deterministic ordering, paging results become unstable.
2. **`AsNoTracking` is mandatory** (see the Read convention in section 4).
3. **Count + Items run as two queries** — do not aggregate on the client side.
4. **Validate `pageSize` in the Application layer** so clients cannot request `pageSize=100000` and overload the database.

---

## 6. Database — PostgreSQL on Render

The project's database is hosted on **Render** (free PostgreSQL tier suitable for student / hobby projects).

### Why Render?

- Free, with a quota that fits the EXE202 timeline.
- Provides an **External Connection String** usable from anywhere (local dev, Azure App Service, etc.).
- Managed through a web dashboard; automatic backups available on paid plans.

### Connecting from the App

The connection string is stored as an environment variable (or in User Secrets for local dev) with the key:

```
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...;SslMode=Require;Trust Server Certificate=true
```

EF Core is configured to use **Npgsql** in [DependencyInjection.cs](src/SporticoApp.Infrastructure/DependencyInjection.cs):

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Default")));
```

### Naming Convention (Snake Case)

PostgreSQL conventionally uses `snake_case`, while C# entities use `PascalCase`. [AppDbContext.cs](src/SporticoApp.Infrastructure/Persistence/AppDbContext.cs) contains an `ApplySnakeCaseNames()` method that converts them automatically:

- `User` → table `user`
- `CoachProfile.CreatedAt` → column `created_at`

It also enables several useful PostgreSQL extensions out of the box:

```csharp
modelBuilder.HasPostgresExtension("citext");    // Case-insensitive text (emails)
modelBuilder.HasPostgresExtension("pg_trgm");   // Fuzzy text search
modelBuilder.HasPostgresExtension("pgcrypto");  // Encryption, UUIDs
```

### Migrations

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api

# Apply migrations to the database
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

> **Caveat**: The Render free tier may **disconnect when idle**. The first request after an idle period can be slow — that is expected behavior, not a bug.

---

## 7. CI/CD — Deploying to Azure App Service (Student Account)

The backend is deployed to **Azure App Service** using an **Azure for Students** account (free $100 credit, no credit card required).

### Azure Resources

- **App Service**: `sportico-api-khoi`
- **Plan**: Free F1 (enough for EXE202, with daily CPU-minute limits)
- **Region**: closest to Vietnam (Southeast Asia)
- **Production URL**: `https://sportico-api-khoi.azurewebsites.net/`

### GitHub Actions Workflow

The file [.github/workflows/main_sportico-api-khoi.yml](.github/workflows/main_sportico-api-khoi.yml) runs automatically on every push to the `main` branch:

```yaml
on:
  push:
    branches: [ main ]

jobs:
  build:
    - Checkout code
    - Setup .NET 8
    - dotnet restore SporticoApp.Api.sln
    - dotnet build -c Release
    - dotnet publish -c Release -o myapp
    - Upload artifact

  deploy:
    needs: build
    - Download artifact
    - Login to Azure (OIDC via federated credentials)
    - azure/webapps-deploy@v3 → push the artifact to App Service
```

### Authentication — OIDC (no publish profile)

The workflow uses **OpenID Connect** instead of a publish profile, which is safer because no password is ever stored:

```yaml
permissions:
  id-token: write
  contents: read
```

Three secrets must be declared under **Repo Settings → Secrets and variables → Actions**:

| Secret | Where it comes from |
|---|---|
| `AZURE_CLIENT_ID` | App registration in Azure AD |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure for Students subscription |

### Configuring Environment Variables on Azure

Go to **App Service → Settings → Environment variables (App settings)** and add:

```
ConnectionStrings__Default     = <Render Postgres connection string>
JWT__SecretKey                 = ...
JWT__Issuer                    = ...
JWT__Audience                  = ...
EmailSettings__Password        = ...
```

> Azure automatically maps `__` (double underscore) to the `:` separator used by `IConfiguration` — no code changes required.

### After Deploying

- Visit `https://sportico-api-khoi.azurewebsites.net/` to open Swagger UI.
- Stream logs in real time: **App Service → Log stream**.
- Restart the app if needed: **Overview → Restart**.

---

## 8. Running the Project Locally

### Requirements

- .NET SDK **8.0** or later
- PostgreSQL (local) or just reuse the Render connection string
- IDE: Visual Studio 2022 / Rider / VS Code

### Quick Setup

1. Clone the repo:
   ```bash
   git clone <repo-url>
   cd sportico-platform
   ```

2. Create a `.env` file in the repo root (it will be loaded automatically by `LoadEnvIfPresent()`):
   ```env
   ConnectionStrings__Default=Host=...;Port=5432;Database=sportico;Username=...;Password=...
   JWT__SecretKey=your-super-secret-min-32-chars
   JWT__Issuer=Sportico
   JWT__Audience=SporticoClient
   EmailSettings__Password=app-password-gmail
   ```

3. Restore dependencies and apply migrations:
   ```bash
   dotnet restore
   dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
   ```

4. Run the API:
   ```bash
   dotnet run --project src/SporticoApp.Api
   ```

5. Open Swagger: `https://localhost:5001/` (port depends on `launchSettings.json`).

---

## 9. Code Conventions & Contributing

- **Naming**: PascalCase for C#, snake_case for DB columns (converted automatically).
- **Async**: every I/O method is `async/await`, suffixed with `Async`.
- **Read-only repositories**: must use `AsNoTracking()`.
- **List endpoints**: must use pagination and return `PagedResult<T>`.
- **Exceptions**: throw `AppException` with the proper `ErrorType` — do not return `BadRequest()` manually.
- **Responses**: always wrap in `Result<T>` so the client sees a uniform shape.
- **Commit messages**: short, prefixed with `feat:`, `fix:`, `refactor:`, `chore:`.

### Pull Request Checklist

- [ ] Are read-only queries using `AsNoTracking`?
- [ ] Do list endpoints implement pagination?
- [ ] Are `AppException`s thrown with the correct `ErrorType`?
- [ ] Does the app run locally and is Swagger still working?
- [ ] If entities changed, is the migration updated?

---

## Contact

This project is part of **EXE202 — FPT University**.
For questions please open an issue or reach out to the Sportico team.
