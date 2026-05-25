# Sportico Platform — Backend API

Sportico là nền tảng kết nối giữa **Coach (huấn luyện viên thể thao)** và **Learner (người học)**, hỗ trợ đăng bài, theo dõi, nhắn tin, đặt gói tập, đánh giá và thanh toán.

Repo này chứa toàn bộ **Backend API** được xây dựng bằng **ASP.NET Core 8 (Web API)** theo kiến trúc **Clean Architecture**, sử dụng **PostgreSQL** làm cơ sở dữ liệu chính và được triển khai (deploy) trên **Azure App Service** thông qua **GitHub Actions CI/CD**.

---

## Mục lục

1. [Kiến trúc tổng quan — Clean Architecture](#1-kiến-trúc-tổng-quan--clean-architecture)
2. [Cấu trúc thư mục](#2-cấu-trúc-thư-mục)
3. [Request Pipeline (vòng đời của một request)](#3-request-pipeline-vòng-đời-của-một-request)
4. [Read vs Write — Quy ước `AsNoTracking`](#4-read-vs-write--quy-ước-asnotracking)
5. [Pagination — Phân trang chuẩn](#5-pagination--phân-trang-chuẩn)
6. [Cơ sở dữ liệu — PostgreSQL trên Render](#6-cơ-sở-dữ-liệu--postgresql-trên-render)
7. [CI/CD — Deploy Azure App Service (Student Account)](#7-cicd--deploy-azure-app-service-student-account)
8. [Chạy dự án ở local](#8-chạy-dự-án-ở-local)
9. [Quy ước code & đóng góp](#9-quy-ước-code--đóng-góp)

---

## 1. Kiến trúc tổng quan — Clean Architecture

Dự án được tổ chức theo **Clean Architecture**, mục tiêu là tách biệt **business logic** ra khỏi **framework, database và các dịch vụ bên ngoài**. Nhờ đó:

- Tầng trong (Core, Application) **không phụ thuộc** vào tầng ngoài (Infrastructure, Api).
- Có thể thay PostgreSQL bằng SQL Server, thay SMTP bằng SendGrid… mà **không phải sửa logic nghiệp vụ**.
- Dễ viết unit test cho Application vì không cần dựng database.

### Sơ đồ phụ thuộc

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

> Quy tắc bất biến: **mũi tên luôn hướng vào trong**. Core không biết Application, Application không biết Infrastructure.

### Vai trò từng tầng

| Tầng | Trách nhiệm | Ví dụ |
|---|---|---|
| **SporticoApp.Core** | Entities, Enums, các quy tắc nghiệp vụ thuần | `User`, `Post`, `CoachProfile`, `Role` |
| **SporticoApp.Application** | Use cases, định nghĩa Interfaces (Repository, Service), DTOs, Validation | `AuthService`, `IUserRepository`, `LoginRequest` |
| **SporticoApp.Infrastructure** | Cài đặt các interface ở tầng Application: EF Core, JWT, Email, Repository | `AppDbContext`, `UserRepository`, `JwtService`, `EmailService` |
| **SporticoApp.Api** | Cổng vào HTTP: Controllers, Middlewares, cấu hình Swagger, DI | `AuthController`, `ExceptionMiddleware`, `Program.cs` |
| **SporticoApp.Shared** | Thành phần dùng chung xuyên tầng | `Result<T>`, `AppException`, `ErrorType`, `PagedResult<T>` |

Dependency Injection được khai báo ở mỗi tầng qua file `DependencyInjection.cs` rồi được gọi lên trong `Program.cs`:

```csharp
builder.Services.AddApplicationDI();
builder.Services.AddInfrastructureDI(builder.Configuration);
```

---

## 2. Cấu trúc thư mục

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
│   │   ├── Program.cs                  # Cấu hình pipeline + DI
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
    └── main_sportico-api-khoi.yml      # CI/CD lên Azure App Service
```

---

## 3. Request Pipeline (vòng đời của một request)

ASP.NET Core xử lý request theo cơ chế **middleware pipeline** — mỗi request đi qua một chuỗi middleware theo đúng thứ tự được đăng ký trong `Program.cs`. Thứ tự **rất quan trọng**, sai thứ tự là sai luôn hành vi (ví dụ: gọi `UseAuthorization` trước `UseAuthentication` sẽ làm policy không hoạt động).

### Pipeline hiện tại của Sportico

```
HTTP Request
   │
   ▼
[1] UseSwagger / UseSwaggerUI           ← Bật Swagger ở MỌI môi trường để xem trên Azure
   │
   ▼
[2] (Development) UseHttpsRedirection   ← Chỉ bật ở local
   │
   ▼
[3] UseHttpsRedirection                 ← Bắt buộc HTTPS
   │
   ▼
[4] UseAuthorization                    ← Kiểm tra quyền truy cập
   │
   ▼
[5] UseMiddleware<ExceptionMiddleware>  ← Bắt mọi exception, trả Result<T> chuẩn
   │
   ▼
[6] MapControllers                      ← Routing vào Controller tương ứng
   │
   ▼
Controller → Service (Application) → Repository (Infrastructure) → DbContext → PostgreSQL
   │
   ▼
Response (JSON, camelCase, enum dạng string)
```

### Các điểm quan trọng

**a. Cấu hình JSON toàn cục**

```csharp
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

→ Đảm bảo mọi response trả về client đều ở dạng `camelCase`, enum hiển thị tên chuỗi thay vì số.

**b. ExceptionMiddleware — Xử lý lỗi tập trung**

Mọi exception ở các tầng dưới đều được middleware này bắt lại và trả về định dạng `Result<T>` thống nhất:

```json
{
  "isSuccess": false,
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "Người dùng không tồn tại",
    "type": "NotFound",
    "details": null
  }
}
```

Service / Repository **chỉ cần** `throw new AppException(...)` với `ErrorType` phù hợp — middleware tự ánh xạ sang HTTP status code:

| ErrorType | HTTP Status |
|---|---|
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` (mặc định) | 500 |

**c. Swagger luôn bật**

Khác với template mặc định (chỉ bật Swagger ở Development), dự án này **bật Swagger ở mọi môi trường**, đồng thời `RoutePrefix = string.Empty` để truy cập trực tiếp tại root URL của Azure App Service:

```
https://sportico-api-khoi.azurewebsites.net/
```

**d. Load `.env` ở local**

`LoadEnvIfPresent()` sẽ leo lên tối đa 5 cấp thư mục tìm file `.env` và load vào biến môi trường — tiện cho dev local mà không cần sửa `appsettings.Development.json`. Trên Azure, biến môi trường được cấu hình qua **App Settings**.

---

## 4. Read vs Write — Quy ước `AsNoTracking`

### Vấn đề

EF Core mặc định **track** mọi entity được load lên: nó giữ snapshot để phát hiện thay đổi khi `SaveChanges()`. Cơ chế này **tốn RAM và CPU** một cách vô ích nếu bạn chỉ đọc dữ liệu để trả về client mà không định cập nhật.

### Quy ước trong Sportico

| Loại thao tác | Cách viết query | Lý do |
|---|---|---|
| **Read-only** (GET, list, detail, search…) | **PHẢI** dùng `.AsNoTracking()` | Nhanh hơn, ít tốn RAM, không cần change tracking |
| **Write** (Create / Update / Delete) | Dùng query tracking (mặc định) | EF Core cần track để phát hiện change |

### Ví dụ

```csharp
// ✅ Read-only — dùng AsNoTracking
public async Task<User?> GetByEmailAsync(string email)
{
    return await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email);
}

// ✅ Write — KHÔNG dùng AsNoTracking
public async Task UpdateAsync(User user)
{
    _context.Users.Update(user);
    await _context.SaveChangesAsync();
}
```

> **Lưu ý**: Khi bạn dùng `.AsNoTracking()` rồi muốn `Update` lại entity đó, phải `Attach` lại hoặc dùng `Update()` thủ công. Vì vậy: query nào dùng để **trả về cho client** thì `AsNoTracking`; query nào dùng để **sửa rồi save** thì giữ nguyên.

Quy ước này áp dụng cho **mọi Repository mới**. Khi review PR, đây là một trong những checklist bắt buộc.

---

## 5. Pagination — Phân trang chuẩn

Bất kỳ endpoint nào trả về **danh sách** đều **bắt buộc** hỗ trợ phân trang. Không bao giờ trả nguyên một `List<T>` không giới hạn về client.

### Request format

```
GET /api/posts?pageNumber=1&pageSize=20
```

| Tham số | Mặc định | Giới hạn |
|---|---|---|
| `pageNumber` | 1 | >= 1 |
| `pageSize` | 10 | 1 — 100 (chặn ở Application layer) |

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

### Cách implement chuẩn ở Repository

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

### Nguyên tắc

1. **Luôn `OrderBy` trước khi `Skip/Take`** — nếu không, thứ tự không deterministic và phân trang sẽ sai.
2. **`AsNoTracking` bắt buộc** (theo quy ước Read ở mục 4).
3. **Count + Items** chạy trong 2 query, không gộp vào client.
4. **Validate `pageSize` ở Application layer** để client không thể yêu cầu `pageSize=100000` gây quá tải DB.

---

## 6. Cơ sở dữ liệu — PostgreSQL trên Render

Database của dự án được host trên **Render** (gói PostgreSQL miễn phí dành cho student / hobby project).

### Vì sao chọn Render?

- Miễn phí với hạn mức đủ cho giai đoạn EXE202.
- Cung cấp **External Connection String** dùng được từ bất kỳ đâu (local dev, Azure App Service…).
- Quản lý qua dashboard, có sẵn backup tự động ở plan trả phí.

### Kết nối từ ứng dụng

Connection string đặt trong biến môi trường (hoặc User Secrets ở local) với key:

```
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...;SslMode=Require;Trust Server Certificate=true
```

EF Core được cấu hình dùng **Npgsql** trong [DependencyInjection.cs](src/SporticoApp.Infrastructure/DependencyInjection.cs):

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Default")));
```

### Quy ước đặt tên (Snake Case)

PostgreSQL theo convention là `snake_case`, nhưng C# entity là `PascalCase`. [AppDbContext.cs](src/SporticoApp.Infrastructure/Persistence/AppDbContext.cs) có hàm `ApplySnakeCaseNames()` tự động convert:

- `User` → bảng `user`
- `CoachProfile.CreatedAt` → cột `created_at`

Đồng thời bật sẵn các extension PostgreSQL hữu ích:

```csharp
modelBuilder.HasPostgresExtension("citext");    // Case-insensitive text (email)
modelBuilder.HasPostgresExtension("pg_trgm");   // Fuzzy search
modelBuilder.HasPostgresExtension("pgcrypto");  // Mã hóa, UUID
```

### Migrations

```bash
# Tạo migration mới
dotnet ef migrations add <TenMigration> --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api

# Apply lên database
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

> **Cẩn thận**: Render free tier có thể bị **idle disconnect**. Connection đầu tiên sau khi idle có thể chậm hơn — đây là behavior bình thường, không phải bug.

---

## 7. CI/CD — Deploy Azure App Service (Student Account)

Backend được deploy lên **Azure App Service** với tài khoản **Azure for Students** (free credit $100, không yêu cầu thẻ tín dụng).

### Tài nguyên Azure

- **App Service**: `sportico-api-khoi`
- **Plan**: Free F1 (đủ cho EXE202, có giới hạn về CPU minutes/ngày)
- **Region**: gần Việt Nam nhất (Southeast Asia)
- **URL Production**: `https://sportico-api-khoi.azurewebsites.net/`

### Workflow GitHub Actions

File [.github/workflows/main_sportico-api-khoi.yml](.github/workflows/main_sportico-api-khoi.yml) tự động chạy mỗi khi có push lên branch `main`:

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
    - Login Azure (OIDC qua federated credentials)
    - azure/webapps-deploy@v3 → push artifact lên App Service
```

### Xác thực — OIDC (không dùng publish profile)

Workflow dùng **OpenID Connect** thay vì publish profile, an toàn hơn vì không cần lưu password:

```yaml
permissions:
  id-token: write
  contents: read
```

Ba secret cần khai báo trong **Repo Settings → Secrets and variables → Actions**:

| Secret | Lấy từ đâu |
|---|---|
| `AZURE_CLIENT_ID` | App registration trong Azure AD |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Subscription Azure for Students |

### Cấu hình biến môi trường trên Azure

Vào **App Service → Settings → Environment variables (App settings)**, thêm các key:

```
ConnectionStrings__Default     = <Render Postgres connection string>
JWT__SecretKey                 = ...
JWT__Issuer                    = ...
JWT__Audience                  = ...
EmailSettings__Password        = ...
```

> Azure tự động map `__` (double underscore) thành dấu `:` của `IConfiguration` — không cần sửa code.

### Sau khi deploy

- Truy cập `https://sportico-api-khoi.azurewebsites.net/` để vào Swagger UI.
- Xem log realtime: **App Service → Log stream**.
- Restart app nếu cần: **Overview → Restart**.

---

## 8. Chạy dự án ở local

### Yêu cầu

- .NET SDK **8.0** trở lên
- PostgreSQL (local) hoặc dùng luôn connection string Render
- IDE: Visual Studio 2022 / Rider / VS Code

### Setup nhanh

1. Clone repo:
   ```bash
   git clone <repo-url>
   cd sportico-platform
   ```

2. Tạo file `.env` ở root (sẽ được `LoadEnvIfPresent()` tự load):
   ```env
   ConnectionStrings__Default=Host=...;Port=5432;Database=sportico;Username=...;Password=...
   JWT__SecretKey=your-super-secret-min-32-chars
   JWT__Issuer=Sportico
   JWT__Audience=SporticoClient
   EmailSettings__Password=app-password-gmail
   ```

3. Restore + chạy migration:
   ```bash
   dotnet restore
   dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
   ```

4. Chạy API:
   ```bash
   dotnet run --project src/SporticoApp.Api
   ```

5. Mở Swagger: `https://localhost:5001/` (cổng tùy theo `launchSettings.json`).

---

## 9. Quy ước code & đóng góp

- **Naming**: PascalCase cho C#, snake_case cho cột DB (tự động convert).
- **Async**: mọi method I/O đều `async/await`, hậu tố `Async`.
- **Repository read-only**: bắt buộc `AsNoTracking()`.
- **List endpoint**: bắt buộc pagination, trả `PagedResult<T>`.
- **Exception**: throw `AppException` với `ErrorType` phù hợp — không trả `BadRequest()` thủ công.
- **Response**: luôn bọc trong `Result<T>` để client có format thống nhất.
- **Commit message**: ngắn gọn, dùng prefix `feat:`, `fix:`, `refactor:`, `chore:`.

### Pull Request checklist

- [ ] Query read-only đã dùng `AsNoTracking`?
- [ ] Endpoint trả list đã có pagination?
- [ ] Đã `throw AppException` đúng `ErrorType`?
- [ ] Đã chạy được local + không break Swagger?
- [ ] Đã update migration nếu có sửa entity?

---

## Liên hệ

Dự án thuộc môn **EXE202 — FPT University**.
Mọi câu hỏi vui lòng tạo issue hoặc liên hệ team Sportico.
