# Bàn giao API cho Frontend — Voucher, Community Post, Admin Moderation, User-to-User Chat, User Block

> **Nguồn sự thật:** tài liệu này được viết bằng cách đọc trực tiếp source code trong working tree
> (controllers, DTOs, FluentValidation, constants, `ErrorCodes`, services, EF configurations) và
> đối chiếu với **Swagger/OpenAPI thật** sinh ra từ API đang chạy (`GET /swagger/v1/swagger.json`).
> Mọi field, enum, route, HTTP status trong tài liệu đều đã được xác minh. Nếu tài liệu cũ hoặc báo
> cáo trước đó mâu thuẫn với nội dung ở đây, **nội dung ở đây đúng**.
>
> Ngày đối chiếu: 2026-08-04. Build: `dotnet build SporticoApp.Api.sln` → **0 Warning, 0 Error**.

---

## Mục lục

1. [Mục đích tài liệu](#1-mục-đích-tài-liệu)
2. [Tổng quan kiến trúc frontend](#2-tổng-quan-kiến-trúc-frontend)
3. [Quy ước API chung](#3-quy-ước-api-chung)
4. [Authentication và authorization](#4-authentication-và-authorization)
5. [Shared response schemas](#5-shared-response-schemas)
6. [Voucher integration](#6-voucher-integration)
7. [Booking purchase với voucher](#7-booking-purchase-với-voucher)
8. [Admin voucher management](#8-admin-voucher-management)
9. [Community feed](#9-community-feed)
10. [Community post detail](#10-community-post-detail)
11. [Create/update community post](#11-createupdate-community-post)
12. [Media handling](#12-media-handling)
13. [Comment và reply](#13-comment-và-reply)
14. [Like/reaction](#14-likereaction)
15. [Community applications](#15-community-applications)
16. [Admin community management](#16-admin-community-management)
17. [Report và moderation](#17-report-và-moderation)
18. [User-to-user chat](#18-user-to-user-chat)
19. [Message requests](#19-message-requests)
20. [Message attachments](#20-message-attachments)
21. [User block](#21-user-block)
22. [Notifications](#22-notifications)
23. [Realtime/SignalR](#23-realtimesignalr)
24. [State machines](#24-state-machines)
25. [Error handling matrix](#25-error-handling-matrix)
26. [Frontend route proposal](#26-frontend-route-proposal)
27. [Component proposal](#27-component-proposal)
28. [API service/type proposal](#28-api-servicetype-proposal)
29. [Cache invalidation](#29-cache-invalidation)
30. [Optimistic UI](#30-optimistic-ui)
31. [Form validation](#31-form-validation)
32. [Empty/loading/error states](#32-emptyloadingerror-states)
33. [End-to-end user flows](#33-end-to-end-user-flows)
34. [Testing checklist](#34-testing-checklist)
35. [Known limitations](#35-known-limitations)
36. [Exact TypeScript interfaces](#36-exact-typescript-interfaces)

---

## 1. Mục đích tài liệu

Tài liệu này mô tả **toàn bộ contract HTTP** của 5 nhóm tính năng mới, đủ để một frontend agent
triển khai giao diện mà không cần hỏi lại backend:

| Nhóm | Nội dung |
|---|---|
| **Voucher** | Learner nhập mã giảm giá khi mua `TrainingPackage`; admin quản lý chiến dịch voucher |
| **Community Post** | Diễn đàn + tuyển người chơi vãng lai: bài viết, media, comment, reply, like, xin tham gia |
| **Admin Community** | Ẩn/khôi phục/xóa mềm bài + comment, xem báo cáo vi phạm, xử lý báo cáo |
| **User-to-user Chat** | Mọi user active nhắn tin với nhau (không chỉ coach), có cơ chế chat request |
| **User Block** | Chặn/bỏ chặn user, ảnh hưởng đến chat |

### Ba loại thông tin trong tài liệu

Tài liệu phân biệt rõ ràng 3 loại nội dung. Đừng nhầm lẫn giữa chúng:

| Nhãn | Ý nghĩa |
|---|---|
| **Backend contract** | Sự thật đã xác minh trong code. Frontend phải tuân theo chính xác. |
| **Frontend recommendation** | Gợi ý triển khai (route, component, query key, Zod schema). Có thể thay đổi tùy ý. |
| **Known limitation** | Backend hiện **chưa** làm điều gì đó. Frontend phải xử lý hoặc chấp nhận. |

---

## 2. Tổng quan kiến trúc frontend

**Frontend recommendation** — toàn bộ mục này là gợi ý, không phải contract.

```
Next.js App Router
 ├── Server Components  → fetch feed công khai (GET /api/community/posts) để SEO
 ├── Client Components  → mọi thứ cần token, mutation, realtime-ish polling
 └── TanStack Query     → cache + invalidation (xem mục 29)
```

Nguyên tắc cần nhớ khi thiết kế:

1. **Feed community là public** (`[AllowAnonymous]`) → có thể render server-side không cần token.
   Nhưng khi có token, response bổ sung các field viewer-relative (`currentUserReacted`, `canApply`…),
   nên trang detail nên render client-side hoặc re-fetch sau khi có token.
2. **Không có realtime.** Chat và notification phải polling/refetch (xem mục 23).
3. **Không có upload endpoint.** Backend chỉ nhận URL HTTPS (xem mục 12).
4. **Mọi list lớn đều phân trang** theo `PagedResult<T>`, **trừ** `GET /api/chat/rooms` và
   `GET /api/users/me/blocked` (trả mảng phẳng).

---

## 3. Quy ước API chung

**Backend contract.**

### 3.1 Base URL và routing

| Mục | Giá trị |
|---|---|
| Base URL | Cấu hình qua env (`NEXT_PUBLIC_API_BASE_URL`), ví dụ `https://api.sportico.example` |
| Prefix | Mọi endpoint bắt đầu bằng `/api` |
| Swagger UI | `GET /swagger` (bật ở cả Development và Production trong `Program.cs`) |
| OpenAPI JSON | `GET /swagger/v1/swagger.json` |

### 3.2 Headers

```http
Authorization: Bearer <access_token>
Content-Type: application/json
Accept: application/json
```

- `Content-Type: application/json` bắt buộc với mọi request có body.
- **Không có endpoint nào nhận `multipart/form-data`** trong phạm vi tài liệu này.

### 3.3 JSON naming

- **Response body: `camelCase`.** Cấu hình bằng `JsonNamingPolicy.CamelCase` trong `Program.cs`
  (`AddControllers().AddJsonOptions(...)`) và cả trong `ExceptionMiddleware`.
- **Request body: `camelCase`.** System.Text.Json deserialize **case-insensitive** theo mặc định của
  ASP.NET Core, nên `PascalCase` cũng chạy — nhưng hãy dùng `camelCase` cho nhất quán.
- **Query string:** Swagger sinh ra tên **`PascalCase`** (ví dụ `PostType`, `PageNumber`) vì binder
  đọc trực tiếp tên property C#. Model binding của ASP.NET Core **không phân biệt hoa thường**, nên
  `?postType=discussion&pageNumber=1` hoạt động hoàn toàn bình thường.
  → **Khuyến nghị: dùng `camelCase` cho query string.**

### 3.4 Kiểu dữ liệu

| Kiểu C# | JSON | Ghi chú |
|---|---|---|
| `Guid` | `string` | UUID v4 dạng `"9f420548-9b36-49d8-a99f-cb3fb0051135"` |
| `DateTime` | `string` | ISO 8601. Backend **luôn ghi UTC** (`DateTime.UtcNow`). |
| `decimal` | `number` | Tiền VND. Swagger khai `format: double`. |
| `long` | `number` | `orderCode` là int64 — an toàn trong `Number` của JS (< 2^53). |
| `int?` | `number \| null` | |
| `enum ErrorType` | `string` | `JsonStringEnumConverter` → `"Validation"`, `"Conflict"`… (PascalCase) |

**Timezone — quan trọng:**
- Backend lưu và trả **UTC**. Chuỗi thời gian có thể **không có hậu tố `Z`** (ví dụ
  `"2026-08-07T12:00:00"`) tùy cột Postgres (`timestamp without time zone`).
- **Frontend phải tự coi mọi `DateTime` là UTC** và convert sang `Asia/Ho_Chi_Minh` (UTC+7) để hiển thị.
- Khi gửi lên, hãy gửi ISO 8601 UTC có `Z`: `"2026-08-07T12:00:00Z"` (= 19:00 giờ VN).

```ts
// Frontend recommendation
const toUtcIso = (localDate: Date) => localDate.toISOString();       // "…Z"
const parseUtc = (s: string) => new Date(s.endsWith('Z') ? s : s + 'Z');
```

**Tiền tệ:** đơn vị VND, không có phần lẻ trong thực tế nhưng cột DB là `numeric(12,2)` nên
response có thể trả `900000` hoặc `900000.00`. Format hiển thị bằng `Intl.NumberFormat('vi-VN')`.

### 3.5 Null và mảng rỗng

- Field nullable **luôn có mặt** trong JSON với giá trị `null` (không bị bỏ qua).
- Mảng rỗng trả `[]`, **không** trả `null` — trừ khi DTO khai `List<T>?` (chỉ có ở request).
- `data` trong response envelope là `null` khi request lỗi.

### 3.6 HTTP status codes

| Status | Khi nào |
|---|---|
| `200 OK` | **Mọi** response thành công, kể cả POST/PUT/DELETE. Không có 201/204 trong phạm vi này. |
| `400 Bad Request` | `ErrorType.Validation` — FluentValidation fail hoặc model binding fail |
| `401 Unauthorized` | Thiếu/hết hạn token, hoặc `ErrorType.Unauthorized` |
| `403 Forbidden` | Sai role, hoặc `ErrorType.Forbidden` (không sở hữu tài nguyên, bị block…) |
| `404 Not Found` | `ErrorType.NotFound` |
| `409 Conflict` | `ErrorType.Conflict` — vi phạm business rule, hoặc `DbUpdateConcurrencyException` |
| `500 Internal Server Error` | `ErrorType.Failure` hoặc exception không lường trước |

> **Không có `201 Created` và không có `204 No Content`.** Mọi controller đều `return Ok(result)`.
> DELETE cũng trả `200` kèm body.

### 3.7 Pagination

Mọi request phân trang dùng chung 2 query param:

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

Sai ràng buộc → `400` với `COMMON_VALIDATION_ERROR`.

### 3.8 Search behavior

- Tìm kiếm dùng **PostgreSQL `ILIKE '%keyword%'`** (case-insensitive, substring).
- Áp dụng cho `title` **OR** `content` của community post.
- **Không dùng full-text search / `pg_trgm` ranking.** Không có fuzzy matching, không có xếp hạng
  theo độ liên quan — kết quả vẫn sort theo `sortBy`.
- Keyword được `.Trim()` trước khi query. Keyword rỗng/whitespace → bỏ qua filter.

---

## 4. Authentication và authorization

**Backend contract.**

### 4.1 Token

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

JWT chứa claim `ClaimTypes.NameIdentifier` (= `nameid` / `sub`) là **user id dạng Guid**, và claim
role (`ClaimTypes.Role`). Backend lấy user id **luôn từ token**, không bao giờ từ body.

- `User.GetUserId()` → ném `401 AUTH_INVALID_CREDENTIALS` nếu claim thiếu/không parse được.
- `User.GetUserIdOrNull()` → trả `null`, dùng cho endpoint public.

### 4.2 Role constants

```
learner | coach | admin
```

### 4.3 Bảng authorization đầy đủ

| Endpoint | Auth | Role |
|---|---|---|
| `POST /api/vouchers/validate` | Bắt buộc | Bất kỳ (chỉ `[Authorize]`) |
| `POST /api/bookings/purchase/payos` | Bắt buộc | `learner` |
| `POST /api/bookings/purchase/manual` | Bắt buộc | `learner` |
| `* /api/admin/voucher-campaigns/**` | Bắt buộc | `admin` |
| `GET /api/community/posts` | **Không bắt buộc** | — |
| `GET /api/community/posts/{id}` | **Không bắt buộc** | — |
| `GET /api/community/posts/{postId}/comments` | **Không bắt buộc** | — |
| `GET /api/community/posts/me` | Bắt buộc | Bất kỳ |
| `POST/PUT/DELETE /api/community/posts/**` | Bắt buộc | Bất kỳ |
| `* /api/community/comments/**` | Bắt buộc | Bất kỳ |
| `* /api/community/applications/**` | Bắt buộc | Bất kỳ |
| `POST /api/reports` | Bắt buộc | Bất kỳ |
| `* /api/admin/community/**` | Bắt buộc | `admin` |
| `* /api/chat/**` | Bắt buộc | Bất kỳ |
| `* /api/users/{id}/block`, `GET /api/users/me/blocked` | Bắt buộc | Bất kỳ |

> **Quan trọng:** Chat và Community **không yêu cầu role `coach`**. Mọi user active đều dùng được.
> Chỉ purchase mới cần role `learner`.

### 4.4 Endpoint public — hành vi khi chưa đăng nhập

`GET /api/community/posts`, `GET /api/community/posts/{id}`, `GET /api/community/posts/{postId}/comments`
chạy được **không cần token**. Khi không có token:

| Field | Giá trị khi anonymous |
|---|---|
| `currentUserReacted` | `false` |
| `currentUserApplicationStatus` | `null` |
| `canApply` | `false` |
| `canEdit` | `false` |
| `canModerate` | `false` |
| `canEdit` (comment) | `false` |
| `canModerate` (comment) | `false` |

→ Frontend hiển thị nút "Đăng nhập để tham gia" thay vì "Tham gia".

Nếu gửi token **hết hạn** tới endpoint public: ASP.NET trả `401` trước khi vào controller. Frontend
nên **bỏ header Authorization** khi token đã hết hạn ở các trang public.

---

## 5. Shared response schemas

**Backend contract.** Đã xác minh trong `SporticoApp.Shared/Responses/`.

### 5.1 `Result<T>` — envelope thành công

> ⚠️ Field là **`isSuccess`**, **không phải** `success`. Không có field `message` ở `Result<T>`.

```json
{
  "isSuccess": true,
  "data": { },
  "error": null
}
```

### 5.2 `Result<T>` — envelope lỗi

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "VOUCHER_EXPIRED",
    "message": "This voucher has expired",
    "type": "Conflict",
    "details": null
  }
}
```

- `error.type` là **string PascalCase**, một trong:
  `"Validation"` | `"NotFound"` | `"Unauthorized"` | `"Forbidden"` | `"Conflict"` | `"Failure"`.
- `error.details` là `string[] | null`. Chỉ có giá trị khi:
  - FluentValidation fail → mảng message tiếng Anh của từng rule.
  - Model binding fail → mảng message của ModelState.
  - Lỗi 500 **ở môi trường Development** → `[message, stackTrace]`.

### 5.3 Lỗi validation — ví dụ thật

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "COMMON_VALIDATION_ERROR",
    "message": "Invalid request data",
    "type": "Validation",
    "details": [
      "sportId is required for this post type",
      "maxParticipants (>= 2, including the author) is required for this post type"
    ]
  }
}
```

### 5.4 Lỗi concurrency — ví dụ thật

Sinh ra bởi `ExceptionMiddleware` khi `DbUpdateConcurrencyException` (hai request tranh cùng một
tài nguyên giới hạn: slot cuối, lượt voucher cuối).

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "CONCURRENCY_CONFLICT",
    "message": "This request conflicted with another concurrent request. Please try again.",
    "type": "Conflict",
    "details": null
  }
}
```

HTTP `409`.

### 5.5 `PagedResult<T>`

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0,
  "hasPrevious": false,
  "hasNext": false
}
```

`totalPages`, `hasPrevious`, `hasNext` là computed property (`readOnly` trong Swagger) — chỉ đọc,
đừng gửi lên.

Kết hợp với envelope:

```json
{
  "isSuccess": true,
  "data": {
    "items": [],
    "pageNumber": 1,
    "pageSize": 12,
    "totalCount": 47,
    "totalPages": 4,
    "hasPrevious": false,
    "hasNext": true
  },
  "error": null
}
```

### 5.6 Response `Result<object>` cho action đơn giản

Một số action trả object nhỏ ad-hoc thay vì DTO. Giá trị **chính xác** như sau:

| Endpoint | `data` |
|---|---|
| `PUT /api/community/posts/{id}/like` | `{ "liked": true }` |
| `DELETE /api/community/posts/{id}/like` | `{ "liked": false }` |
| `DELETE /api/community/posts/{id}` | `{ "deleted": true }` |
| `DELETE /api/community/comments/{id}` | `{ "deleted": true }` |
| `DELETE /api/community/posts/{id}/applications/me` | `{ "cancelled": true }` |
| `DELETE /api/admin/community/posts/{id}` | `{ "deleted": true }` |
| `DELETE /api/admin/community/comments/{id}` | `{ "deleted": true }` |
| `PUT /api/users/{userId}/block` | `{ "blocked": true }` |
| `DELETE /api/users/{userId}/block` | `{ "blocked": false }` |

> Các key này là **camelCase trong anonymous object C#** nên serializer giữ nguyên: `liked`,
> `deleted`, `cancelled`, `blocked`.

### 5.7 Helper unwrap — Frontend recommendation

```ts
export class ApiError extends Error {
  constructor(
    public code: string,
    message: string,
    public type: ErrorTypeName,
    public status: number,
    public details: string[] | null,
  ) { super(message); }
}

export async function unwrap<T>(res: Response): Promise<T> {
  const body = (await res.json()) as ApiResult<T>;
  if (!res.ok || !body.isSuccess) {
    const e = body.error;
    throw new ApiError(
      e?.code ?? 'UNKNOWN',
      e?.message ?? 'Đã có lỗi xảy ra',
      e?.type ?? 'Failure',
      res.status,
      e?.details ?? null,
    );
  }
  return body.data as T;
}
```

---

## 6. Voucher integration

**Backend contract.**

### 6.1 `POST /api/vouchers/validate`

| Mục | Giá trị |
|---|---|
| Method | `POST` |
| URL | `/api/vouchers/validate` |
| Auth | **Bắt buộc** |
| Role | Bất kỳ (`[Authorize]` không giới hạn role) |
| Success status | `200` |

**Request:**

```json
{
  "code": "WELCOME10",
  "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135"
}
```

**Validation (`ValidateVoucherRequestValidator`):**

| Field | Rule |
|---|---|
| `code` | `NotEmpty`, `MaximumLength(64)` |
| `trainingPackageId` | `NotEmpty` (khác `Guid.Empty`) |

**Success response — voucher percentage có cap:**

```json
{
  "isSuccess": true,
  "data": {
    "code": "WELCOME10",
    "originalAmount": 1000000,
    "discountAmount": 100000,
    "totalAmount": 900000,
    "discountType": "percentage",
    "discountValue": 10,
    "maxDiscountAmount": 100000
  },
  "error": null
}
```

**Success response — voucher fixed_amount:**

```json
{
  "isSuccess": true,
  "data": {
    "code": "SPORTICO50K",
    "originalAmount": 1000000,
    "discountAmount": 50000,
    "totalAmount": 950000,
    "discountType": "fixed_amount",
    "discountValue": 50000,
    "maxDiscountAmount": null
  },
  "error": null
}
```

**Success response — voucher giảm 100%:**

```json
{
  "isSuccess": true,
  "data": {
    "code": "FREE100",
    "originalAmount": 1000000,
    "discountAmount": 1000000,
    "totalAmount": 0,
    "discountType": "percentage",
    "discountValue": 100,
    "maxDiscountAmount": null
  },
  "error": null
}
```

> Lưu ý: `code` trả về là **code chuẩn hóa lấy từ DB** (`campaign.Code`), không phải chuỗi user gõ.
> Ví dụ user gõ `welcome10` → response trả `"WELCOME10"`. Frontend nên hiển thị giá trị này.

### 6.2 Công thức tính discount (chính xác theo `VoucherService.ComputeDiscount`)

```
fixed_amount:
    discount = min(discountValue, originalAmount)

percentage:
    discount = originalAmount * discountValue / 100
    nếu maxDiscountAmount != null:
        discount = min(discount, maxDiscountAmount)

Sau đó clamp:
    discount = clamp(discount, 0, originalAmount)

totalAmount = max(0, originalAmount - discount)
```

`originalAmount` = `trainingPackage.Price`.

### 6.3 Thứ tự kiểm tra và error code tương ứng

Backend kiểm tra **theo đúng thứ tự này**, dừng ở lỗi đầu tiên:

| # | Điều kiện | Error code | HTTP |
|---|---|---|---|
| 1 | Package không tồn tại | `TRAINING_PACKAGE_NOT_FOUND` | 404 |
| 2 | Không tìm thấy campaign theo code | `VOUCHER_NOT_FOUND` | 404 |
| 3 | `status` là `draft` hoặc `paused` | `VOUCHER_NOT_ACTIVE` | 409 |
| 4 | `status` là `ended` | `VOUCHER_EXPIRED` | 409 |
| 5 | `now < startAt` | `VOUCHER_NOT_STARTED` | 409 |
| 6 | `now > endAt` | `VOUCHER_EXPIRED` | 409 |
| 7 | `originalAmount < minOrderAmount` | `VOUCHER_MIN_ORDER_NOT_MET` | 409 |
| 8 | `reservedCount + usedCount >= maxUsesTotal` | `VOUCHER_USAGE_LIMIT_REACHED` | 409 |
| 9 | Learner đã dùng đủ `maxUsesPerLearner` | `VOUCHER_LEARNER_LIMIT_REACHED` | 409 |
| 10 | `reservedDiscount + usedDiscount + discount > budgetAmount` | `VOUCHER_BUDGET_EXCEEDED` | 409 |

**Điểm quan trọng cho frontend:**
- `VOUCHER_NOT_ACTIVE` (draft/paused) và `VOUCHER_EXPIRED` (ended/quá hạn) là **hai code khác nhau**
  cho hai tình huống, hãy hiển thị message khác nhau.
- Bước 9 đếm cả redemption ở trạng thái `reserved` **lẫn** `applied` của learner đó.
- Bước 8 tính cả `reservedCount` → một learner khác đang thanh toán dở cũng chiếm lượt.

### 6.4 Quy tắc bắt buộc frontend phải tuân thủ

| Quy tắc | Chi tiết |
|---|---|
| **Validate KHÔNG reserve** | `ValidateAsync` chỉ đọc, không ghi DB, không giữ lượt. Kết quả có thể hết hiệu lực ngay sau đó. |
| **Không tin kết quả validate khi checkout** | Backend **tính lại toàn bộ** trong `ReserveForBookingAsync` tại thời điểm purchase. |
| **Không gửi `discountAmount`/`totalAmount` từ client** | DTO purchase chỉ có `trainingPackageId` + `voucherCode`. Không có field tiền nào. |
| **Voucher có thể hết lượt giữa validate và purchase** | Purchase vẫn có thể fail với `VOUCHER_USAGE_LIMIT_REACHED`/`VOUCHER_BUDGET_EXCEEDED`. Phải xử lý. |
| **Mỗi booking tối đa 1 voucher** | DB có `UNIQUE (booking_id)` trên `voucher_redemptions`. |
| **Không cộng dồn voucher** | Request chỉ nhận **một** `voucherCode` dạng string. |
| **Case-insensitive** | Cột `code` là PostgreSQL `citext` + unique index → `welcome10` = `WELCOME10`. |
| **Có trim whitespace** | Backend gọi `request.Code.Trim()`. Frontend vẫn nên trim để UX tốt. |
| **Xóa mã → về giá gốc** | Gửi `voucherCode: null` (hoặc bỏ field) → booking dùng nguyên `package.Price`. |

### 6.5 UI state machine cho ô nhập voucher

**Frontend recommendation.**

```mermaid
stateDiagram-v2
    [*] --> idle
    idle --> validating: bấm "Áp dụng"
    validating --> valid: 200 OK
    validating --> invalid: 4xx
    invalid --> validating: sửa mã, bấm lại
    valid --> removed: bấm "Xóa mã"
    removed --> idle
    valid --> purchase_rejected: purchase trả lỗi VOUCHER_*
    purchase_rejected --> validating: bấm "Thử lại"
    purchase_rejected --> removed: bấm "Xóa mã"
```

| State | Ý nghĩa | Hiển thị |
|---|---|---|
| `idle` | Chưa nhập / chưa áp dụng | Input trống, nút "Áp dụng" disabled |
| `validating` | Đang gọi `POST /api/vouchers/validate` | Spinner trên nút, input disabled |
| `valid` | Đã có quote hợp lệ | Badge xanh + dòng "Giảm giá", nút "Xóa mã" |
| `invalid` | Validate trả lỗi | Text đỏ = message theo error code, giữ nguyên giá gốc |
| `removed` | User vừa xóa mã | Reset về `idle`, tổng tiền = giá gốc |
| `purchase_rejected` | Validate OK nhưng purchase fail vì voucher | Banner cảnh báo + 2 nút: "Thử lại" / "Mua không dùng mã" |

### 6.6 Điều kiện disable các nút

| Nút | Disable khi |
|---|---|
| **Áp dụng** | `code.trim() === ''` **hoặc** `state === 'validating'` **hoặc** `!trainingPackageId` **hoặc** đang purchase |
| **Xóa mã** | `state !== 'valid' && state !== 'purchase_rejected'` **hoặc** đang purchase |
| **Thử lại** | `state !== 'purchase_rejected'` **hoặc** đang validating |
| **Thanh toán** | Đang validating **hoặc** đang purchase **hoặc** chưa chọn package |

> **Không** disable nút "Thanh toán" chỉ vì voucher `invalid` — user vẫn được mua với giá gốc.

### 6.7 Hiển thị bảng giá

```
Giá gốc          1.000.000 ₫        ← quote.originalAmount (hoặc package.price khi chưa có voucher)
Giảm giá          -100.000 ₫        ← quote.discountAmount, chỉ hiện khi > 0
─────────────────────────────
Tổng thanh toán    900.000 ₫        ← quote.totalAmount (hoặc package.price khi chưa có voucher)
```

Khi `totalAmount === 0`, hiển thị "Miễn phí" và đổi nhãn nút thành "Nhận gói miễn phí"
(nhưng **quyết định redirect vẫn dựa vào `paymentRequired`**, xem mục 7.4).

---

## 7. Booking purchase với voucher

**Backend contract.**

### 7.1 `POST /api/bookings/purchase/payos`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Role | **`learner`** |
| Success status | `200` |

**Request — có voucher:**

```json
{
  "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135",
  "voucherCode": "WELCOME10"
}
```

**Request — không voucher (cả hai cách đều hợp lệ):**

```json
{ "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135", "voucherCode": null }
```

```json
{ "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135" }
```

**Validation (`PurchaseTrainingPackagePayOsRequestValidator`):**

| Field | Rule |
|---|---|
| `trainingPackageId` | `NotEmpty` |
| `voucherCode` | `MaximumLength(64)` — chỉ kiểm khi `!= null` |

### 7.2 Response — cần thanh toán PayOS

```json
{
  "isSuccess": true,
  "data": {
    "bookingId": "6b0f1b2a-6c2e-4a3b-9c1d-2e5f8a7b6c40",
    "paymentId": "1c9d3e5f-4a2b-4c6d-8e0f-9a1b2c3d4e5f",
    "orderCode": 1785801600123,
    "checkoutUrl": "https://pay.payos.vn/web/8f1c2d3e4a5b6c7d",
    "status": "pending",
    "paymentStatus": "pending",
    "paymentRequired": true,
    "bookingStatus": "pending_payment",
    "expiredAt": "2026-08-04T10:30:00Z"
  },
  "error": null
}
```

### 7.3 Response — voucher giảm 100% (không gọi PayOS)

```json
{
  "isSuccess": true,
  "data": {
    "bookingId": "7a1e2c3d-8b4f-4d5e-a6b7-c8d9e0f1a2b3",
    "paymentId": "2d0e4f6a-5b3c-4d7e-9f10-ab2c3d4e5f60",
    "orderCode": null,
    "checkoutUrl": null,
    "status": "paid",
    "paymentStatus": "paid",
    "paymentRequired": false,
    "bookingStatus": "active",
    "expiredAt": null
  },
  "error": null
}
```

**Giải thích field:**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `bookingId` | `string` | Luôn có |
| `paymentId` | `string` | Luôn có (kể cả voucher 100% — vẫn tạo `Payment` nội bộ với `method = "voucher"`) |
| `orderCode` | `number \| null` | `null` khi voucher 100% |
| `checkoutUrl` | `string \| null` | `null` khi voucher 100% |
| `status` | `string` | Payment status. Giữ để backward compatible với client cũ. |
| `paymentStatus` | `string` | **Read-only alias** của `status`. Giá trị luôn giống hệt. |
| `paymentRequired` | `boolean` | **Cờ quyết định luồng frontend** |
| `bookingStatus` | `string` | `"pending_payment"` hoặc `"active"` |
| `expiredAt` | `string \| null` | Hạn của link PayOS. `null` khi voucher 100%. |

### 7.4 Logic điều hướng bắt buộc

```ts
// Backend contract
if (res.paymentRequired) {
  window.location.href = res.checkoutUrl!;   // redirect PayOS
} else {
  router.push(`/bookings/${res.bookingId}`); // đã active, KHÔNG mở checkoutUrl
}
```

> ⚠️ **Không** kiểm tra voucher 100% bằng `totalAmount === 0` ở frontend. Response purchase không
> có field `totalAmount`. Dùng đúng `paymentRequired`.

### 7.5 `POST /api/bookings/purchase/manual`

Cùng request DTO, nhưng response là **`BookingResponse` đầy đủ** (không phải
`PurchaseTrainingPackagePayOsResponse`), và booking được tạo ở trạng thái `active` ngay.

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc, role `learner` |
| Feature flag | `Features:EnableManualPurchase`. Khi tắt → `403 MANUAL_PURCHASE_DISABLED` |
| Dùng cho | **Chỉ dev/test.** Production tắt flag này. |

**Request:**

```json
{
  "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135",
  "voucherCode": "WELCOME10"
}
```

**Response:** xem `BookingResponse` ở mục 7.6.

**Lỗi khi flag tắt:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "MANUAL_PURCHASE_DISABLED",
    "message": "Manual purchase is disabled. Please use the PayOS purchase flow.",
    "type": "Forbidden",
    "details": null
  }
}
```

### 7.6 `BookingResponse` — field đầy đủ

```json
{
  "isSuccess": true,
  "data": {
    "id": "6b0f1b2a-6c2e-4a3b-9c1d-2e5f8a7b6c40",
    "learnerId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
    "coachId": "8e0c2a4b-6d8f-401a-b3c5-d7e9f1a3b5c7",
    "trainingPackageId": "9f420548-9b36-49d8-a99f-cb3fb0051135",
    "trainingPackageTitle": "Gói cầu lông cơ bản 8 buổi",
    "totalAmount": 900000,
    "originalAmount": 1000000,
    "discountAmount": 100000,
    "voucherCampaignId": "5c7e9a1b-3d5f-4072-8194-a6b8c0d2e4f6",
    "voucherCode": "WELCOME10",
    "platformFeeRate": 0.15,
    "platformFeeAmount": 150000,
    "coachReceiveAmount": 850000,
    "perSessionCoachAmount": 106250,
    "totalSessions": 8,
    "completedSessions": 0,
    "usedSessions": 8,
    "remainingSessions": 0,
    "canBookSession": false,
    "sessionCountsByStatus": { "scheduled": 8 },
    "status": "active",
    "paidAt": "2026-08-04T09:15:00Z",
    "completedAt": null,
    "cancelledAt": null,
    "expiresAt": "2026-09-03T09:15:00Z",
    "createdAt": "2026-08-04T09:15:00Z",
    "updatedAt": "2026-08-04T09:15:00Z"
  },
  "error": null
}
```

**Booking không dùng voucher:**

```json
{
  "totalAmount": 1000000,
  "originalAmount": 1000000,
  "discountAmount": 0,
  "voucherCampaignId": null,
  "voucherCode": null
}
```

> Booking cũ (trước migration) được backfill: `originalAmount = totalAmount`, `discountAmount = 0`,
> `voucherCampaignId = null`, `voucherCode = null`. Frontend không cần xử lý đặc biệt.

### 7.7 Ai được xem field nào

**Frontend recommendation** (backend **không** lọc field theo role — mọi field đều trả về; đây là
gợi ý về mặt UI/nghiệp vụ):

| Field | Learner | Coach | Admin |
|---|---|---|---|
| `originalAmount`, `discountAmount`, `totalAmount` | ✅ Hiện | ⚠️ Nên ẩn `discountAmount` | ✅ Hiện |
| `voucherCode` | ✅ Hiện | ❌ Không cần | ✅ Hiện |
| `platformFeeRate`, `platformFeeAmount` | ❌ **Không hiển thị công khai** | ⚠️ Cân nhắc | ✅ Hiện |
| `coachReceiveAmount`, `perSessionCoachAmount` | ❌ **Không hiển thị** | ✅ Hiện | ✅ Hiện |
| `totalSessions`, `usedSessions`, `remainingSessions` | ✅ | ✅ | ✅ |

> ⚠️ **Known limitation:** backend trả toàn bộ field cho mọi role. Việc ẩn `platformFee*` /
> `coachReceiveAmount` khỏi màn hình learner là **trách nhiệm của frontend**. Đừng coi đây là bảo mật.

### 7.8 Voucher KHÔNG làm giảm thu nhập HLV

**Backend contract** — công thức trong `BookingService.CreateBookingSnapshot`:

```
originalAmount     = trainingPackage.Price
discountAmount     = voucherReservation?.DiscountAmount ?? 0
totalAmount        = max(0, originalAmount - discountAmount)     ← learner trả

platformFeeAmount  = originalAmount × platformFeeRate            ← tính trên GIÁ GỐC
coachReceiveAmount = originalAmount - platformFeeAmount          ← KHÔNG bị voucher ảnh hưởng
perSessionCoachAmount = coachReceiveAmount / totalSessions       ← KHÔNG bị voucher ảnh hưởng
```

→ Voucher là **platform-funded**: nền tảng chịu toàn bộ phần giảm giá. Ví dụ với giá 1.000.000₫,
commission 15%, voucher giảm 100.000₫:

| Chỉ số | Giá trị |
|---|---|
| Learner trả | 900.000 ₫ |
| HLV nhận | 850.000 ₫ (= 1.000.000 − 150.000, **không đổi**) |
| Platform gross fee | 150.000 ₫ |
| **Platform net revenue** | **50.000 ₫** (= 900.000 − 850.000) |

`platformFeeRate` được snapshot tại thời điểm tạo booking từ `PlatformSetting` (configurable, mặc
định 0%). Thay đổi commission hay campaign về sau **không** làm đổi booking cũ.

### 7.9 Idempotency — webhook và reconcile

**Backend contract.** Frontend không cần làm gì, nhưng cần hiểu để không hoảng:

- Redemption chỉ chuyển `reserved → applied` khi đang ở `reserved`. Chạy lại → no-op.
- Redemption chỉ chuyển `reserved → released` khi đang ở `reserved`. Đã `applied` → **không bao giờ**
  bị release.
- Webhook PayOS và endpoint reconcile chạy đồng thời vẫn chỉ apply đúng một lần.
- Counter dùng `Math.Max(0, …)` → không bao giờ âm.
- Background worker `PaymentAndVoucherExpirySweepBackgroundService` chạy mỗi **10 phút**, giải phóng
  booking `pending_payment` quá hạn + voucher `reserved` quá `expiresAt` (30 phút kể từ khi reserve).

→ **Hệ quả cho frontend:** sau khi user quay lại từ PayOS, **phải gọi backend** để lấy trạng thái
thật. Đừng tin query param PayOS trả về. Xem mục 33 Flow A.

### 7.10 Reconcile sau khi quay lại từ PayOS

**Backend contract.** Đây là endpoint frontend **bắt buộc** phải gọi sau khi user quay lại từ PayOS.

| Mục | Giá trị |
|---|---|
| Method / URL | `POST /api/payments/payos/reconcile` |
| Biến thể | `POST /api/payments/payos/{orderCode}/reconcile` (không cần body) |
| Auth | Bắt buộc, role **`learner`** |
| Webhook | `POST /api/payments/payos/webhook` — **`[AllowAnonymous]`, chỉ PayOS gọi. Frontend không đụng tới.** |

**Request (body variant) — gửi `orderCode` HOẶC `paymentId`:**

```json
{ "orderCode": 1785801600123, "paymentId": null }
```

```json
{ "orderCode": null, "paymentId": "1c9d3e5f-4a2b-4c6d-8e0f-9a1b2c3d4e5f" }
```

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "paymentId": "1c9d3e5f-4a2b-4c6d-8e0f-9a1b2c3d4e5f",
    "orderCode": 1785801600123,
    "paymentStatus": "paid",
    "bookingId": "6b0f1b2a-6c2e-4a3b-9c1d-2e5f8a7b6c40",
    "bookingStatus": "active",
    "activated": true,
    "payOsStatus": "PAID",
    "message": "Payment confirmed and booking activated."
  },
  "error": null
}
```

Khi user hủy trên PayOS:

```json
{
  "isSuccess": true,
  "data": {
    "paymentId": "1c9d3e5f-4a2b-4c6d-8e0f-9a1b2c3d4e5f",
    "orderCode": 1785801600123,
    "paymentStatus": "cancelled",
    "bookingId": "6b0f1b2a-6c2e-4a3b-9c1d-2e5f8a7b6c40",
    "bookingStatus": "cancelled",
    "activated": false,
    "payOsStatus": "CANCELLED",
    "message": "Payment was cancelled."
  },
  "error": null
}
```

> `message` là **tiếng Anh**, sinh từ backend. Frontend nên map sang tiếng Việt theo
> `paymentStatus`/`activated` thay vì hiển thị thẳng.

---

## 8. Admin voucher management

**Backend contract.** Toàn bộ endpoint yêu cầu **role `admin`**.
Base route: `/api/admin/voucher-campaigns`.

### 8.1 `POST /api/admin/voucher-campaigns` — tạo campaign

**Request — percentage:**

```json
{
  "code": "WELCOME10",
  "name": "Giảm 10% cho người học mới",
  "description": "Chương trình chào mừng học viên mới",
  "discountType": "percentage",
  "discountValue": 10,
  "maxDiscountAmount": 100000,
  "minOrderAmount": 500000,
  "startAt": "2026-08-05T00:00:00Z",
  "endAt": "2026-08-31T23:59:59Z",
  "maxUsesTotal": 500,
  "maxUsesPerLearner": 1,
  "budgetAmount": 50000000
}
```

**Request — fixed_amount:**

```json
{
  "code": "SPORTICO50K",
  "name": "Giảm 50.000đ",
  "description": null,
  "discountType": "fixed_amount",
  "discountValue": 50000,
  "maxDiscountAmount": null,
  "minOrderAmount": 300000,
  "startAt": "2026-08-05T00:00:00Z",
  "endAt": "2026-09-05T00:00:00Z",
  "maxUsesTotal": 1000,
  "maxUsesPerLearner": 2,
  "budgetAmount": 50000000
}
```

**Request tối thiểu (mọi field optional đều bỏ được):**

```json
{
  "code": "NOLIMIT",
  "name": "Không giới hạn",
  "discountType": "percentage",
  "discountValue": 5
}
```

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "id": "5c7e9a1b-3d5f-4072-8194-a6b8c0d2e4f6",
    "code": "WELCOME10",
    "name": "Giảm 10% cho người học mới",
    "description": "Chương trình chào mừng học viên mới",
    "discountType": "percentage",
    "discountValue": 10,
    "maxDiscountAmount": 100000,
    "minOrderAmount": 500000,
    "startAt": "2026-08-05T00:00:00Z",
    "endAt": "2026-08-31T23:59:59Z",
    "status": "draft",
    "maxUsesTotal": 500,
    "maxUsesPerLearner": 1,
    "reservedCount": 0,
    "usedCount": 0,
    "budgetAmount": 50000000,
    "reservedDiscountAmount": 0,
    "usedDiscountAmount": 0,
    "createdByUserId": "0a1b2c3d-4e5f-4061-8273-8495a6b7c8d9",
    "updatedByUserId": null,
    "createdAt": "2026-08-04T08:00:00Z",
    "updatedAt": "2026-08-04T08:00:00Z"
  },
  "error": null
}
```

> ⚠️ Campaign **luôn** được tạo với `status: "draft"`. Không có cách nào tạo thẳng `active` —
> phải gọi `PUT .../activate` sau đó.

**Validation (`CreateVoucherCampaignRequestValidator`) — chính xác:**

| Field | Bắt buộc | Rule |
|---|---|---|
| `code` | ✅ | `NotEmpty`, `MaximumLength(64)`, regex `^[A-Za-z0-9_-]+$` (chỉ chữ, số, `-`, `_`) |
| `name` | ✅ | `NotEmpty`, `MaximumLength(200)` |
| `description` | ❌ | `MaximumLength(2000)` |
| `discountType` | ✅ | Phải là `"fixed_amount"` hoặc `"percentage"` |
| `discountValue` | ✅ | `> 0`. Nếu `discountType == "percentage"` thì thêm `<= 100` |
| `maxDiscountAmount` | ❌ | `> 0` khi có giá trị |
| `minOrderAmount` | ❌ | `>= 0` khi có giá trị |
| `startAt` | ❌ | — |
| `endAt` | ❌ | — |
| (cặp ngày) | — | `startAt < endAt` khi **cả hai** đều có giá trị |
| `maxUsesTotal` | ❌ | `> 0` khi có giá trị |
| `maxUsesPerLearner` | ❌ | `> 0` khi có giá trị |
| `budgetAmount` | ❌ | `> 0` khi có giá trị |

> **Lưu ý:** validator **không** ép `startAt` phải ở tương lai. Admin có thể tạo campaign với ngày
> quá khứ (nó sẽ lập tức hết hạn khi active).
> `maxDiscountAmount` **không** bị chặn với `fixed_amount` — nhưng nó vô nghĩa với loại này
> (`ComputeDiscount` chỉ dùng `maxDiscountAmount` cho `percentage`). Frontend nên ẩn field này khi
> chọn `fixed_amount`.

**Lỗi trùng code:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "VOUCHER_CODE_ALREADY_EXISTS",
    "message": "Voucher code 'WELCOME10' already exists",
    "type": "Conflict",
    "details": null
  }
}
```

HTTP `409`. Do cột `code` là `citext`, `welcome10` cũng bị coi là trùng với `WELCOME10`.

### 8.2 `GET /api/admin/voucher-campaigns` — danh sách

**Query parameters:**

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `status` | `string` | — | `draft` \| `active` \| `paused` \| `ended` |
| `keyword` | `string` | — | Tìm theo code/name |
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

```http
GET /api/admin/voucher-campaigns?status=active&keyword=welcome&pageNumber=1&pageSize=20
```

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "5c7e9a1b-3d5f-4072-8194-a6b8c0d2e4f6",
        "code": "WELCOME10",
        "name": "Giảm 10% cho người học mới",
        "description": "Chương trình chào mừng học viên mới",
        "discountType": "percentage",
        "discountValue": 10,
        "maxDiscountAmount": 100000,
        "minOrderAmount": 500000,
        "startAt": "2026-08-05T00:00:00Z",
        "endAt": "2026-08-31T23:59:59Z",
        "status": "active",
        "maxUsesTotal": 500,
        "maxUsesPerLearner": 1,
        "reservedCount": 3,
        "usedCount": 47,
        "budgetAmount": 50000000,
        "reservedDiscountAmount": 300000,
        "usedDiscountAmount": 4700000,
        "createdByUserId": "0a1b2c3d-4e5f-4061-8273-8495a6b7c8d9",
        "updatedByUserId": "0a1b2c3d-4e5f-4061-8273-8495a6b7c8d9",
        "createdAt": "2026-08-04T08:00:00Z",
        "updatedAt": "2026-08-04T09:12:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

### 8.3 `GET /api/admin/voucher-campaigns/{id}` — chi tiết

`No request body.` Response: `Result<VoucherCampaignResponse>` — schema y hệt mục 8.1.

Không tìm thấy → `404 VOUCHER_CAMPAIGN_NOT_FOUND`.

### 8.4 `PUT /api/admin/voucher-campaigns/{id}` — cập nhật

**Mọi field đều optional.** Field `null`/bỏ qua → giữ nguyên giá trị cũ.

**Request:**

```json
{
  "name": "Giảm 10% — gia hạn tháng 9",
  "description": "Gia hạn thêm 1 tháng",
  "discountType": null,
  "discountValue": null,
  "maxDiscountAmount": null,
  "minOrderAmount": null,
  "startAt": null,
  "endAt": "2026-09-30T23:59:59Z",
  "maxUsesTotal": 800,
  "maxUsesPerLearner": null,
  "budgetAmount": 80000000
}
```

**Response:** `Result<VoucherCampaignResponse>` (bản đã cập nhật).

**Quy tắc khóa field tài chính — quan trọng:**

Nếu campaign **đã từng có redemption** (bất kỳ trạng thái nào), backend **từ chối** request có chứa
bất kỳ field nào trong nhóm sau (dù giá trị y hệt cũ):

```
discountType, discountValue, maxDiscountAmount, minOrderAmount
```

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "VOUCHER_CAMPAIGN_HAS_REDEMPTIONS",
    "message": "This campaign already has redemptions; its discount fields can no longer be edited. End it and create a new campaign instead.",
    "type": "Conflict",
    "details": null
  }
}
```

HTTP `409`.

> **Frontend recommendation:** khi `usedCount + reservedCount > 0`, disable 4 input trên và
> **không gửi chúng** trong payload (gửi `null`). Hiển thị tooltip: "Chiến dịch đã phát sinh lượt sử
> dụng — hãy kết thúc và tạo chiến dịch mới nếu cần đổi mức giảm."
>
> ⚠️ Lưu ý: `reservedCount`/`usedCount` **không** phải chỉ báo chính xác 100% của
> `hasRedemptions` — backend kiểm bằng query riêng trên bảng `voucher_redemptions` (bao gồm cả
> redemption đã `released`). Một campaign có `reservedCount = 0, usedCount = 0` vẫn có thể bị khóa
> nếu từng có redemption bị release. Frontend phải xử lý lỗi `409` này kể cả khi counter = 0.

**Các field KHÔNG bị khóa** (sửa được kể cả khi đã có redemption):
`name`, `description`, `startAt`, `endAt`, `maxUsesTotal`, `maxUsesPerLearner`, `budgetAmount`.

**`code` không thể sửa** — `UpdateVoucherCampaignRequest` không có field `code`.

Ngày sai sau khi merge → `400 VOUCHER_INVALID_DATE_RANGE`.

### 8.5 Chuyển trạng thái

| Endpoint | Kết quả |
|---|---|
| `PUT /api/admin/voucher-campaigns/{id}/activate` | `status → "active"` |
| `PUT /api/admin/voucher-campaigns/{id}/pause` | `status → "paused"` |
| `PUT /api/admin/voucher-campaigns/{id}/end` | `status → "ended"` |

- `No request body` cho cả ba.
- Response: `Result<VoucherCampaignResponse>`.
- **Quy tắc duy nhất:** nếu `status` hiện tại đã là `"ended"` → `409`:

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "VOUCHER_CAMPAIGN_ALREADY_ENDED",
    "message": "An ended campaign cannot be reactivated",
    "type": "Conflict",
    "details": null
  }
}
```

- Mọi chuyển đổi khác đều được phép: `draft → active`, `draft → paused`, `draft → ended`,
  `active → paused`, `paused → active`, `active → ended`, `paused → ended`, và cả
  `active → active` (no-op thành công).
- Mỗi lần chuyển đều set `updatedByUserId` = admin thực hiện.

### 8.6 Ma trận nút theo trạng thái

**Backend contract** (suy ra trực tiếp từ `TransitionStatusAsync` + `UpdateCampaignAsync`):

| Trạng thái | Edit | Activate | Pause | End |
|---|:---:|:---:|:---:|:---:|
| `draft` | ✅ | ✅ | ✅ (hợp lệ nhưng vô nghĩa) | ✅ |
| `active` | ✅¹ | ✅ (no-op) | ✅ | ✅ |
| `paused` | ✅¹ | ✅ | ✅ (no-op) | ✅ |
| `ended` | ✅¹ | ❌ `409` | ❌ `409` | ❌ `409` |

¹ Edit luôn được gọi, nhưng 4 field tài chính bị khóa nếu campaign đã có redemption (mục 8.4).

**Frontend recommendation** — chỉ hiện nút thực sự có ý nghĩa:

| Trạng thái | Nút nên hiện |
|---|---|
| `draft` | Sửa · **Kích hoạt** · Kết thúc |
| `active` | Sửa · **Tạm dừng** · Kết thúc |
| `paused` | Sửa · **Kích hoạt lại** · Kết thúc |
| `ended` | Xem chi tiết (read-only) · Xem redemptions |

### 8.7 Badge trạng thái hiển thị

**Frontend recommendation** — backend **chỉ** trả 4 giá trị `status`. Các nhãn phái sinh dưới đây do
frontend tự tính:

| Nhãn hiển thị | Điều kiện tính từ response |
|---|---|
| Nháp | `status === 'draft'` |
| Đang chạy | `status === 'active'` và trong khoảng thời gian |
| **Đã lên lịch** | `status === 'active'` và `startAt != null && now < startAt` |
| **Đã hết hạn** | `status === 'active'` và `endAt != null && now > endAt` |
| Tạm dừng | `status === 'paused'` |
| Đã kết thúc | `status === 'ended'` |
| **Hết lượt** | `maxUsesTotal != null && reservedCount + usedCount >= maxUsesTotal` |
| **Hết ngân sách** | `budgetAmount != null && reservedDiscountAmount + usedDiscountAmount >= budgetAmount` |

> Backend **không** có status `scheduled`/`exhausted`. Đừng gửi các giá trị này lên filter — validator
> sẽ trả `400`.

### 8.8 `GET /api/admin/voucher-campaigns/{id}/redemptions`

**Query parameters:**

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `status` | `string` | — | `reserved` \| `applied` \| `released` |
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

> ⚠️ `VoucherRedemptionFilterRequestValidator` **không** validate giá trị `status` — gửi giá trị lạ
> sẽ không lỗi, chỉ trả về danh sách rỗng.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "9b8c7d6e-5f4a-4b3c-9d2e-1f0a9b8c7d6e",
        "voucherCampaignId": "5c7e9a1b-3d5f-4072-8194-a6b8c0d2e4f6",
        "bookingId": "6b0f1b2a-6c2e-4a3b-9c1d-2e5f8a7b6c40",
        "learnerId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
        "paymentId": "1c9d3e5f-4a2b-4c6d-8e0f-9a1b2c3d4e5f",
        "status": "applied",
        "originalAmount": 1000000,
        "discountAmount": 100000,
        "reservedAt": "2026-08-04T09:10:00Z",
        "expiresAt": "2026-08-04T09:40:00Z",
        "appliedAt": "2026-08-04T09:15:00Z",
        "releasedAt": null,
        "releaseReason": null
      },
      {
        "id": "1a2b3c4d-5e6f-4708-8192-a3b4c5d6e7f8",
        "voucherCampaignId": "5c7e9a1b-3d5f-4072-8194-a6b8c0d2e4f6",
        "bookingId": "8c1d2e3f-4a5b-4c6d-8e9f-0a1b2c3d4e5f",
        "learnerId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
        "paymentId": null,
        "status": "released",
        "originalAmount": 1000000,
        "discountAmount": 100000,
        "reservedAt": "2026-08-04T08:00:00Z",
        "expiresAt": "2026-08-04T08:30:00Z",
        "appliedAt": null,
        "releasedAt": "2026-08-04T08:31:00Z",
        "releaseReason": "payment_expired"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 2,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

**Giá trị `releaseReason` có thể gặp** (string tự do, tối đa 50 ký tự, do backend set):

| Giá trị | Khi nào |
|---|---|
| `payos_link_creation_failed` | Tạo link PayOS thất bại → booking bị hủy ngay |
| `payment_expired` | Link PayOS hết hạn, worker/reconcile dọn dẹp |
| `null` | Redemption chưa bị release |

> `releaseReason` cũng có thể mang giá trị khác do webhook (cancelled/failed). Frontend nên hiển thị
> raw string với fallback, đừng hardcode exhaustive switch.

Campaign không tồn tại → `404 VOUCHER_CAMPAIGN_NOT_FOUND`.

### 8.9 Ảnh hưởng lên Admin Payment Dashboard

**Backend contract.** `PaymentStatisticsResponse` (endpoint dashboard admin sẵn có) bổ sung 5 field:

| Field | Công thức | Ý nghĩa |
|---|---|---|
| `grossPackageValue` | `SUM(booking.OriginalAmount)` | Tổng giá gói trước voucher |
| `totalDiscount` | `SUM(booking.DiscountAmount)` | Tổng tiền nền tảng tài trợ |
| `netCollected` | `grossPackageValue - totalDiscount` | Tiền thực thu — **alias của `totalRevenue`** |
| `platformGrossFee` | `SUM(booking.PlatformFeeAmount)` | Hoa hồng trên giá gốc — **alias của `platformRevenue`** |
| `platformNetRevenue` | `netCollected - coachRevenue` | **Lợi nhuận thật của nền tảng** |

Field cũ giữ nguyên để backward compatible:

| Field cũ | Bằng với |
|---|---|
| `totalRevenue` | `netCollected` |
| `platformRevenue` | `platformGrossFee` |
| `coachRevenue` | `SUM(booking.CoachReceiveAmount)` — **không bao giờ bị voucher làm giảm** |

> ⚠️ **Đừng hiển thị `platformRevenue` / `platformGrossFee` như "lợi nhuận".** Sau khi có voucher,
> con số đúng để gọi là lợi nhuận là **`platformNetRevenue`**.

---

## 9. Community feed

**Backend contract.**

### 9.1 `GET /api/community/posts`

| Mục | Giá trị |
|---|---|
| Auth | **Không bắt buộc** (`[AllowAnonymous]`) |
| Role | — |
| Success status | `200` |

```http
GET /api/community/posts?pageNumber=1&pageSize=12&postType=looking_for_players&sportId=2&sortBy=upcoming
```

### 9.2 Query parameters đầy đủ

| Param | Kiểu | Mặc định | Ràng buộc / Hành vi |
|---|---|---|---|
| `postType` | `string \| null` | — | Phải thuộc 7 giá trị hợp lệ, nếu không → `400` |
| `sportId` | `int \| null` | — | Lọc chính xác |
| `keyword` | `string \| null` | — | `ILIKE '%kw%'` trên `title` **OR** `content` |
| `city` | `string \| null` | — | ⚠️ Thực tế lọc `ILIKE '%city%'` trên **`locationName`**, không phải `address` |
| `fromDate` | `DateTime \| null` | — | `startAt == null OR startAt >= fromDate` |
| `toDate` | `DateTime \| null` | — | `startAt == null OR startAt <= toDate` |
| `level` | `string \| null` | — | So sánh **bằng chính xác** với `post.level` (không ILIKE) |
| `hasAvailableSlots` | `bool \| null` | — | Chỉ có tác dụng khi `= true`: `maxParticipants != null AND acceptedParticipants < maxParticipants` |
| `authorId` | `Guid \| null` | — | Lọc theo tác giả |
| `followingOnly` | `bool` | `false` | Chỉ hiệu lực khi **đã đăng nhập**; lọc theo bảng `follows` |
| `sortBy` | `string` | `"latest"` | `latest` \| `upcoming` \| `most_discussed` — sai → `400` |
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

**Chi tiết `sortBy`:**

| Giá trị | Sắp xếp thực tế |
|---|---|
| `latest` | `createdAt DESC` |
| `upcoming` | Bài có `startAt` lên trước (`startAt == null` xuống cuối), rồi `startAt ASC` |
| `most_discussed` | `(commentCount + reactionCount) DESC` |

> ⚠️ `fromDate`/`toDate` **cũng khớp** bài có `startAt == null` (thảo luận, câu hỏi). Nếu bạn muốn
> chỉ lấy sự kiện có ngày, hãy kết hợp `postType`.
>
> ⚠️ `city` khớp với `locationName`, **không** khớp `address`. Nếu người dùng nhập "Quận 7" nhưng
> `locationName` là "Sân cầu lông ABC" thì sẽ không ra kết quả. **Frontend recommendation:** đặt
> placeholder ô lọc là "Tên địa điểm" thay vì "Thành phố".

### 9.3 Trạng thái nào xuất hiện trong feed

Feed **chỉ** trả bài có `status` thuộc `CommunityPostStatuses.PubliclyVisible`:

| Status | Trong feed công khai? | Ghi chú |
|---|:---:|---|
| `published` | ✅ Có | Bình thường |
| `closed` | ✅ **Có** | Đã đủ người / tác giả đóng — vẫn hiện, nhưng `canApply = false` |
| `expired` | ✅ **Có** | Đã qua thời gian — vẫn hiện |
| `draft` | ❌ Không | Chỉ chính tác giả thấy qua `/me` |
| `hidden` | ❌ Không | Admin ẩn |
| `deleted` | ❌ Không | Soft delete |

> **Frontend recommendation:** vì `closed`/`expired` vẫn nằm trong feed, hãy hiển thị badge rõ ràng
> ("Đã đủ người" / "Đã kết thúc") và làm mờ card, hoặc thêm bộ lọc client-side để ẩn chúng.

### 9.4 Response — một post card đầy đủ

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "author": {
          "id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
          "fullName": "Nguyễn Văn A",
          "avatarUrl": "https://cdn.sportico.example/avatars/a.webp"
        },
        "sportId": 2,
        "sportName": "Cầu lông",
        "postType": "looking_for_players",
        "title": "Tìm 2 người đánh cầu lông tối thứ Sáu",
        "content": "Nhóm hiện có hai người, trình độ trung bình, chơi lúc 19h-21h tại Quận 7. Cần thêm 2 bạn để đánh đôi.",
        "locationName": "Sân cầu lông Quận 7",
        "address": "123 Nguyễn Thị Thập, Quận 7, TP.HCM",
        "latitude": 10.735,
        "longitude": 106.721,
        "startAt": "2026-08-07T12:00:00Z",
        "endAt": "2026-08-07T14:00:00Z",
        "maxParticipants": 4,
        "acceptedParticipants": 2,
        "slotsRemaining": 2,
        "level": "intermediate",
        "feePerPerson": 70000,
        "status": "published",
        "allowComments": true,
        "commentCount": 5,
        "reactionCount": 12,
        "applicationCount": 3,
        "viewCount": 100,
        "media": [
          {
            "id": "c8e3a2b1-4d5f-4e60-9b7c-8d9e0f1a2b3c",
            "mediaType": "image",
            "url": "https://cdn.sportico.example/community/court-1.webp",
            "thumbnailUrl": "https://cdn.sportico.example/community/court-1-thumb.webp",
            "orderIndex": 0
          }
        ],
        "publishedAt": "2026-08-03T13:00:00Z",
        "createdAt": "2026-08-03T12:55:00Z",
        "updatedAt": "2026-08-03T13:20:00Z",
        "currentUserReacted": true,
        "currentUserApplicationStatus": "pending",
        "canApply": false,
        "canEdit": false,
        "canModerate": false
      }
    ],
    "pageNumber": 1,
    "pageSize": 12,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

**Bài thảo luận (không tuyển người):**

```json
{
  "id": "d9f4b3c2-5e60-4f71-ac8d-9e0f1a2b3c4d",
  "author": {
    "id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "fullName": "Trần Thị B",
    "avatarUrl": null
  },
  "sportId": 2,
  "sportName": "Cầu lông",
  "postType": "discussion",
  "title": "Nên chọn vợt cầu lông nào cho người mới?",
  "content": "Tôi mới chơi được hai tháng, đang phân vân giữa Yonex Nanoflare và Lining...",
  "locationName": null,
  "address": null,
  "latitude": null,
  "longitude": null,
  "startAt": null,
  "endAt": null,
  "maxParticipants": null,
  "acceptedParticipants": 0,
  "slotsRemaining": null,
  "level": null,
  "feePerPerson": null,
  "status": "published",
  "allowComments": true,
  "commentCount": 8,
  "reactionCount": 3,
  "applicationCount": 0,
  "viewCount": 214,
  "media": [],
  "publishedAt": "2026-08-02T10:00:00Z",
  "createdAt": "2026-08-02T10:00:00Z",
  "updatedAt": "2026-08-02T10:00:00Z",
  "currentUserReacted": false,
  "currentUserApplicationStatus": null,
  "canApply": false,
  "canEdit": false,
  "canModerate": false
}
```

### 9.5 Field quan trọng cần lưu ý

| Field | Ghi chú |
|---|---|
| `content` | **Trả FULL content**, không có `contentPreview`. Frontend tự truncate cho card (khuyến nghị 180 ký tự). |
| `sportId` / `sportName` | Hai field phẳng, **không** phải object `sport: { id, name }`. `sportName` là `null` khi `sportId` null. |
| `slotsRemaining` | `null` khi `maxParticipants == null`. Ngược lại `max(0, maxParticipants - acceptedParticipants)`. |
| `acceptedParticipants` | Bài tuyển người khởi tạo = **1** (tính cả chủ bài). Bài khác = `0`. |
| `currentUserReacted` | **`boolean`**, không phải string `currentUserReaction`. MVP chỉ có `like`. |
| `currentUserApplicationStatus` | `"pending"` \| `"accepted"` \| `"rejected"` \| `"cancelled"` \| `null` |
| `canModerate` | **Luôn `false`** ở mọi endpoint community thường. Chỉ `true` ở endpoint admin. |
| `media[].mediaType` | `"image"` \| `"video"` |
| `media` | Đã sắp xếp theo `orderIndex` tăng dần bởi backend. |

### 9.6 `GET /api/community/posts/me` — bài của tôi

| Mục | Giá trị |
|---|---|
| Auth | **Bắt buộc** |
| Role | Bất kỳ |

Dùng **cùng bộ query parameter** như feed (`CommunityPostFilterRequest`).

Khác biệt so với feed:
- Lọc `authorId == currentUser` (bỏ qua param `authorId` trong filter — nó vẫn được apply thêm nhưng vô nghĩa).
- Trả **mọi status trừ `deleted`** → bao gồm cả `draft` và `hidden`.
- `followingOnly` **không có tác dụng** ở endpoint này.

> ⚠️ **Known limitation:** repository của `/me` **không** `Include(x => x.Author)`. Vì vậy trong
> response của `/me`, object `author` chỉ có `id` đúng, còn `fullName` là chuỗi rỗng `""` và
> `avatarUrl` là `null`.
> **Frontend phải dùng thông tin user đang đăng nhập từ session/profile store để hiển thị tác giả
> trên trang "Bài của tôi", không lấy từ `post.author`.**

Ví dụ response `/me` (chú ý `author`):

```json
{
  "id": "e0a5c4d3-6f71-4082-bd9e-0f1a2b3c4d5e",
  "author": { "id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c", "fullName": "", "avatarUrl": null },
  "sportId": 2,
  "sportName": "Cầu lông",
  "postType": "looking_for_players",
  "title": "Bản nháp chưa đăng",
  "content": "Đang soạn...",
  "locationName": null,
  "address": null,
  "latitude": null,
  "longitude": null,
  "startAt": "2026-08-20T12:00:00Z",
  "endAt": null,
  "maxParticipants": 4,
  "acceptedParticipants": 1,
  "slotsRemaining": 3,
  "level": null,
  "feePerPerson": null,
  "status": "draft",
  "allowComments": true,
  "commentCount": 0,
  "reactionCount": 0,
  "applicationCount": 0,
  "viewCount": 0,
  "media": [],
  "publishedAt": null,
  "createdAt": "2026-08-04T07:00:00Z",
  "updatedAt": "2026-08-04T07:00:00Z",
  "currentUserReacted": false,
  "currentUserApplicationStatus": null,
  "canApply": false,
  "canEdit": true,
  "canModerate": false
}
```

---

## 10. Community post detail

**Backend contract.**

### 10.1 `GET /api/community/posts/{id}`

| Mục | Giá trị |
|---|---|
| Auth | **Không bắt buộc** |
| Request body | `No request body` |
| Response | `Result<CommunityPostResponse>` — **cùng schema với post card ở mục 9.4** |

**Không có schema riêng cho detail.** Feed và detail dùng chung `CommunityPostResponse`.
Khác biệt duy nhất về dữ liệu: detail luôn `Include(Author)` và `Include(Sport)` nên `author.fullName`
luôn đúng.

### 10.2 Quy tắc hiển thị theo status

| Status của bài | Người xem là tác giả | Người xem khác / anonymous |
|---|---|---|
| `published` | ✅ 200 | ✅ 200 |
| `closed` | ✅ 200 | ✅ 200 |
| `expired` | ✅ 200 | ✅ 200 |
| `draft` | ✅ 200 | ❌ `404 COMMUNITY_POST_NOT_FOUND` |
| `hidden` | ✅ 200 | ❌ `404 COMMUNITY_POST_NOT_FOUND` |
| `deleted` | ❌ **404** | ❌ `404 COMMUNITY_POST_NOT_FOUND` |

> Bài `deleted` trả `404` cho **cả tác giả**. Chỉ endpoint admin mới xem được.

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "COMMUNITY_POST_NOT_FOUND",
    "message": "Post not found",
    "type": "NotFound",
    "details": null
  }
}
```

### 10.3 Side effect: tăng view count

Mỗi lần gọi `GET /api/community/posts/{id}` **thành công**, backend chạy
`UPDATE community_posts SET view_count = view_count + 1` (atomic, qua `ExecuteUpdateAsync`).

**Hệ quả cho frontend:**
- `viewCount` trong response là **giá trị TRƯỚC khi tăng** (backend đọc entity rồi mới update).
- Không có chống trùng: refresh trang 10 lần → +10 view. Kể cả chính tác giả xem cũng tính.
- **Frontend recommendation:** đừng gọi endpoint này trong `useEffect` không có dependency ổn định,
  và tránh refetch tự động quá dày (`refetchOnWindowFocus: false` cho query detail).

### 10.4 Các field quyền — backend trả gì, frontend phải tự suy gì

**Backend trả sẵn 3 field:**

| Field | Công thức chính xác trong `CommunityMappingExtensions.ToResponse` |
|---|---|
| `canEdit` | `currentUserId != null && post.authorId == currentUserId` |
| `canApply` | `currentUserId != null` **AND** không phải chủ bài **AND** `postType` thuộc nhóm recruitment **AND** `status == "published"` **AND** (`startAt == null` hoặc `startAt > now`) **AND** (`slotsRemaining == null` hoặc `> 0`) **AND** `currentUserApplicationStatus == null` |
| `canModerate` | **Luôn `false`** ở endpoint community; `true` chỉ ở `GET/PUT /api/admin/community/posts/...` |

**Backend KHÔNG trả các field sau — frontend phải tự suy:**

| Field cần | Công thức frontend recommendation |
|---|---|
| `canDelete` | `canEdit` (chỉ tác giả xóa được — service kiểm `post.authorId == userId`) |
| `canClose` | `canEdit && status === 'published'` |
| `canComment` | `isLoggedIn && allowComments && status !== 'deleted'` (backend chỉ chặn khi `!allowComments` hoặc post deleted) |
| `canMessageAuthor` | `isLoggedIn && currentUserId !== author.id` (kết quả thật phụ thuộc block/inactive → xử lý lỗi khi gọi API) |
| `canCancelApplication` | `currentUserApplicationStatus === 'pending' \|\| currentUserApplicationStatus === 'accepted'` |

> ⚠️ Đừng giả định tồn tại `canDelete`, `canClose`, `canComment`, `canMessageAuthor`, `canModerate:true`
> trong response community — chúng **không có** trong DTO.

### 10.5 Quy tắc `canEdit` vs. thực tế backend cho phép sửa

`canEdit` chỉ kiểm quyền sở hữu. Backend **còn** kiểm status khi thực sự `PUT`:

```
Cho phép sửa khi: status ∈ { draft, published }
Từ chối (409 COMMUNITY_POST_INVALID_STATUS) khi: status ∈ { closed, expired, hidden, deleted }
```

→ **Frontend recommendation:** nút "Sửa" nên hiện khi `canEdit && (status === 'draft' || status === 'published')`,
nếu không sẽ nhận `409` khi submit.

---

## 11. Create/update community post

**Backend contract.**

### 11.1 `POST /api/community/posts`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Role | Bất kỳ (**không** cần `coach`) |
| Điều kiện | `user.status` phải là `"active"`, nếu không → `403 COMMON_ACCOUNT_NOT_ACTIVE` |

**Request — `looking_for_players` (tuyển người):**

```json
{
  "postType": "looking_for_players",
  "sportId": 2,
  "title": "Tìm 2 người đánh cầu lông tối thứ Sáu",
  "content": "Nhóm hiện có hai người, trình độ trung bình, chơi lúc 19h-21h tại Quận 7.",
  "locationName": "Sân cầu lông Quận 7",
  "address": "123 Nguyễn Thị Thập, Quận 7, TP.HCM",
  "latitude": 10.735,
  "longitude": 106.721,
  "startAt": "2026-08-07T12:00:00Z",
  "endAt": "2026-08-07T14:00:00Z",
  "maxParticipants": 4,
  "level": "intermediate",
  "feePerPerson": 70000,
  "allowComments": true,
  "publish": true,
  "media": [
    {
      "mediaType": "image",
      "url": "https://cdn.sportico.example/community/court-1.webp",
      "thumbnailUrl": "https://cdn.sportico.example/community/court-1-thumb.webp",
      "mimeType": "image/webp",
      "fileSize": 245000,
      "width": 1080,
      "height": 1080,
      "durationSeconds": null
    }
  ]
}
```

**Request — `discussion` (thảo luận):**

```json
{
  "postType": "discussion",
  "sportId": 2,
  "title": "Nên chọn vợt cầu lông nào cho người mới?",
  "content": "Tôi mới chơi được hai tháng, đang phân vân giữa Yonex Nanoflare và Lining.",
  "locationName": null,
  "address": null,
  "latitude": null,
  "longitude": null,
  "startAt": null,
  "endAt": null,
  "maxParticipants": null,
  "level": null,
  "feePerPerson": null,
  "allowComments": true,
  "publish": true,
  "media": []
}
```

**Request — lưu nháp:**

```json
{
  "postType": "question",
  "sportId": null,
  "title": "Bản nháp",
  "content": "Đang soạn dở...",
  "allowComments": true,
  "publish": false
}
```

**Response:** `Result<CommunityPostResponse>` (schema mục 9.4). Với `publish: false` →
`status: "draft"`, `publishedAt: null`.

> ⚠️ Field `publish` **có tồn tại** trong `CreateCommunityPostRequest` và mặc định là `true`.
> `allowComments` mặc định `true`.
> Field `orderIndex` **không có** trong `CommunityPostMediaRequest` — backend tự gán theo thứ tự
> phần tử trong mảng (`0, 1, 2, …`).

### 11.2 Ma trận validation theo `postType`

`CommunityPostTypes.RecruitmentTypes` = `looking_for_players`, `looking_for_team`,
`training_partner`, `friendly_match`.

| Field | `looking_for_players` `looking_for_team` `training_partner` `friendly_match` | `event` | `discussion` | `question` |
|---|---|---|---|---|
| `postType` | **Bắt buộc** | **Bắt buộc** | **Bắt buộc** | **Bắt buộc** |
| `title` | **Bắt buộc**, ≤ 200 | **Bắt buộc**, ≤ 200 | **Bắt buộc**, ≤ 200 | **Bắt buộc**, ≤ 200 |
| `content` | **Bắt buộc**, ≤ 5000 | **Bắt buộc**, ≤ 5000 | **Bắt buộc**, ≤ 5000 | **Bắt buộc**, ≤ 5000 |
| `sportId` | **Bắt buộc** | Optional | Optional | Optional |
| `startAt` | **Bắt buộc** | Optional | Optional | Optional |
| `maxParticipants` | **Bắt buộc**, `>= 2` | Optional | Optional | Optional |
| `endAt` | Optional | Optional | Optional | Optional |
| `locationName` | Optional, ≤ 200 | Optional, ≤ 200 | Optional, ≤ 200 | Optional, ≤ 200 |
| `address` | Optional, ≤ 300 | Optional, ≤ 300 | Optional, ≤ 300 | Optional, ≤ 300 |
| `latitude` / `longitude` | Optional, **không có ràng buộc phạm vi** | Optional | Optional | Optional |
| `level` | Optional, ≤ 30 | Optional, ≤ 30 | Optional, ≤ 30 | Optional, ≤ 30 |
| `feePerPerson` | Optional, `>= 0` | Optional, `>= 0` | Optional, `>= 0` | Optional, `>= 0` |
| `allowComments` | Optional, default `true` | ⟵ | ⟵ | ⟵ |
| `publish` | Optional, default `true` | ⟵ | ⟵ | ⟵ |
| `media` | Optional, ≤ 8 item, ≤ 1 video | ⟵ | ⟵ | ⟵ |

**Rule chung áp dụng cho mọi loại:**
- `startAt < endAt` khi **cả hai** cùng có giá trị. Sai → `400`, message `"startAt must be before endAt"`.
- **Không có** rule ép `startAt` phải ở tương lai khi tạo bài. Bài quá khứ vẫn tạo được (worker sẽ
  chuyển sang `expired` sau tối đa 15 phút).
- `sportId` phải tồn tại trong bảng `sports`, nếu không → `404 SPORT_NOT_FOUND`.
- `level` là **string tự do ≤ 30 ký tự** — backend **không** validate theo enum.
  **Frontend recommendation:** dùng tập cố định `beginner` / `intermediate` / `advanced` / `all`
  và filter cũng dùng đúng các giá trị này (filter so sánh **bằng chính xác**).

**Message lỗi tiếng Anh chính xác từ validator** (để map sang tiếng Việt):

| Message backend | Nghĩa |
|---|---|
| `postType must be one of: looking_for_players, looking_for_team, training_partner, friendly_match, event, discussion, question` | Sai loại bài |
| `sportId is required for this post type` | Thiếu môn thể thao |
| `startAt is required for this post type` | Thiếu thời gian bắt đầu |
| `maxParticipants (>= 2, including the author) is required for this post type` | Thiếu/sai số người |
| `startAt must be before endAt` | Sai khoảng thời gian |
| `A post may have at most 8 media items` | Quá 8 media |
| `A post may have at most 1 video` | Quá 1 video |
| `mediaType must be 'image' or 'video'` | Sai loại media |
| `Media url must be an absolute https URL` | URL không phải HTTPS tuyệt đối |

### 11.3 `PUT /api/community/posts/{id}` — cập nhật

**Mọi field đều optional (nullable).** Field `null`/bỏ qua → **giữ nguyên**.

**Request:**

```json
{
  "title": "Tìm 2 người đánh cầu lông tối thứ Sáu (cập nhật giờ)",
  "content": "Đổi giờ sang 20h-22h.",
  "locationName": "Sân cầu lông Quận 7",
  "address": "123 Nguyễn Thị Thập, Quận 7, TP.HCM",
  "latitude": null,
  "longitude": null,
  "startAt": "2026-08-07T13:00:00Z",
  "endAt": "2026-08-07T15:00:00Z",
  "maxParticipants": 6,
  "level": "intermediate",
  "feePerPerson": 80000,
  "allowComments": true,
  "media": null
}
```

**Response:** `Result<CommunityPostResponse>`.

**Field KHÔNG thể sửa** (không có trong `UpdateCommunityPostRequest`):
`postType`, `sportId`, `publish` (không đổi được draft ⇄ published qua endpoint này).

**Validation update (`UpdateCommunityPostRequestValidator`):**

| Field | Rule |
|---|---|
| `title` | ≤ 200 (chỉ kiểm khi `!= null`) |
| `content` | ≤ 5000 (chỉ kiểm khi `!= null`) |
| `locationName` | ≤ 200 |
| `address` | ≤ 300 |
| `level` | ≤ 30 |
| `feePerPerson` | `>= 0` |
| `maxParticipants` | `>= 1` (**khác create: create yêu cầu `>= 2`**) |
| cặp ngày | `startAt < endAt` khi cả hai có giá trị |
| `media` | ≤ 8 item, ≤ 1 video, mỗi item phải HTTPS |

**Ràng buộc nghiệp vụ khi update:**

| Điều kiện | Kết quả |
|---|---|
| Không phải tác giả | `403 COMMUNITY_POST_NOT_OWNED` |
| `status ∉ { draft, published }` | `409 COMMUNITY_POST_INVALID_STATUS`, message `"This post can no longer be edited"` |
| `maxParticipants < acceptedParticipants` | `409 COMMUNITY_POST_INVALID_STATUS`, message `"maxParticipants cannot be lower than the N participant(s) already accepted"` |

**Side effect tự động:** nếu bài đang `closed` và sau update còn chỗ
(`maxParticipants == null` hoặc `acceptedParticipants < maxParticipants`) → tự chuyển về `published`.

> ⚠️ Nhưng bài `closed` **không sửa được** (bị chặn ở rule status ở trên). Nhánh tự-mở-lại này chỉ
> có tác dụng trong trường hợp biên. Thực tế: muốn mở lại bài `closed`, hiện **chưa có endpoint**.
> Xem [Known limitations](#35-known-limitations).

### 11.4 Xử lý media khi update

**Backend contract — quan trọng:**

| `media` trong request | Hành vi backend |
|---|---|
| `null` hoặc bỏ qua | **Giữ nguyên** toàn bộ media hiện tại |
| `[]` (mảng rỗng) | **XÓA HẾT** media (`post.Media.Clear()` rồi không thêm gì) |
| `[a, b, c]` | **THAY THẾ TOÀN BỘ**: xóa hết media cũ, thêm mới theo đúng thứ tự, `orderIndex` = 0,1,2 |

> ⚠️ **Không có** cơ chế sửa/xóa từng media riêng lẻ. Không có endpoint `DELETE /media/{id}`.
> Media cũ bị **xóa khỏi DB** (hard delete khỏi collection), file trên storage **không** bị đụng tới.
>
> **Frontend recommendation:** form sửa bài phải load media hiện tại vào state, cho user
> thêm/xóa/sắp xếp trong state, rồi gửi **toàn bộ mảng cuối cùng**. Nếu user không đụng vào media,
> hãy gửi `media: null` để tránh xóa nhầm.

### 11.5 `PUT /api/community/posts/{id}/close` — đóng bài

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc, phải là tác giả |
| Request body | `No request body` |
| Response | `Result<CommunityPostResponse>` |

| Điều kiện | Kết quả |
|---|---|
| Không phải tác giả | `403 COMMUNITY_POST_NOT_OWNED` |
| `status != "published"` | `409 COMMUNITY_POST_INVALID_STATUS`, message `"Only a published post can be closed"` |
| OK | `status → "closed"` |

### 11.6 `DELETE /api/community/posts/{id}` — xóa mềm

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc, phải là tác giả |
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "deleted": true }, "error": null }` |

- Là **soft delete**: `status → "deleted"`, `deletedAt = now`. Dữ liệu vẫn còn trong DB.
- **Idempotent**: gọi lại trên bài đã `deleted` → vẫn trả `200 { deleted: true }`.
- Xóa được ở **mọi status** (kể cả `closed`, `expired`) — chỉ cần là tác giả.
- Không phải tác giả → `403 COMMUNITY_POST_NOT_OWNED`.

### 11.7 Xử lý `409 CONCURRENCY_CONFLICT` khi update

Entity `CommunityPost` có cột `version` là **optimistic concurrency token**. Nếu hai request cùng
sửa một bài (hoặc bạn sửa trong khi có người đang được accept), `SaveChanges` ném
`DbUpdateConcurrencyException` → middleware trả `409` với code `CONCURRENCY_CONFLICT`.

**Frontend recommendation:**

```ts
if (err.code === 'CONCURRENCY_CONFLICT') {
  toast.error('Bài viết vừa được cập nhật bởi thao tác khác. Đang tải lại...');
  await queryClient.invalidateQueries({ queryKey: ['community-post', postId] });
  // KHÔNG tự động retry — hiển thị dữ liệu mới rồi để user quyết định.
}
```

> **Không** có field `version` trong request/response DTO. Frontend **không** cần (và không thể)
> gửi version lên. Backend tự quản lý.

---

## 12. Media handling

**Backend contract — đọc kỹ, đây là điểm dễ hiểu sai nhất.**

### 12.1 Backend hiện KHÔNG có upload endpoint

Đã kiểm tra toàn bộ controller: **không có** endpoint nào nhận `multipart/form-data`, **không có**
storage abstraction (S3/Supabase Storage/Cloudinary client) trong Application hay Infrastructure cho
module community/chat.

→ **Client phải tự upload file lên storage (Supabase Storage / Cloudinary / S3…) và chỉ gửi URL
kết quả cho backend.**

### 12.2 Backend nhận gì — `CommunityPostMediaRequest`

```json
{
  "mediaType": "image",
  "url": "https://cdn.sportico.example/community/court-1.webp",
  "thumbnailUrl": null,
  "mimeType": "image/webp",
  "fileSize": 245000,
  "width": 1080,
  "height": 1080,
  "durationSeconds": null
}
```

| Field | Kiểu | Bắt buộc | Backend validate? |
|---|---|:---:|---|
| `mediaType` | `string` | ✅ | ✅ phải là `"image"` hoặc `"video"` |
| `url` | `string` | ✅ | ✅ phải là **absolute URL, scheme = `https`** |
| `thumbnailUrl` | `string \| null` | ❌ | ❌ **không validate gì cả** |
| `mimeType` | `string \| null` | ❌ | ❌ **không validate** — lưu nguyên |
| `fileSize` | `number \| null` | ❌ | ❌ **không validate** — lưu nguyên (byte) |
| `width` | `number \| null` | ❌ | ❌ không validate |
| `height` | `number \| null` | ❌ | ❌ không validate |
| `durationSeconds` | `number \| null` | ❌ | ❌ không validate |

**Không có field `orderIndex` trong request** — backend gán tự động theo thứ tự mảng.

### 12.3 Giới hạn backend thực sự áp dụng

| Giới hạn | Giá trị | Vi phạm → |
|---|---|---|
| Số media / bài | **≤ 8** | `400`, `"A post may have at most 8 media items"` |
| Số video / bài | **≤ 1** | `400`, `"A post may have at most 1 video"` |
| Scheme của `url` | **`https` bắt buộc** | `400`, `"Media url must be an absolute https URL"` |
| Whitelist host | **KHÔNG CÓ** | — |
| Kiểm tra MIME thật | **KHÔNG CÓ** | — |
| Kiểm tra kích thước file thật | **KHÔNG CÓ** | — |
| Kiểm tra file có tồn tại | **KHÔNG CÓ** | — |
| Kiểm tra ownership file | **KHÔNG CÓ** | — |

### 12.4 ⚠️ Cảnh báo bảo mật bắt buộc đọc

> **Backend hiện KHÔNG xác minh ownership của file, KHÔNG whitelist host, KHÔNG kiểm tra MIME
> type hay kích thước thật của file.**
>
> Bất kỳ URL HTTPS nào cũng được chấp nhận và sẽ được render trên feed công khai.
>
> **Frontend BẮT BUỘC:**
> 1. **Chỉ gửi URL trả về từ storage chính thức của dự án.** Không bao giờ cho user dán URL tùy ý.
> 2. Validate MIME type và kích thước **ở phía client** trước khi upload.
> 3. Khi render, dùng `<img>`/`<video>` với `referrerPolicy="no-referrer"` và cân nhắc CSP
>    `img-src`/`media-src` chỉ cho phép domain storage của dự án.
> 4. Không render media trong `<iframe>` hoặc dùng `dangerouslySetInnerHTML`.

### 12.5 Giới hạn client-side khuyến nghị

**Frontend recommendation** (backend không ép, nhưng nên tự áp):

| Loại | MIME cho phép | Kích thước tối đa | Ghi chú |
|---|---|---|---|
| Ảnh | `image/jpeg`, `image/png`, `image/webp` | 5 MB | Nén/convert sang WebP trước khi upload |
| Video | `video/mp4`, `video/webm` | 50 MB | Nên sinh `thumbnailUrl` từ frame đầu |

- `thumbnailUrl` **không bắt buộc**. Với video, nếu `null`, frontend phải tự render poster mặc định.
- **Không gửi base64 trong JSON.** Không có giới hạn body size riêng nhưng đây là anti-pattern và
  cột `url` giới hạn độ dài ở DB.

### 12.6 Media trong response — `CommunityPostMediaResponse`

Response **chỉ trả 5 field** (ít hơn request):

```json
{
  "id": "c8e3a2b1-4d5f-4e60-9b7c-8d9e0f1a2b3c",
  "mediaType": "image",
  "url": "https://cdn.sportico.example/community/court-1.webp",
  "thumbnailUrl": "https://cdn.sportico.example/community/court-1-thumb.webp",
  "orderIndex": 0
}
```

> ⚠️ `mimeType`, `fileSize`, `width`, `height`, `durationSeconds` **được lưu vào DB nhưng KHÔNG
> trả về trong response**. Nếu frontend cần aspect-ratio để tránh layout shift, phải tự tính từ ảnh
> khi load hoặc dùng `aspect-ratio` CSS cố định.

Mảng `media` đã được backend sắp xếp theo `orderIndex` tăng dần.

### 12.7 Media của chat — khác hoàn toàn

Attachment của chat dùng DTO khác (`SendMessageAttachmentRequest`), xem [mục 20](#20-message-attachments).
Điểm khác quan trọng: chat attachment chấp nhận **cả `http` lẫn `https`**, community media **chỉ `https`**.

---

## 13. Comment và reply

**Backend contract.** Route prefix của controller là `/api/community`.

### 13.1 `GET /api/community/posts/{postId}/comments`

| Mục | Giá trị |
|---|---|
| Auth | **Không bắt buộc** |
| Request body | `No request body` |

**Query parameters:**

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

> Không có filter/sort nào khác. `CommunityCommentFilterRequest` chỉ có 2 field này.

**Cơ chế phân trang — quan trọng:**
- Chỉ **root comment** được phân trang (`parentCommentId == null`).
- **Reply KHÔNG phân trang** — toàn bộ reply của mỗi root comment được nhúng thẳng vào mảng
  `replies`, sắp xếp `createdAt ASC`.
- Root comment sắp xếp `createdAt DESC` (mới nhất trước).
- `totalCount` đếm **số root comment**, không tính reply.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
        "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "author": {
          "id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
          "fullName": "Trần Thị B",
          "avatarUrl": null
        },
        "parentCommentId": null,
        "content": "Tôi muốn tham gia buổi này, còn chỗ không bạn?",
        "status": "active",
        "replyCount": 2,
        "replies": [
          {
            "id": "a2b3c4d5-e6f7-4081-92a3-b4c5d6e7f8a9",
            "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
            "author": {
              "id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
              "fullName": "Nguyễn Văn A",
              "avatarUrl": "https://cdn.sportico.example/avatars/a.webp"
            },
            "parentCommentId": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
            "content": "Còn 2 chỗ nhé bạn, bấm Tham gia giúp mình.",
            "status": "active",
            "replyCount": 0,
            "replies": [],
            "canEdit": false,
            "canModerate": false,
            "createdAt": "2026-08-03T15:10:00Z",
            "updatedAt": "2026-08-03T15:10:00Z"
          },
          {
            "id": "b3c4d5e6-f7a8-4192-a3b4-c5d6e7f8a9b0",
            "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
            "author": {
              "id": "5b7c9d0e-1f2a-4304-9506-b7c8d9e0f1a2",
              "fullName": "Lê Văn C",
              "avatarUrl": null
            },
            "parentCommentId": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
            "content": "Bình luận đã bị xóa",
            "status": "deleted",
            "replyCount": 0,
            "replies": [],
            "canEdit": false,
            "canModerate": false,
            "createdAt": "2026-08-03T15:12:00Z",
            "updatedAt": "2026-08-03T16:00:00Z"
          }
        ],
        "canEdit": false,
        "canModerate": false,
        "createdAt": "2026-08-03T15:00:00Z",
        "updatedAt": "2026-08-03T15:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

**Điều kiện lỗi:**

| Điều kiện | Kết quả |
|---|---|
| Post không tồn tại | `404 COMMUNITY_POST_NOT_FOUND` |
| Post `status == "deleted"` | `404 COMMUNITY_POST_NOT_FOUND` |
| Post `status == "hidden"` | `404 COMMUNITY_POST_NOT_FOUND` (**kể cả tác giả**) |
| Post `draft`/`closed`/`expired` | ✅ Trả comment bình thường |

### 13.2 Comment bị xóa / bị ẩn hiển thị thế nào

| Trường hợp | Có trong response? | `content` trả về |
|---|---|---|
| Root comment `status == "deleted"` | ❌ **Bị loại hoàn toàn khỏi danh sách** | — |
| **Reply** `status == "deleted"` | ✅ Vẫn nằm trong `replies` | `"Bình luận đã bị xóa"` (chuỗi tiếng Việt cứng từ backend) |
| Comment `status == "hidden"` | ✅ Vẫn xuất hiện | ⚠️ **Nội dung THẬT** (xem limitation) |

> ⚠️ **Known limitation:** comment bị admin ẩn (`status == "hidden"`) **vẫn được trả về kèm nội dung
> gốc** cho endpoint công khai. Backend chỉ thay nội dung khi `status == "deleted"`.
>
> **Frontend BẮT BUỘC tự lọc/che:** khi `status === 'hidden'`, hiển thị placeholder
> "Bình luận đã bị ẩn bởi quản trị viên" thay vì `content`, hoặc ẩn hẳn khỏi danh sách.

```ts
// Frontend recommendation — bắt buộc
const displayContent = (c: CommunityCommentResponse) =>
  c.status === 'hidden' ? 'Bình luận đã bị ẩn bởi quản trị viên' : c.content;
```

### 13.3 `POST /api/community/posts/{postId}/comments` — bình luận gốc

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Điều kiện | User phải `active` |

**Request:**

```json
{
  "content": "Tôi muốn tham gia buổi này, còn chỗ không bạn?"
}
```

**Validation:** `content` — `NotEmpty`, `MaximumLength(2000)`.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "id": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
    "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
    "author": {
      "id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
      "fullName": "Trần Thị B",
      "avatarUrl": null
    },
    "parentCommentId": null,
    "content": "Tôi muốn tham gia buổi này, còn chỗ không bạn?",
    "status": "active",
    "replyCount": 0,
    "replies": [],
    "canEdit": true,
    "canModerate": false,
    "createdAt": "2026-08-03T15:00:00Z",
    "updatedAt": "2026-08-03T15:00:00Z"
  },
  "error": null
}
```

**Điều kiện lỗi:**

| Điều kiện | Error code | HTTP |
|---|---|---|
| User không tồn tại | `USER_NOT_FOUND` | 404 |
| `user.status != "active"` | `COMMON_ACCOUNT_NOT_ACTIVE` | 403 |
| Post không tồn tại / `deleted` | `COMMUNITY_POST_NOT_FOUND` | 404 |
| `post.allowComments == false` | `COMMUNITY_COMMENTS_DISABLED` | 409 |

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "COMMUNITY_COMMENTS_DISABLED",
    "message": "Comments are disabled on this post",
    "type": "Conflict",
    "details": null
  }
}
```

> ⚠️ Backend **cho phép** bình luận trên bài `hidden`, `closed`, `expired`, `draft` — chỉ chặn bài
> `deleted` và bài tắt comment. **Frontend recommendation:** ẩn ô nhập comment khi
> `status === 'closed' || status === 'expired'` để tránh UX kỳ lạ.

### 13.4 `POST /api/community/comments/{commentId}/replies` — trả lời

**Request:**

```json
{
  "content": "Còn 2 chỗ nhé bạn, bấm Tham gia giúp mình."
}
```

**Validation:** `content` — `NotEmpty`, `MaximumLength(2000)`.

**Response:** `Result<CommunityCommentResponse>` với `parentCommentId` = id comment cha.

**Quy tắc chỉ 1 cấp — quan trọng:**

Nếu `{commentId}` truyền vào **chính là một reply** (`parentCommentId != null`), backend **từ chối**:

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "COMMUNITY_COMMENT_NESTING_NOT_ALLOWED",
    "message": "Cannot reply to a reply — reply to the original comment instead",
    "type": "Conflict",
    "details": null
  }
}
```

HTTP `409`.

> Backend **KHÔNG tự chuẩn hóa** về root comment — nó **reject**. Frontend phải luôn gửi
> **id của root comment**, kể cả khi user bấm "Trả lời" trên một reply.
>
> ```ts
> // Frontend recommendation
> const replyTargetId = comment.parentCommentId ?? comment.id;  // luôn về root
> // và prefill "@FullName " vào ô nhập để giữ ngữ cảnh
> ```

**Điều kiện lỗi thêm:**

| Điều kiện | Error code | HTTP |
|---|---|---|
| Comment cha không tồn tại / `deleted` | `COMMUNITY_COMMENT_NOT_FOUND` | 404 |
| Comment cha là reply | `COMMUNITY_COMMENT_NESTING_NOT_ALLOWED` | 409 |
| Post của comment cha `deleted` | `COMMUNITY_POST_NOT_FOUND` | 404 |
| `post.allowComments == false` | `COMMUNITY_COMMENTS_DISABLED` | 409 |
| User không `active` | `COMMON_ACCOUNT_NOT_ACTIVE` | 403 |

**Side effect:** `parent.replyCount++` và `post.commentCount++` (reply **cũng** tính vào
`commentCount` của bài).

### 13.5 `PUT /api/community/comments/{commentId}` — sửa

**Request:**

```json
{
  "content": "Nội dung đã chỉnh sửa."
}
```

**Validation:** `content` — `NotEmpty`, `MaximumLength(2000)`.

| Điều kiện | Error code | HTTP |
|---|---|---|
| Comment không tồn tại / `deleted` | `COMMUNITY_COMMENT_NOT_FOUND` | 404 |
| Không phải tác giả comment | `COMMUNITY_COMMENT_NOT_OWNED` | 403 |

- **Không có giới hạn thời gian** để sửa comment.
- Comment `hidden` **vẫn sửa được** (chỉ chặn `deleted`).
- Chỉ tác giả comment sửa được. **Chủ bài KHÔNG sửa được comment của người khác.**
- Không có cờ `isEdited` — frontend so sánh `updatedAt !== createdAt` để hiện nhãn "(đã chỉnh sửa)".

### 13.6 `DELETE /api/community/comments/{commentId}` — xóa mềm

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "deleted": true }, "error": null }` |

| Điều kiện | Kết quả |
|---|---|
| Không tồn tại | `404 COMMUNITY_COMMENT_NOT_FOUND` |
| Không phải tác giả | `403 COMMUNITY_COMMENT_NOT_OWNED` |
| Đã `deleted` | `200 { deleted: true }` (idempotent) |
| OK | `status → "deleted"`, `deletedAt = now`, `post.commentCount--` (clamp ≥ 0) |

- **Soft delete** — record vẫn còn, reply con **không bị mất**.
- **Chủ bài KHÔNG xóa được comment của người khác** — chỉ tác giả comment hoặc admin.
- ⚠️ Xóa root comment → root đó **biến mất khỏi danh sách công khai cùng toàn bộ reply của nó**
  (vì query lọc `Status != deleted` ở cấp root). Reply vẫn tồn tại trong DB nhưng không hiển thị.

### 13.7 Ai làm được gì với comment

| Hành động | Tác giả comment | Chủ bài | Admin | Người khác |
|---|:---:|:---:|:---:|:---:|
| Xem | ✅ | ✅ | ✅ | ✅ |
| Sửa | ✅ | ❌ | ❌¹ | ❌ |
| Xóa mềm | ✅ | ❌ | ✅² | ❌ |
| Ẩn | ❌ | ❌ | ✅² | ❌ |
| Khôi phục | ❌ | ❌ | ✅² | ❌ |

¹ Admin không có endpoint sửa nội dung comment.
² Qua `/api/admin/community/comments/{id}/...`, xem [mục 16](#16-admin-community-management).

**Ý nghĩa 2 field quyền trong response:**

| Field | Công thức backend |
|---|---|
| `canEdit` | `status != "deleted" && comment.authorId == currentUserId` |
| `canModerate` | `= isPostOwner` — **nhưng endpoint community luôn truyền `false`**, chỉ endpoint admin truyền `true` |

> ⚠️ `canModerate` **luôn `false`** ở `GET /api/community/posts/{postId}/comments`, kể cả khi bạn
> là chủ bài. Đừng dùng nó để hiện nút xóa cho chủ bài — chủ bài **không có quyền** xóa comment
> người khác.

---

## 14. Like/reaction

**Backend contract.** MVP chỉ có một loại reaction: `like`.

### 14.1 `PUT /api/community/posts/{id}/like`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "liked": true }, "error": null }` |

### 14.2 `DELETE /api/community/posts/{id}/like`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "liked": false }, "error": null }` |

### 14.3 Idempotency — đã xác minh trong code

| Tình huống | Kết quả |
|---|---|
| Like bài chưa like | Tạo reaction, `reactionCount++`, trả `{ liked: true }` |
| Like bài **đã** like | **Không làm gì**, trả `{ liked: true }` — counter **không tăng lần 2** |
| Unlike bài đã like | Xóa reaction, `reactionCount--` (clamp ≥ 0), trả `{ liked: false }` |
| Unlike bài **chưa** like | **Không làm gì**, trả `{ liked: false }` — counter **không giảm** |

DB có `UNIQUE (post_id, user_id)` trên `community_post_reactions` + check constraint
`reaction_count >= 0`. Counter **không bao giờ âm**.

→ **Frontend có thể spam nút Like/Unlike an toàn.** Không cần debounce cho tính đúng đắn (vẫn nên
debounce để giảm tải mạng).

### 14.4 Điều kiện lỗi

| Endpoint | Điều kiện | Kết quả |
|---|---|---|
| Like | Post không tồn tại **hoặc** `status == "deleted"` | `404 COMMUNITY_POST_NOT_FOUND` |
| Unlike | Post không tồn tại | `404 COMMUNITY_POST_NOT_FOUND` |
| Unlike | Post `status == "deleted"` | ✅ **Vẫn cho unlike** (chỉ Like mới chặn `deleted`) |

> Like được phép trên bài `hidden`, `closed`, `expired`, `draft`. **Frontend recommendation:** ẩn nút
> Like trên bài `expired`/`closed` nếu muốn, đó là lựa chọn UX.

### 14.5 Đọc trạng thái like hiện tại

Không có endpoint riêng. Lấy từ `currentUserReacted` (boolean) trong `CommunityPostResponse`
(feed hoặc detail). Khi anonymous → luôn `false`.

> ⚠️ **Known limitation:** `MapListAsync` gọi 2 query cho **mỗi post** trong feed
> (`GetAsync` reaction + `GetByPostAndApplicantAsync` application) khi user đã đăng nhập → N+1.
> Với `pageSize = 100` là ~200 query phụ. **Frontend recommendation: dùng `pageSize` ≤ 20 cho feed.**

### 14.6 Optimistic UI cho Like

**Frontend recommendation** — đây là mutation **an toàn nhất** để làm optimistic (idempotent, counter
được clamp server-side):

```ts
const like = useMutation({
  mutationFn: () => api.likePost(postId),
  onMutate: async () => {
    await qc.cancelQueries({ queryKey: ['community-post', postId] });
    const prev = qc.getQueryData<CommunityPostResponse>(['community-post', postId]);
    qc.setQueryData(['community-post', postId], (o: CommunityPostResponse) => ({
      ...o, currentUserReacted: true, reactionCount: o.reactionCount + 1,
    }));
    return { prev };
  },
  onError: (_e, _v, ctx) => qc.setQueryData(['community-post', postId], ctx!.prev),
  onSettled: () => qc.invalidateQueries({ queryKey: ['community-post', postId] }),
});
```

---

## 15. Community applications

**Backend contract.**

### 15.1 State machine

```mermaid
stateDiagram-v2
    [*] --> pending: POST .../applications
    pending --> accepted: chủ bài PUT .../accept
    pending --> rejected: chủ bài PUT .../reject
    pending --> cancelled: applicant DELETE .../applications/me
    accepted --> cancelled: applicant DELETE .../applications/me
    rejected --> [*]: TERMINAL (không thể cancel)
    cancelled --> [*]: TERMINAL
    accepted --> [*]
```

| Transition | Ai thực hiện | Endpoint |
|---|---|---|
| `→ pending` | Applicant | `POST /api/community/posts/{postId}/applications` |
| `pending → accepted` | **Chủ bài** | `PUT /api/community/applications/{id}/accept` |
| `pending → rejected` | **Chủ bài** | `PUT /api/community/applications/{id}/reject` |
| `pending → cancelled` | Applicant | `DELETE /api/community/posts/{postId}/applications/me` |
| `accepted → cancelled` | Applicant | `DELETE /api/community/posts/{postId}/applications/me` |
| `rejected → *` | — | ❌ Không thể (409) |

### 15.2 `POST /api/community/posts/{postId}/applications` — xin tham gia

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Role | Bất kỳ |

**Request:**

```json
{
  "message": "Tôi chơi trình độ trung bình và có thể đến đúng giờ."
}
```

**Request không có lời nhắn (hợp lệ):**

```json
{ "message": null }
```

**Validation:** `message` — **optional**, `MaximumLength(500)`.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "id": "c4d5e6f7-a8b9-4203-b4c5-d6e7f8a9b0c1",
    "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
    "applicant": {
      "id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
      "fullName": "Trần Thị B",
      "avatarUrl": null
    },
    "message": "Tôi chơi trình độ trung bình và có thể đến đúng giờ.",
    "status": "pending",
    "createdAt": "2026-08-04T09:00:00Z",
    "respondedAt": null,
    "cancelledAt": null
  },
  "error": null
}
```

> `CommunityApplicationResponse` **không có** `respondedByUserId`. Field đó tồn tại trong entity/DB
> nhưng không được map ra response.

**Điều kiện kiểm tra — theo đúng thứ tự backend:**

| # | Điều kiện | Error code | HTTP |
|---|---|---|---|
| 1 | Post không tồn tại / `deleted` | `COMMUNITY_POST_NOT_FOUND` | 404 |
| 2 | Là chủ bài | `COMMUNITY_APPLICATION_NOT_ALLOWED` | **403** |
| 3 | `postType` không thuộc nhóm recruitment | `COMMUNITY_APPLICATION_NOT_ALLOWED` | **409** |
| 4 | `status != "published"` | `COMMUNITY_POST_NOT_PUBLISHED` | 409 |
| 5 | `startAt != null && startAt <= now` | `COMMUNITY_POST_EXPIRED` | 409 |
| 6 | `maxParticipants != null && acceptedParticipants >= maxParticipants` | `COMMUNITY_POST_FULL` | 409 |
| 7 | Đã có application (bất kỳ status nào) | `COMMUNITY_APPLICATION_ALREADY_EXISTS` | 409 |

> ⚠️ Chú ý: cùng error code `COMMUNITY_APPLICATION_NOT_ALLOWED` được trả với **2 HTTP status khác
> nhau** (403 khi là chủ bài, 409 khi sai loại bài). Frontend nên phân biệt bằng `error.type`
> (`"Forbidden"` vs `"Conflict"`) hoặc đơn giản là hiển thị `error.message`.

**Không thể apply lại sau `rejected`/`cancelled`:** DB có `UNIQUE (post_id, applicant_id)` và
service kiểm `existing != null` với **mọi** status → luôn `409 COMMUNITY_APPLICATION_ALREADY_EXISTS`.

> **Known limitation:** đây là hành vi hiện tại. Một user bị từ chối hoặc tự hủy **không bao giờ**
> apply lại được vào cùng bài đó. Frontend phải hiển thị rõ trạng thái cuối và disable nút.

**Side effect:** `post.applicationCount++`; gửi notification cho chủ bài
(`type: "system"`, title `"New join request"`).

> ⚠️ **Known limitation — block KHÔNG được kiểm tra ở đây.** `CommunityPostService.ApplyAsync`
> không gọi `IUserBlockRepository`. Một user đã bị chủ bài chặn **vẫn xin tham gia được**.
> (Chặn chỉ có tác dụng ở chat, xem [mục 21](#21-user-block).)

### 15.3 `DELETE /api/community/posts/{postId}/applications/me` — hủy đơn của mình

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "cancelled": true }, "error": null }` |

| Trạng thái hiện tại | Kết quả |
|---|---|
| Không có application | `404 COMMUNITY_APPLICATION_NOT_FOUND` |
| `cancelled` | `200 { cancelled: true }` (idempotent) |
| `rejected` | `409 COMMUNITY_APPLICATION_NOT_PENDING`, message `"This application was already rejected"` |
| `pending` | → `cancelled`. **Không** đụng tới `acceptedParticipants`. |
| `accepted` | → `cancelled`, `post.acceptedParticipants--` (clamp ≥ 0) + notification cho chủ bài |

**Tự mở lại bài khi người đã accepted rời đi:**

Khi hủy một application `accepted`, nếu bài đang `closed` **và** (`startAt == null` hoặc `startAt > now`)
→ bài tự chuyển về `published`.

> ⚠️ **Không có giới hạn thời gian.** User có thể hủy đơn `accepted` ngay cả sau khi hoạt động đã
> bắt đầu (chỉ khác là bài sẽ không tự mở lại).
>
> ⚠️ `post.applicationCount` **KHÔNG giảm** khi hủy. Nó là counter "tổng số đơn từng nhận".

### 15.4 `GET /api/community/posts/{postId}/applications` — chủ bài xem danh sách

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Quyền | **Chỉ chủ bài** |

**Query parameters:**

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `status` | `string \| null` | — | `pending` \| `accepted` \| `rejected` \| `cancelled` (**không được validate**) |
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

| Điều kiện | Kết quả |
|---|---|
| Post không tồn tại | `404 COMMUNITY_POST_NOT_FOUND` |
| Không phải chủ bài | `403 COMMUNITY_POST_NOT_OWNED`, message `"Only the post owner can view applications"` |

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "c4d5e6f7-a8b9-4203-b4c5-d6e7f8a9b0c1",
        "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "applicant": {
          "id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
          "fullName": "Trần Thị B",
          "avatarUrl": null
        },
        "message": "Tôi chơi trình độ trung bình và có thể đến đúng giờ.",
        "status": "pending",
        "createdAt": "2026-08-04T09:00:00Z",
        "respondedAt": null,
        "cancelledAt": null
      },
      {
        "id": "d5e6f7a8-b9c0-4314-c5d6-e7f8a9b0c1d2",
        "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "applicant": {
          "id": "5b7c9d0e-1f2a-4304-9506-b7c8d9e0f1a2",
          "fullName": "Lê Văn C",
          "avatarUrl": "https://cdn.sportico.example/avatars/c.webp"
        },
        "message": null,
        "status": "accepted",
        "createdAt": "2026-08-04T08:30:00Z",
        "respondedAt": "2026-08-04T08:45:00Z",
        "cancelledAt": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 2,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

> **Không có endpoint "đơn của tôi"** (kiểu `GET /api/community/applications/me`). Applicant chỉ
> biết trạng thái đơn của mình qua field `currentUserApplicationStatus` trên post detail/feed.

### 15.5 `PUT /api/community/applications/{id}/accept` và `/reject`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Quyền | **Chỉ chủ bài** |
| Request body | `No request body` |
| Response | `Result<CommunityApplicationResponse>` |

| Điều kiện | Error code | HTTP |
|---|---|---|
| Application không tồn tại | `COMMUNITY_APPLICATION_NOT_FOUND` | 404 |
| Post của application không tồn tại | `COMMUNITY_POST_NOT_FOUND` | 404 |
| Không phải chủ bài | `COMMUNITY_POST_NOT_OWNED` | 403 |
| `status != "pending"` | `COMMUNITY_APPLICATION_NOT_PENDING` | 409 |
| (accept) Bài đã đầy | `COMMUNITY_POST_FULL` | 409 |

**Khi `accept` thành công:**
1. `application.status → "accepted"`, `respondedAt = now`, `respondedByUserId = ownerId`
2. `post.acceptedParticipants++`
3. Nếu `acceptedParticipants >= maxParticipants` → `post.status → "closed"` (**tự động**)
4. `post.version++` (concurrency token)
5. Notification cho applicant

**Khi `reject` thành công:**
1. `application.status → "rejected"`, `respondedAt = now`, `respondedByUserId = ownerId`
2. **Không** đụng `acceptedParticipants`, **không** đổi status bài
3. Notification cho applicant

**Concurrency:** hai request accept đồng thời tranh slot cuối → một thành công, một nhận
**`409 CONCURRENCY_CONFLICT`** (từ `DbUpdateConcurrencyException`, vì `post.Version` là concurrency
token) hoặc **`409 COMMUNITY_POST_FULL`** (nếu request thứ hai đọc sau khi request đầu commit).
`acceptedParticipants` **không bao giờ** vượt `maxParticipants`.

```ts
// Frontend recommendation
if (err.code === 'CONCURRENCY_CONFLICT' || err.code === 'COMMUNITY_POST_FULL') {
  toast.error('Bài viết vừa thay đổi. Đang tải lại danh sách...');
  await qc.invalidateQueries({ queryKey: ['community-applications', postId] });
  await qc.invalidateQueries({ queryKey: ['community-post', postId] });
}
```

### 15.6 Query cần invalidate sau accept/reject

**Frontend recommendation:**

| Sau khi | Invalidate |
|---|---|
| Accept | `['community-applications', postId]`, `['community-post', postId]`, `['community-posts']` (feed — vì `acceptedParticipants`/`status` đổi) |
| Reject | `['community-applications', postId]` |
| Apply | `['community-post', postId]` (đổi `currentUserApplicationStatus`, `canApply`, `applicationCount`) |
| Cancel | `['community-post', postId]`, `['community-posts']` |

### 15.7 Bảng hiển thị nút "Tham gia"

**Frontend recommendation** — dựa trên field thật của `CommunityPostResponse`:

| Điều kiện (đánh giá theo thứ tự) | Nút hiển thị | Trạng thái |
|---|---|---|
| Chưa đăng nhập | "Đăng nhập để tham gia" | Enabled → mở modal login |
| `canEdit === true` (là chủ bài) | "Quản lý người tham gia" | Enabled → `/community/posts/{id}/applications` |
| `!isRecruitmentType(postType)` | *(không hiện nút nào)* | — |
| `status === 'expired'` | "Đã kết thúc" | Disabled |
| `status === 'closed'` | "Đã đủ người" | Disabled |
| `status === 'hidden' \|\| status === 'draft'` | *(không hiện)* | — |
| `currentUserApplicationStatus === 'pending'` | "Hủy yêu cầu" | Enabled → `DELETE .../applications/me` |
| `currentUserApplicationStatus === 'accepted'` | "Rời buổi chơi" | Enabled → `DELETE .../applications/me` |
| `currentUserApplicationStatus === 'rejected'` | "Đã bị từ chối" | Disabled (không apply lại được) |
| `currentUserApplicationStatus === 'cancelled'` | "Bạn đã hủy yêu cầu" | Disabled (không apply lại được) |
| `slotsRemaining === 0` | "Đã đủ người" | Disabled |
| `canApply === true` | **"Tham gia"** | Enabled → mở modal nhập lời nhắn |
| Còn lại | "Không thể tham gia" | Disabled |

```ts
// Frontend recommendation — helper
const RECRUITMENT_TYPES = ['looking_for_players','looking_for_team','training_partner','friendly_match'] as const;
const isRecruitment = (t: CommunityPostType) => (RECRUITMENT_TYPES as readonly string[]).includes(t);
```

---

## 16. Admin community management

**Backend contract.** Base route `/api/admin/community`. **Toàn bộ yêu cầu role `admin`.**

### 16.1 `GET /api/admin/community/posts` — danh sách quản trị

**Query parameters:**

| Param | Kiểu | Mặc định | Hành vi |
|---|---|---|---|
| `status` | `string \| null` | — | So sánh bằng chính xác. **Không validate** — giá trị lạ → danh sách rỗng |
| `postType` | `string \| null` | — | So sánh bằng chính xác. **Không validate** |
| `sportId` | `int \| null` | — | |
| `authorId` | `Guid \| null` | — | |
| `keyword` | `string \| null` | — | `ILIKE` trên `title` **OR** `content` |
| `reportedOnly` | `bool` | `false` | Chỉ bài có report `pending` hoặc `reviewing` |
| `fromDate` | `DateTime \| null` | — | Lọc theo **`createdAt >= fromDate`** (khác feed: feed lọc `startAt`) |
| `toDate` | `DateTime \| null` | — | Lọc theo **`createdAt <= toDate`** |
| `sortBy` | `string` | `"latest"` | Chỉ hỗ trợ `most_discussed`; **mọi giá trị khác** → `createdAt DESC` |
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

> ⚠️ Chỉ `pageNumber`/`pageSize` được validate. `status`, `postType`, `sortBy` **không** validate →
> không bao giờ trả `400` vì giá trị lạ. **Frontend phải tự giới hạn dropdown.**

**Danh sách admin trả về MỌI status**, bao gồm `draft`, `hidden`, `deleted`.

**Response — dùng `AdminCommunityPostResponse` (schema RÚT GỌN, khác `CommunityPostResponse`):**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "author": {
          "id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
          "fullName": "Nguyễn Văn A",
          "avatarUrl": "https://cdn.sportico.example/avatars/a.webp"
        },
        "postType": "looking_for_players",
        "title": "Tìm 2 người đánh cầu lông tối thứ Sáu",
        "status": "hidden",
        "moderationReason": "Nội dung quảng cáo không liên quan",
        "reportCount": 3,
        "commentCount": 5,
        "reactionCount": 12,
        "applicationCount": 3,
        "createdAt": "2026-08-03T12:55:00Z",
        "publishedAt": "2026-08-03T13:00:00Z",
        "hiddenAt": "2026-08-04T07:30:00Z",
        "deletedAt": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

**`AdminCommunityPostResponse` chỉ có 13 field.** Không có `content`, `sportId`, `sportName`,
`startAt`, `media`, `viewCount`, `slotsRemaining`, `canEdit`… → muốn xem đầy đủ phải gọi detail.

**`reportCount`** = số report có `status ∈ { pending, reviewing }` trỏ vào bài đó (không đếm report
đã resolved/rejected).

> ⚠️ **Known limitation:** `reportCount` được tính bằng **một query riêng cho từng bài** trong vòng
> lặp (N+1). Với `pageSize = 100` → 100 query phụ. **Frontend recommendation: `pageSize` ≤ 20 cho
> danh sách admin.**

### 16.2 `GET /api/admin/community/posts/{id}` — chi tiết quản trị

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `Result<CommunityPostResponse>` — **schema ĐẦY ĐỦ** (giống mục 9.4) với `canModerate: true` |

- **Xem được mọi status**, kể cả `deleted` và `hidden` (khác endpoint công khai).
- Không tăng `viewCount` (khác endpoint công khai).
- `canEdit` = `false` (vì `currentUserId` truyền vào là `null`).
- `currentUserReacted` = `false`, `currentUserApplicationStatus` = `null`.
- `canModerate` = **`true`** — đây là chỗ duy nhất field này bằng `true`.
- Không tìm thấy → `404 COMMUNITY_POST_NOT_FOUND`.

### 16.3 `PUT /api/admin/community/posts/{id}/hide` — ẩn bài

**Request:**

```json
{
  "reason": "Nội dung quảng cáo không liên quan đến thể thao"
}
```

**Validation (`HideContentRequestValidator`):** `reason` — **`NotEmpty`** (bắt buộc),
`MaximumLength(1000)`.

**Response:** `Result<CommunityPostResponse>` với `canModerate: true`.

**Hành vi:**
- Nếu `status != "hidden"`: `status → "hidden"`, `hiddenByUserId = adminId`, `hiddenAt = now`,
  `moderationReason = reason` → gửi notification cho tác giả (`type: "report"`,
  title `"Your post was hidden"`, content = `reason`).
- Nếu `status == "hidden"` rồi: **no-op** (không ghi DB, không gửi notification lại), vẫn trả `200`.
- **Ẩn được bài ở MỌI status**, kể cả `expired`, `closed`, `deleted`, `draft`.

### 16.4 `PUT /api/admin/community/posts/{id}/restore` — khôi phục

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `Result<CommunityPostResponse>` với `canModerate: true` |

**Hành vi — quan trọng:**

| Status hiện tại | Kết quả |
|---|---|
| `hidden` **hoặc** `deleted` | Khôi phục (xem bên dưới) |
| Mọi status khác | **No-op**, trả `200` với bài nguyên trạng |

Khi khôi phục:
```
status → publishedAt != null ? "published" : "draft"
hiddenByUserId → null
hiddenAt → null
moderationReason → null
deletedAt → null
```

> ⚠️ **Backend KHÔNG lưu status trước khi ẩn.** Một bài `closed` hoặc `expired` bị ẩn rồi khôi phục
> sẽ trở thành **`published`**, không quay về `closed`/`expired`.
> Bài `expired` được khôi phục sẽ lại thành `published` cho tới lần chạy worker tiếp theo (≤ 15 phút)
> rồi tự về `expired`.
>
> **Restore cho `hidden` và cho `deleted` là CÙNG một logic** — không phân biệt.
> Restore một bài `deleted` sẽ đưa nó trở lại feed công khai.

### 16.5 `DELETE /api/admin/community/posts/{id}` — xóa mềm (admin)

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "deleted": true }, "error": null }` |

**Hành vi:**
- Nếu `status != "deleted"`: `status → "deleted"`, `deletedAt = now`,
  `hiddenByUserId ??= adminId` (chỉ gán nếu đang null) → notification cho tác giả
  (`"Your post was removed"`, content cứng: `"Your post violated community guidelines and was removed by an admin."`).
- Nếu đã `deleted`: **no-op**, vẫn `200`.
- **Không có `reason` trong request** — khác với `hide`. `moderationReason` **không** được set.

> ⚠️ **Frontend recommendation:** vì delete không nhận lý do, nếu cần ghi nhận lý do hãy `hide`
> (có reason) trước rồi mới `delete`, hoặc dùng luồng resolve report với `actionTaken: "post_deleted"`
> + `resolutionNote`.

### 16.6 Hide vs. Delete — khác nhau thế nào

| | `hide` | `delete` |
|---|---|---|
| `status` sau đó | `"hidden"` | `"deleted"` |
| Cần `reason`? | ✅ Bắt buộc | ❌ Không nhận |
| `moderationReason` | Được set | Không đổi |
| `hiddenAt` | Được set | Không đổi |
| `deletedAt` | Không đổi | Được set |
| Notification tác giả | ✅ (nội dung = reason) | ✅ (nội dung cố định) |
| Trong feed công khai | ❌ Ẩn | ❌ Ẩn |
| Tác giả xem detail được? | ✅ **Được** | ❌ `404` |
| Comment của bài xem được? | ❌ `404` | ❌ `404` |
| Admin xem được? | ✅ | ✅ |
| Khôi phục được? | ✅ | ✅ (cùng endpoint) |

### 16.7 `GET /api/admin/community/posts/{id}/comments`

**Query parameters:** `pageNumber` (default 1), `pageSize` (default 20, `1..100`).

**Khác endpoint công khai:**
- Trả **TẤT CẢ** comment của bài — cả root lẫn reply nằm **phẳng cùng cấp** trong `items`,
  sort `createdAt DESC`.
- Bao gồm cả comment `hidden` và `deleted`.
- Mảng `replies` của mỗi item luôn **rỗng** (repository không `Include(Replies)`).
- `canEdit` = `false` (bị ép cứng), `canModerate` = `true`.
- Comment `deleted` vẫn trả `content: "Bình luận đã bị xóa"` (không phải nội dung gốc).

> ⚠️ **Known limitation:** admin **không xem được nội dung gốc** của comment đã bị xóa mềm qua API
> này — mapping thay content bằng placeholder cho mọi caller. Nội dung gốc vẫn còn trong DB.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "a2b3c4d5-e6f7-4081-92a3-b4c5d6e7f8a9",
        "postId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "author": {
          "id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
          "fullName": "Nguyễn Văn A",
          "avatarUrl": "https://cdn.sportico.example/avatars/a.webp"
        },
        "parentCommentId": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
        "content": "Còn 2 chỗ nhé bạn.",
        "status": "hidden",
        "replyCount": 0,
        "replies": [],
        "canEdit": false,
        "canModerate": true,
        "createdAt": "2026-08-03T15:10:00Z",
        "updatedAt": "2026-08-04T07:40:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

### 16.8 Moderation comment

| Endpoint | Body | Response |
|---|---|---|
| `PUT /api/admin/community/comments/{id}/hide` | `{ "reason": "Ngôn từ xúc phạm" }` | `Result<CommunityCommentResponse>` |
| `PUT /api/admin/community/comments/{id}/restore` | `No request body` | `Result<CommunityCommentResponse>` |
| `DELETE /api/admin/community/comments/{id}` | `No request body` | `{ "deleted": true }` |

**Hide:** `reason` bắt buộc, ≤ 1000. Nếu chưa `hidden` → set `status/hiddenByUserId/hiddenAt/moderationReason`
+ notification cho tác giả comment. Nếu đã `hidden` → no-op.

**Restore:** nếu `status ∈ { hidden, deleted }` → `status → "active"`, xóa
`hiddenByUserId`/`hiddenAt`/`moderationReason`/`deletedAt`. Ngược lại no-op.

**Delete:** nếu chưa `deleted` → `status → "deleted"`, `deletedAt = now`, `hiddenByUserId ??= adminId`.
**Không gửi notification.** **Không giảm `post.commentCount`** (khác với khi tác giả tự xóa).

> ⚠️ **Known limitation:** admin xóa comment → `post.commentCount` **không giảm** → số hiển thị trên
> card bài sẽ cao hơn số comment thực tế. Frontend không sửa được điều này.

Comment không tồn tại → `404 COMMUNITY_COMMENT_NOT_FOUND` ở cả 3 endpoint.

### 16.9 Ma trận action theo trạng thái (admin)

**Backend contract:**

| Status bài | Hide | Restore | Delete |
|---|:---:|:---:|:---:|
| `draft` | ✅ | ⚪ no-op | ✅ |
| `published` | ✅ | ⚪ no-op | ✅ |
| `closed` | ✅ | ⚪ no-op | ✅ |
| `expired` | ✅ | ⚪ no-op | ✅ |
| `hidden` | ⚪ no-op | ✅ | ✅ |
| `deleted` | ✅ (đổi sang hidden!) | ✅ | ⚪ no-op |

⚪ = không lỗi, trả `200`, nhưng không thay đổi gì.

> ⚠️ Lưu ý ô cuối cột Hide: gọi `hide` trên bài đang `deleted` sẽ **đổi status thành `hidden`** và
> `deletedAt` vẫn giữ giá trị cũ → trạng thái hỗn hợp. **Frontend recommendation: disable nút Hide
> khi `status === 'deleted'`.**

**Frontend recommendation** — nút nên hiện:

| Status | Nút |
|---|---|
| `draft` / `published` / `closed` / `expired` | **Ẩn bài** · **Xóa bài** |
| `hidden` | **Khôi phục** · **Xóa bài** |
| `deleted` | **Khôi phục** |

---

## 17. Report và moderation

**Backend contract.** Module report dùng **chung bảng `Report`** với report review sẵn có —
không phải hệ thống thứ hai.

### 17.1 Target types

`ReportTargetTypes` có 5 giá trị:

```
user | review | community_post | community_comment | chat_message
```

Nhưng `POST /api/reports` **chỉ chấp nhận 3**:

```
community_post | community_comment | chat_message
```

Gửi `user` hoặc `review` → `400 COMMON_VALIDATION_ERROR` với detail
`"targetType must be one of: community_post, community_comment, chat_message"`.

> `review` được report qua endpoint riêng có sẵn: `POST /api/reviews/{id}/report`.

### 17.2 `POST /api/reports` — tạo report

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Role | Bất kỳ |

**Request:**

```json
{
  "targetType": "community_post",
  "targetId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
  "reason": "spam",
  "description": "Bài đăng lặp lại nhiều lần với nội dung quảng cáo."
}
```

**Request report comment:**

```json
{
  "targetType": "community_comment",
  "targetId": "f1a2b3c4-d5e6-4f70-8192-a3b4c5d6e7f8",
  "reason": "harassment",
  "description": null
}
```

**Request report tin nhắn chat:**

```json
{
  "targetType": "chat_message",
  "targetId": "e7f8a9b0-c1d2-4435-a6b7-c8d9e0f1a2b3",
  "reason": "inappropriate_content",
  "description": "Gửi nội dung không phù hợp trong phòng chat."
}
```

**Validation (`CreateReportRequestValidator`):**

| Field | Rule |
|---|---|
| `targetType` | `NotEmpty`, phải thuộc 3 giá trị ở trên |
| `targetId` | `NotEmpty` (khác `Guid.Empty`) |
| `reason` | `NotEmpty`, `MaximumLength(200)` |
| `description` | Optional, `MaximumLength(1000)` |

> `reason` là **string tự do**, backend **không** ép theo enum.
> **Frontend recommendation:** dùng tập cố định để thống kê được:
> `spam` · `harassment` · `inappropriate_content` · `fake_information` · `scam` · `other`.

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "id": "f8a9b0c1-d2e3-4546-b7c8-d9e0f1a2b3c4",
    "reporterId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "targetType": "community_post",
    "targetId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
    "reason": "spam",
    "description": "Bài đăng lặp lại nhiều lần với nội dung quảng cáo.",
    "status": "pending",
    "handledByUserId": null,
    "handledAt": null,
    "resolutionNote": null,
    "actionTaken": null,
    "createdAt": "2026-08-04T10:00:00Z"
  },
  "error": null
}
```

### 17.3 Xác minh target và chống trùng

| `targetType` | Backend có kiểm tra target tồn tại? |
|---|---|
| `community_post` | ✅ Có — không tồn tại → `404 COMMUNITY_POST_NOT_FOUND` |
| `community_comment` | ✅ Có — không tồn tại → `404 COMMUNITY_COMMENT_NOT_FOUND` |
| `chat_message` | ❌ **KHÔNG kiểm tra gì cả** |

> ⚠️ **Known limitation:** với `chat_message`, backend **không** kiểm tra message có tồn tại,
> **không** kiểm tra reporter có thuộc phòng chat đó hay không. Bất kỳ Guid nào cũng được chấp nhận.
> Code comment trong `CommunityReportService` ghi nhận rõ điều này.
>
> **Frontend recommendation:** chỉ cho phép report chat message từ chính UI phòng chat mà user
> đang tham gia, lấy `targetId` từ `message.id` có sẵn trên màn hình.

**Chống trùng — idempotent:**

Backend gọi `GetOpenReportAsync(targetType, targetId, reporterId)`. Nếu **cùng reporter** đã có
report **đang mở** (`pending`/`reviewing`) cho **cùng target** → trả về report cũ với `200`,
**không tạo bản mới**.

→ Frontend bấm "Báo cáo" nhiều lần an toàn. Nhưng lưu ý: người dùng sẽ thấy `createdAt` là của lần
báo cáo đầu tiên.

**Không chặn tự report chính mình.** Backend không so sánh `reporterId` với chủ sở hữu nội dung.
Một user **có thể** report chính bài/comment của mình.

### 17.4 `GET /api/admin/community/reports` — danh sách report (admin)

**Query parameters:**

| Param | Kiểu | Mặc định | Ghi chú |
|---|---|---|---|
| `targetType` | `string \| null` | — | **Không validate** |
| `status` | `string \| null` | — | `pending` \| `reviewing` \| `resolved` \| `rejected`. **Không validate** |
| `pageNumber` | `int` | `1` | ⚠️ **Không validate** ở endpoint này |
| `pageSize` | `int` | `20` | ⚠️ **Không validate** ở endpoint này |

> ⚠️ `AdminCommunityService.GetReportsAsync` **không chạy validator nào**. Gửi `pageSize=100000`
> sẽ không bị chặn. **Frontend phải tự giới hạn `pageSize` ≤ 100.**

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "f8a9b0c1-d2e3-4546-b7c8-d9e0f1a2b3c4",
        "reporterId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
        "targetType": "community_post",
        "targetId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
        "reason": "spam",
        "description": "Bài đăng lặp lại nhiều lần.",
        "status": "pending",
        "handledByUserId": null,
        "handledAt": null,
        "resolutionNote": null,
        "actionTaken": null,
        "createdAt": "2026-08-04T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

> `ReportResponse` **chỉ có `reporterId`** (Guid), **không** có object `reporter` với tên/avatar.
> Muốn hiển thị tên người báo cáo, frontend phải gọi API user riêng.
>
> `targetId` là `Guid | null` trong DTO (vì report `user` legacy có thể null), nhưng với 3 loại mới
> nó luôn có giá trị.

### 17.5 `PUT /api/admin/community/reports/{id}/resolve` — xử lý report

**Request:**

```json
{
  "status": "resolved",
  "resolutionNote": "Đã xác minh nội dung vi phạm chính sách quảng cáo, tiến hành ẩn bài.",
  "actionTaken": "post_hidden"
}
```

**Request từ chối báo cáo:**

```json
{
  "status": "rejected",
  "resolutionNote": "Nội dung không vi phạm chính sách cộng đồng.",
  "actionTaken": "none"
}
```

**Validation (`ResolveReportRequestValidator`):**

| Field | Rule |
|---|---|
| `status` | Phải là `"resolved"` **hoặc** `"rejected"` |
| `resolutionNote` | Optional, `MaximumLength(1000)` |
| `actionTaken` | ⚠️ **KHÔNG được validate.** Mặc định `"none"` |

**Giá trị `actionTaken` backend thực sự xử lý** (`ReportActions`):

| Giá trị | Backend làm gì |
|---|---|
| `"none"` | Không làm gì thêm |
| `"post_hidden"` | Gọi `HidePostAsync(targetId, reason = resolutionNote ?? report.reason)` |
| `"post_deleted"` | Gọi `DeletePostAsync(targetId)` |
| `"comment_hidden"` | Gọi `HideCommentAsync(targetId, reason = resolutionNote ?? report.reason)` |
| `"comment_deleted"` | Gọi `DeleteCommentAsync(targetId)` |
| `"review_hidden"` / `"review_deleted"` | ⚠️ **Có trong constants nhưng KHÔNG được xử lý** ở service này |
| Giá trị bất kỳ khác | Được lưu vào DB nhưng **không kích hoạt hành động nào** |

> ⚠️ Backend **không kiểm tra** `actionTaken` có khớp `targetType` hay không. Gửi
> `actionTaken: "post_hidden"` cho report `community_comment` → backend sẽ thử hide một **post** có
> id = id của comment → gần như chắc chắn `404 COMMUNITY_POST_NOT_FOUND`.
>
> **Frontend BẮT BUỘC** giới hạn lựa chọn `actionTaken` theo `targetType`:
>
> | `targetType` | `actionTaken` cho phép |
> |---|---|
> | `community_post` | `none` · `post_hidden` · `post_deleted` |
> | `community_comment` | `none` · `comment_hidden` · `comment_deleted` |
> | `chat_message` | **chỉ** `none` |

**Response:** `Result<ReportResponse>` (bản đã cập nhật).

```json
{
  "isSuccess": true,
  "data": {
    "id": "f8a9b0c1-d2e3-4546-b7c8-d9e0f1a2b3c4",
    "reporterId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "targetType": "community_post",
    "targetId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
    "reason": "spam",
    "description": "Bài đăng lặp lại nhiều lần.",
    "status": "resolved",
    "handledByUserId": "0a1b2c3d-4e5f-4061-8273-8495a6b7c8d9",
    "handledAt": "2026-08-04T11:00:00Z",
    "resolutionNote": "Đã xác minh nội dung vi phạm chính sách quảng cáo, tiến hành ẩn bài.",
    "actionTaken": "post_hidden",
    "createdAt": "2026-08-04T10:00:00Z"
  },
  "error": null
}
```

**Điều kiện lỗi:**

| Điều kiện | Error code | HTTP |
|---|---|---|
| Report không tồn tại | `REPORT_NOT_FOUND` | 404 |
| Report đã `resolved` hoặc `rejected` | `REPORT_NOT_FOUND` ⚠️ | **409** |

> ⚠️ **Điểm dễ nhầm:** report đã xử lý trả **code `REPORT_NOT_FOUND` nhưng HTTP `409`** với message
> `"This report has already been handled"`. Frontend **phải phân biệt bằng HTTP status hoặc
> `error.type`**, không chỉ bằng `error.code`:
>
> ```ts
> if (err.code === 'REPORT_NOT_FOUND') {
>   if (err.status === 409) toast.error('Báo cáo này đã được xử lý trước đó.');
>   else                    toast.error('Không tìm thấy báo cáo.');
> }
> ```

**Thứ tự thực thi:** backend lưu report **trước**, rồi mới thực hiện `actionTaken`. Nếu hành động
moderation fail (ví dụ target không tồn tại), report **đã** được đánh dấu `resolved` và request trả
lỗi. Frontend nên invalidate cả danh sách report lẫn danh sách bài sau khi gặp lỗi này.

---

## 18. User-to-user chat

**Backend contract.** Base route `/api/chat`. **Toàn bộ yêu cầu đăng nhập, không giới hạn role.**

### 18.1 Thay đổi contract so với chat cũ

| | Trước | Bây giờ |
|---|---|---|
| Field tạo phòng | `coachId` (bắt buộc) | `targetUserId` (ưu tiên), `coachId` (legacy, vẫn nhận) |
| Đối tượng chat | Chỉ coach | **Mọi user `active`** |
| Trạng thái phòng | Không có | `pending` \| `active` \| `rejected` |
| Ngữ cảnh | Không có | `sourceType` + `sourceId` |
| Tin nhắn | Chỉ `content` | `content` **hoặc/và** `attachments[]` |

**Backward compatibility:** frontend cũ gửi `{ "coachId": "..." }` vẫn chạy. Nhưng phòng mới tạo có
thể ở trạng thái `pending` (xem 18.5) — đây là **thay đổi hành vi** so với trước.

### 18.2 `POST /api/chat/rooms` — tạo hoặc lấy phòng

**Request — cách dùng mới (khuyến nghị):**

```json
{
  "targetUserId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
  "coachId": null,
  "sourceType": "community_post",
  "sourceId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b"
}
```

**Request — tối giản:**

```json
{ "targetUserId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9" }
```

**Request — legacy (vẫn hoạt động):**

```json
{ "coachId": "8e0c2a4b-6d8f-401a-b3c5-d7e9f1a3b5c7" }
```

**Quy tắc ưu tiên field:**

```csharp
var targetUserId = request.TargetUserId ?? request.CoachId!.Value;
```

→ Nếu gửi **cả hai**, `targetUserId` **thắng**, `coachId` bị bỏ qua hoàn toàn.

**Validation (`CreateChatRoomRequestValidator`):**

| Rule | Chi tiết |
|---|---|
| Phải có ít nhất một trong `targetUserId` / `coachId` | Thiếu cả hai → `400`, detail `"targetUserId is required"` |
| `sourceType` | `MaximumLength(30)` khi `!= null` |
| `sourceId` | **Không validate** |

> `sourceType` là **string tự do ≤ 30 ký tự**. `ChatSourceTypes` gợi ý 2 giá trị: `"booking"`,
> `"community_post"` — nhưng backend **không ép**. Chỉ dùng để hiển thị ngữ cảnh, **không bao giờ**
> dùng cho authorization.

**Response (phòng mới, chưa có quan hệ booking → `pending`):**

```json
{
  "isSuccess": true,
  "data": {
    "id": "a9b0c1d2-e3f4-4657-c8d9-e0f1a2b3c4d5",
    "user1Id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
    "user2Id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "otherUserId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "status": "pending",
    "requestedByUserId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
    "requestedAt": "2026-08-04T16:00:00Z",
    "acceptedAt": null,
    "rejectedAt": null,
    "lastMessageAt": null,
    "sourceType": "community_post",
    "sourceId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
    "createdAt": "2026-08-04T16:00:00Z"
  },
  "error": null
}
```

**Response (đã có booking chung → `active` ngay):**

```json
{
  "isSuccess": true,
  "data": {
    "id": "b0c1d2e3-f4a5-4768-d9e0-f1a2b3c4d5e6",
    "user1Id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
    "user2Id": "8e0c2a4b-6d8f-401a-b3c5-d7e9f1a3b5c7",
    "otherUserId": "8e0c2a4b-6d8f-401a-b3c5-d7e9f1a3b5c7",
    "status": "active",
    "requestedByUserId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
    "requestedAt": "2026-08-04T16:05:00Z",
    "acceptedAt": "2026-08-04T16:05:00Z",
    "rejectedAt": null,
    "lastMessageAt": null,
    "sourceType": null,
    "sourceId": null,
    "createdAt": "2026-08-04T16:05:00Z"
  },
  "error": null
}
```

### 18.3 ⚠️ `ChatRoomResponse` KHÔNG có những field bạn có thể mong đợi

**Backend contract — đã đối chiếu Swagger:**

| Field bạn có thể mong đợi | Có tồn tại? |
|---|:---:|
| `otherUser` (object có `fullName`, `avatarUrl`) | ❌ **KHÔNG** |
| `unreadCount` | ❌ **KHÔNG** |
| `lastMessage` (object) | ❌ **KHÔNG** |

Response **chỉ** có `otherUserId` (Guid) và `lastMessageAt` (DateTime?).

> **Frontend BẮT BUỘC:**
> - Lấy tên/avatar của đối phương bằng cách gọi API user riêng theo `otherUserId`, hoặc cache
>   thông tin user ở store.
> - **Không thể** hiển thị preview tin nhắn cuối trong danh sách phòng — chỉ có `lastMessageAt`.
>   Muốn có preview phải gọi `GET /api/chat/rooms/{roomId}/messages?pageSize=1` cho từng phòng
>   (tốn kém — cân nhắc chỉ làm cho vài phòng đầu).
> - **Không có badge số tin chưa đọc.** Xem 18.7.

`otherUserId` = `null` khi caller không xác định — thực tế **luôn có giá trị** vì mọi endpoint chat
đều yêu cầu đăng nhập.

### 18.4 Quy tắc tạo phòng

| # | Kiểm tra | Error code | HTTP |
|---|---|---|---|
| 1 | `targetUserId == currentUserId` | `CHAT_CANNOT_MESSAGE_SELF` | **403** |
| 2 | Target user không tồn tại | `CHAT_TARGET_USER_NOT_FOUND` | 404 |
| 3 | `target.status != "active"` | `CHAT_TARGET_USER_INACTIVE` | **409** |
| 4 | Bị block (**một trong hai chiều**) | `CHAT_USER_BLOCKED` | **403** |
| 5 | Đã có phòng giữa 2 người | ✅ Trả phòng cũ (`200`), **không tạo mới** |

**Chuẩn hóa cặp user:** `user1Id` = Guid nhỏ hơn, `user2Id` = Guid lớn hơn
(`userId.CompareTo(targetUserId) <= 0`). DB có unique constraint trên cặp này → **không bao giờ có
2 phòng cho cùng 2 người**.

> ⚠️ Khi trả phòng cũ, backend **không cập nhật** `sourceType`/`sourceId` mới. Ngữ cảnh chỉ được lưu
> ở lần tạo phòng đầu tiên. Mở chat từ community post lần thứ hai vẫn giữ `sourceType` cũ.
>
> ⚠️ Khi trả phòng cũ đang ở trạng thái `rejected`, bạn **vẫn nhận `200`** với phòng `rejected` đó —
> không có cách nào tạo lại phòng mới. Xem 18.5.

**Không yêu cầu target có coach profile.** Learner ↔ learner hoàn toàn hợp lệ.

### 18.5 Trạng thái phòng — `pending` vs `active`

**Backend contract:**

```csharp
var hasBookingRelationship =
    await _bookingRepository.GetActiveOrCompletedBetweenUsersAsync(userId, targetUserId) != null;

status      = hasBookingRelationship ? "active" : "pending";
acceptedAt  = hasBookingRelationship ? now      : null;
```

| Tình huống | Trạng thái phòng mới |
|---|---|
| Hai user **đã có booking** `active` hoặc `completed` với nhau (learner ↔ coach của gói đó) | **`active` ngay** |
| Mọi trường hợp khác (learner ↔ learner, mở từ community post, coach ↔ người lạ) | **`pending`** |

> ⚠️ **Known limitation:** application community đã `accepted` **KHÔNG** làm phòng active ngay —
> chỉ quan hệ **booking** mới có tác dụng. Người xin tham gia đã được duyệt vẫn phải qua bước chat
> request.

**Chat coach ↔ learner hiện có không bị phá vỡ:** nếu đã từng mua gói của coach đó, phòng vẫn
`active` ngay như trước. Phòng cũ (tạo trước migration) được backfill `status = "active"`.

### 18.6 `GET /api/chat/rooms` — danh sách phòng

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Request body | `No request body` |
| Query params | **Không có** — không phân trang, không filter |
| Response | `Result<ChatRoomResponse[]>` — **mảng phẳng, KHÔNG phải `PagedResult`** |

**Sắp xếp:** `lastMessageAt ?? createdAt` **giảm dần** (phòng có hoạt động gần nhất lên đầu).

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": "a9b0c1d2-e3f4-4657-c8d9-e0f1a2b3c4d5",
      "user1Id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
      "user2Id": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
      "otherUserId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
      "status": "active",
      "requestedByUserId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
      "requestedAt": "2026-08-04T16:00:00Z",
      "acceptedAt": "2026-08-04T16:20:00Z",
      "rejectedAt": null,
      "lastMessageAt": "2026-08-04T16:45:00Z",
      "sourceType": "community_post",
      "sourceId": "b7d2f1a0-3c4e-4d5f-8a6b-7c8d9e0f1a2b",
      "createdAt": "2026-08-04T16:00:00Z"
    },
    {
      "id": "c1d2e3f4-a5b6-4879-e0f1-a2b3c4d5e6f7",
      "user1Id": "2e4f6a8b-0c1d-4e2f-8a3b-4c5d6e7f8a9b",
      "user2Id": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
      "otherUserId": "2e4f6a8b-0c1d-4e2f-8a3b-4c5d6e7f8a9b",
      "status": "pending",
      "requestedByUserId": "2e4f6a8b-0c1d-4e2f-8a3b-4c5d6e7f8a9b",
      "requestedAt": "2026-08-04T15:00:00Z",
      "acceptedAt": null,
      "rejectedAt": null,
      "lastMessageAt": null,
      "sourceType": null,
      "sourceId": null,
      "createdAt": "2026-08-04T15:00:00Z"
    }
  ],
  "error": null
}
```

**Trả về TẤT CẢ phòng** ở mọi trạng thái (`pending`, `active`, `rejected`) mà user là thành viên.

> **Frontend recommendation:** chia UI thành 2 tab:
> - **Tin nhắn**: `status === 'active'`
> - **Lời mời** (badge số lượng): `status === 'pending' && requestedByUserId !== currentUserId`
>
> Phòng `pending` do **chính mình** gửi → hiện trong tab Tin nhắn với nhãn "Đang chờ phản hồi".
> Phòng `rejected` → nên ẩn hoặc gom vào mục lưu trữ.

### 18.7 ⚠️ Không có mark-as-read

**Backend contract — đã kiểm tra toàn bộ codebase:**

- **Không có endpoint** `PUT /api/chat/rooms/{roomId}/read`.
- `Message.IsRead` **chỉ được gán `false`** khi tạo tin nhắn. **Không có dòng code nào set nó thành
  `true`.**
- **Không có** field `unreadCount` ở bất kỳ DTO chat nào.

→ **`isRead` trong `ChatMessageResponse` LUÔN LUÔN là `false`.**

> **Known limitation nghiêm trọng.** Frontend **không thể** triển khai:
> - Badge số tin chưa đọc chính xác từ server
> - Read receipt / "đã xem"
> - Đánh dấu đã đọc
>
> **Frontend recommendation — workaround client-side:**
> ```ts
> // Lưu localStorage: { [roomId]: lastSeenIso }
> const unread = (room: ChatRoomResponse) => {
>   const seen = localStorage.getItem(`chat:lastSeen:${room.id}`);
>   return !!room.lastMessageAt && (!seen || room.lastMessageAt > seen);
> };
> // → chỉ ra được "có tin mới hay không" (boolean), KHÔNG ra được số lượng.
> ```
> Không đồng bộ giữa các thiết bị. Nếu cần chính xác, phải yêu cầu backend bổ sung.

### 18.8 `GET /api/chat/rooms/{roomId}/messages` — lịch sử tin nhắn

**Query parameters:**

| Param | Kiểu | Mặc định | Ràng buộc |
|---|---|---|---|
| `pageNumber` | `int` | `1` | `> 0` |
| `pageSize` | `int` | `20` | `1..100` |

**Sắp xếp: `sentAt` GIẢM DẦN** (tin mới nhất ở `items[0]`).

> **Frontend recommendation:** đảo mảng khi render (`[...items].reverse()`) để hiển thị theo thứ tự
> hội thoại từ cũ → mới, và load thêm trang khi scroll lên trên (infinite scroll ngược).

**Response:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "e7f8a9b0-c1d2-4435-a6b7-c8d9e0f1a2b3",
        "roomId": "a9b0c1d2-e3f4-4657-c8d9-e0f1a2b3c4d5",
        "senderId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
        "content": "Đây là hình sân nhé bạn.",
        "isRead": false,
        "sentAt": "2026-08-04T16:45:00Z",
        "attachments": [
          {
            "id": "f0a1b2c3-d4e5-4f68-9a0b-1c2d3e4f5a6b",
            "fileUrl": "https://cdn.sportico.example/chat/court.webp",
            "fileType": "image"
          }
        ]
      },
      {
        "id": "d6e7f8a9-b0c1-4324-95a6-b7c8d9e0f1a2",
        "roomId": "a9b0c1d2-e3f4-4657-c8d9-e0f1a2b3c4d5",
        "senderId": "3f5a7c9e-1b2d-4e6f-8a0c-2d4e6f8a0b1c",
        "content": "Chào bạn, sân ở đâu vậy?",
        "isRead": false,
        "sentAt": "2026-08-04T16:30:00Z",
        "attachments": []
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 2,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  },
  "error": null
}
```

**Điều kiện lỗi:**

| Điều kiện | Error code | HTTP |
|---|---|---|
| Phòng không tồn tại | `CHAT_NOT_ALLOWED` | **404** |
| Không phải thành viên phòng | `CHAT_NOT_ALLOWED` | **403** |

> ⚠️ Cùng error code `CHAT_NOT_ALLOWED` cho cả 404 lẫn 403. Phân biệt bằng HTTP status / `error.type`.

**Đọc lịch sử được ở MỌI trạng thái phòng** — kể cả `pending` và `rejected`. Backend **không** chặn
GET theo status. Lịch sử chat **không bao giờ bị xóa** khi block hay reject.

### 18.9 `POST /api/chat/rooms/{roomId}/messages` — gửi tin nhắn

Xem [mục 19](#19-message-requests) cho quy tắc theo trạng thái phòng và
[mục 20](#20-message-attachments) cho attachment.

---

## 19. Message requests

**Backend contract.**

### 19.1 State machine phòng chat

```mermaid
stateDiagram-v2
    [*] --> pending: POST /api/chat/rooms (không có booking chung)
    [*] --> active: POST /api/chat/rooms (đã có booking active/completed)
    pending --> active: receiver PUT .../accept
    pending --> rejected: receiver PUT .../reject
    rejected --> [*]: TERMINAL — không thể tạo lại
    active --> [*]
```

| Transition | Ai thực hiện |
|---|---|
| `→ pending` / `→ active` | Người gửi (requester) |
| `pending → active` | **Chỉ receiver** (người KHÔNG phải `requestedByUserId`) |
| `pending → rejected` | **Chỉ receiver** |
| `rejected → *` | ❌ Không có transition nào |

### 19.2 `PUT /api/chat/rooms/{roomId}/accept`

| Mục | Giá trị |
|---|---|
| Auth | Bắt buộc |
| Request body | `No request body` |
| Response | `Result<ChatRoomResponse>` |

**Kết quả:** `status → "active"`, `acceptedAt = now`. Gửi notification cho `requestedByUserId`
(`type: "message"`, title `"Chat request accepted"`).

### 19.3 `PUT /api/chat/rooms/{roomId}/reject`

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `Result<ChatRoomResponse>` |

**Kết quả:** `status → "rejected"`, `rejectedAt = now`. **Không** gửi notification.

### 19.4 Điều kiện lỗi accept/reject

| # | Điều kiện | Error code | HTTP |
|---|---|---|---|
| 1 | Phòng không tồn tại | `CHAT_NOT_ALLOWED` | 404 |
| 2 | Không phải thành viên phòng | `CHAT_NOT_ALLOWED` | 403 |
| 3 | **Chính bạn là người gửi request** | `CHAT_NOT_ALLOWED` | **403** |
| 4 | `status != "pending"` | `CHAT_ROOM_NOT_PENDING` | **409** |

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "CHAT_NOT_ALLOWED",
    "message": "You cannot respond to your own chat request",
    "type": "Forbidden",
    "details": null
  }
}
```

→ **Người gửi KHÔNG thể tự accept.** Frontend chỉ hiện nút Chấp nhận/Từ chối khi
`room.status === 'pending' && room.requestedByUserId !== currentUserId`.

### 19.5 Ai gửi được tin nhắn khi nào

**Backend contract** — kiểm tra trong `SendMessageAsync`, theo đúng thứ tự:

| # | Điều kiện | Error code | HTTP |
|---|---|---|---|
| 1 | Phòng không tồn tại | `CHAT_NOT_ALLOWED` | 404 |
| 2 | Không phải thành viên | `CHAT_NOT_ALLOWED` | 403 |
| 3 | `status == "rejected"` | `CHAT_ROOM_REJECTED` | **409** |
| 4 | `status == "pending"` **và** bạn **không phải** người gửi request | `CHAT_ROOM_NOT_PENDING` | **409** |
| 5 | Bị block (một trong hai chiều) | `CHAT_USER_BLOCKED` | **403** |

**Bảng tổng hợp:**

| Trạng thái phòng | Requester gửi được? | Receiver gửi được? |
|---|:---:|:---:|
| `pending` | ✅ **Có** | ❌ `409 CHAT_ROOM_NOT_PENDING` |
| `active` | ✅ | ✅ |
| `rejected` | ❌ `409 CHAT_ROOM_REJECTED` | ❌ `409 CHAT_ROOM_REJECTED` |

→ **Tin nhắn đầu tiên ĐƯỢC lưu ngay cả khi phòng đang `pending`.** Requester có thể gửi nhiều tin
liên tiếp trong lúc chờ — backend **không giới hạn số lượng**. Receiver đọc được toàn bộ nhưng
không trả lời được cho tới khi accept.

> **Frontend recommendation** cho phòng `pending`:
> - Nếu bạn là requester: cho gõ + gửi bình thường, hiện banner "Đang chờ đối phương chấp nhận".
> - Nếu bạn là receiver: **disable ô nhập**, hiện 2 nút lớn "Chấp nhận" / "Từ chối" trên đầu, và
>   hiển thị các tin đã nhận (chúng đã có sẵn qua `GET messages`).
> - Phòng `rejected`: **read-only** cho cả hai bên. Hiện banner "Cuộc trò chuyện đã bị từ chối."

**Rejected là vĩnh viễn:** không có endpoint nào đưa `rejected → pending/active`, và
`POST /api/chat/rooms` sẽ trả lại đúng phòng `rejected` cũ (vì unique pair). → **Known limitation.**

---

## 20. Message attachments

**Backend contract.**

### 20.1 `POST /api/chat/rooms/{roomId}/messages`

**Request — chỉ text:**

```json
{
  "content": "Chào bạn, sân ở đâu vậy?",
  "attachments": null
}
```

**Request — text + attachment:**

```json
{
  "content": "Đây là hình sân nhé bạn.",
  "attachments": [
    {
      "fileUrl": "https://cdn.sportico.example/chat/court.webp",
      "fileType": "image"
    }
  ]
}
```

**Request — chỉ attachment (hợp lệ):**

```json
{
  "content": null,
  "attachments": [
    { "fileUrl": "https://cdn.sportico.example/chat/court.webp", "fileType": "image" },
    { "fileUrl": "https://cdn.sportico.example/chat/court2.webp", "fileType": null }
  ]
}
```

### 20.2 Validation (`SendMessageRequestValidator`)

| Rule | Chi tiết |
|---|---|
| `content` | Optional. `MaximumLength(2000)` → `"Content is too long"` |
| **Bắt buộc có ít nhất một** | `content` không rỗng/whitespace **HOẶC** `attachments.length > 0`. Vi phạm → `"Either content or at least one attachment is required"` |
| Số attachment | **≤ 5** → `"A message may have at most 5 attachments"` |
| `attachments[].fileUrl` | `NotEmpty` + phải là absolute URL, scheme `http` **hoặc** `https` → `"Attachment fileUrl must be a valid http(s) URL"` |
| `attachments[].fileType` | Optional, **không validate gì cả** |

> ⚠️ **Khác community media:** chat attachment chấp nhận **cả `http`**, community media **chỉ `https`**.
> **Frontend recommendation: luôn chỉ gửi `https`.**
>
> `fileType` là string tự do, backend không ép enum. **Frontend recommendation:** dùng tập cố định
> `image` · `video` · `file` · `audio` để render icon đúng.

### 20.3 Response

```json
{
  "isSuccess": true,
  "data": {
    "id": "e7f8a9b0-c1d2-4435-a6b7-c8d9e0f1a2b3",
    "roomId": "a9b0c1d2-e3f4-4657-c8d9-e0f1a2b3c4d5",
    "senderId": "4a6b8c0d-2e4f-4061-8273-8495a6b7c8d9",
    "content": "Đây là hình sân nhé bạn.",
    "isRead": false,
    "sentAt": "2026-08-04T16:45:00Z",
    "attachments": [
      {
        "id": "f0a1b2c3-d4e5-4f68-9a0b-1c2d3e4f5a6b",
        "fileUrl": "https://cdn.sportico.example/chat/court.webp",
        "fileType": "image"
      }
    ]
  },
  "error": null
}
```

Khi gửi chỉ attachment (không content), `content` trả về là **chuỗi rỗng `""`**, không phải `null`
(backend làm `request.Content?.Trim() ?? string.Empty`).

### 20.4 Transaction và notification

**Backend contract:**
1. Tạo `Message` + tất cả `MessageAttachment` trong **cùng một `SaveChanges`** → atomic. Không bao
   giờ có message mồ côi hoặc attachment mồ côi.
2. Cập nhật `room.lastMessageAt = now` trong cùng transaction đó.
3. **Chỉ sau khi commit thành công**, mới tạo notification cho **người nhận** (`receiverId`).
4. **Không bao giờ** gửi notification cho chính người gửi.

**Nội dung notification:**

| Trường hợp | `content` của notification |
|---|---|
| Có text | 80 ký tự đầu của message, thêm `…` nếu bị cắt |
| Chỉ attachment | `"Sent an attachment"` (tiếng Anh) |

`title` = `"New message"`, `type` = `"message"`.

> ⚠️ Nội dung notification là **tiếng Anh**. Frontend nên render notification chat dựa vào `type`
> và tự sinh chuỗi tiếng Việt, thay vì hiển thị `title`/`content` thô.

### 20.5 Bảo mật attachment — cùng cảnh báo như community media

> **Backend KHÔNG xác minh ownership file, KHÔNG whitelist host, KHÔNG kiểm MIME/kích thước.**
> Chấp nhận cả `http` (không mã hóa).
>
> **Frontend BẮT BUỘC:** chỉ upload qua storage chính thức và chỉ gửi URL `https` nhận được từ đó.
> Không cho user dán URL tùy ý. Khi render, dùng `referrerPolicy="no-referrer"`.

---

## 21. User block

**Backend contract.** Base route `/api/users`. Yêu cầu đăng nhập, không giới hạn role.

### 21.1 `PUT /api/users/{userId}/block` — chặn

**Request (body optional — có thể gửi rỗng hoặc bỏ hẳn body):**

```json
{ "reason": "Gửi tin nhắn quấy rối" }
```

```json
{ "reason": null }
```

Controller khai `[FromBody] BlockUserRequest? request` và fallback `?? new BlockUserRequest()`
→ **body hoàn toàn optional**.

**Validation:** ⚠️ **Không có validator nào cho `BlockUserRequest`.** `reason` không bị kiểm độ dài
ở tầng ứng dụng. Cột DB giới hạn **500 ký tự** → gửi dài hơn sẽ gây lỗi `500` từ Postgres.

> **Frontend recommendation: tự giới hạn `reason` ≤ 500 ký tự.**

**Response:**

```json
{ "isSuccess": true, "data": { "blocked": true }, "error": null }
```

| Điều kiện | Kết quả |
|---|---|
| `userId == currentUserId` | `403 USER_BLOCK_CANNOT_BLOCK_SELF`, message `"You cannot block yourself"` |
| User không tồn tại | `404 USER_NOT_FOUND` |
| Đã chặn rồi | `200 { blocked: true }` (**idempotent**, không tạo bản ghi mới, `reason` cũ **không** được cập nhật) |
| OK | Tạo `UserBlock`, trả `200` |

> Backend **không** kiểm tra `target.status` — chặn được cả user `inactive`/`banned`.

### 21.2 `DELETE /api/users/{userId}/block` — bỏ chặn

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Response | `{ "isSuccess": true, "data": { "blocked": false }, "error": null }` |

| Điều kiện | Kết quả |
|---|---|
| Chưa từng chặn | `200 { blocked: false }` (**idempotent**, không lỗi) |
| Đang chặn | Xóa bản ghi, `200 { blocked: false }` |

> Endpoint này **không** kiểm tra user có tồn tại hay không → luôn `200`.

### 21.3 `GET /api/users/me/blocked` — danh sách đã chặn

| Mục | Giá trị |
|---|---|
| Request body | `No request body` |
| Query params | **Không có** — không phân trang |
| Response | `Result<BlockedUserResponse[]>` — **mảng phẳng** |

```json
{
  "isSuccess": true,
  "data": [
    {
      "userId": "5b7c9d0e-1f2a-4304-9506-b7c8d9e0f1a2",
      "fullName": "Lê Văn C",
      "avatarUrl": "https://cdn.sportico.example/avatars/c.webp",
      "createdAt": "2026-08-04T12:00:00Z",
      "reason": "Gửi tin nhắn quấy rối"
    },
    {
      "userId": "6c8d0e1f-2a3b-4415-a607-c8d9e0f1a2b3",
      "fullName": "Phạm Thị D",
      "avatarUrl": null,
      "createdAt": "2026-07-20T09:00:00Z",
      "reason": null
    }
  ],
  "error": null
}
```

- Chỉ liệt kê những người **bạn đã chặn**. **Không có** endpoint xem ai đã chặn bạn (đúng về mặt
  quyền riêng tư).
- `fullName` fallback `""` nếu navigation không load được.
- `createdAt` = thời điểm chặn.

### 21.4 Block ảnh hưởng đến những gì — bảng chính xác

**Backend contract.** `IUserBlockRepository.IsBlockedEitherDirectionAsync` chỉ được gọi ở **2 chỗ**
trong toàn bộ codebase: `ChatService.CreateOrGetRoomAsync` và `ChatService.SendMessageAsync`.

| Hành vi | Bị chặn? | Chi tiết |
|---|:---:|---|
| Tạo phòng chat mới | ✅ **CÓ** | `403 CHAT_USER_BLOCKED`, kiểm **cả hai chiều** |
| Gửi tin nhắn | ✅ **CÓ** | `403 CHAT_USER_BLOCKED`, kiểm **cả hai chiều** |
| Đọc lịch sử chat | ❌ Không | Vẫn xem được toàn bộ tin cũ |
| Accept/reject chat request | ❌ Không | Không kiểm block |
| **Xin tham gia community post** | ❌ **KHÔNG** | ⚠️ Không kiểm block — xem limitation |
| Accept/reject application | ❌ Không | |
| Xem bài của người đã chặn trong feed | ❌ Không | Bài **vẫn hiện** bình thường |
| Xem comment của người đã chặn | ❌ Không | Comment **vẫn hiện** bình thường |
| Like bài của người đã chặn | ❌ Không | |
| Report | ❌ Không | |

> ⚠️ **Known limitation quan trọng:** block **chỉ có tác dụng với chat**. Spec ban đầu yêu cầu
> "không xin tham gia bài của người chặn" nhưng `CommunityPostService.ApplyAsync` **không** gọi
> `IUserBlockRepository`. Feed và comment cũng **không** lọc theo block.
>
> **Frontend recommendation:** nếu cần trải nghiệm "ẩn nội dung của người đã chặn", phải tự lọc
> client-side dựa trên `GET /api/users/me/blocked`:
> ```ts
> const blockedIds = new Set(blocked.map(b => b.userId));
> const visiblePosts = posts.filter(p => !blockedIds.has(p.author.id));
> ```
> Lưu ý đây chỉ che ở client, **không** phải bảo mật, và không xử lý được chiều "người khác chặn bạn".

### 21.5 Block/unblock và phòng chat

| Câu hỏi | Trả lời |
|---|---|
| Chặn có xóa lịch sử chat? | ❌ **Không.** Toàn bộ tin nhắn được giữ nguyên. |
| Phòng cũ có thành read-only? | ✅ **Có, trên thực tế** — `status` **không đổi** (vẫn `active`), nhưng mọi lần `POST messages` đều `403 CHAT_USER_BLOCKED`. Đọc vẫn bình thường. |
| Chặn có đổi `room.status`? | ❌ **Không.** `status` vẫn là giá trị cũ. Không có cách nào biết phòng bị chặn từ `ChatRoomResponse`. |
| Bỏ chặn có tự kích hoạt lại phòng? | Không cần — phòng chưa bao giờ bị đổi trạng thái. Sau unblock, gửi tin lại được ngay. |
| Bỏ chặn có đổi `rejected` → `active`? | ❌ **Không.** Đó là hai cơ chế độc lập. |

> **Frontend recommendation:** vì `ChatRoomResponse` **không** cho biết đã bị chặn, hãy cross-check
> `otherUserId` với danh sách `GET /api/users/me/blocked` để hiển thị banner "Bạn đã chặn người này"
> + disable ô nhập. Với chiều ngược lại (người ta chặn bạn), **không có cách nào biết trước** — chỉ
> phát hiện khi gửi tin và nhận `403 CHAT_USER_BLOCKED`.

---

## 22. Notifications

**Backend contract.** Module notification **có sẵn từ trước**, không thay đổi contract.

### 22.1 Endpoints hiện có

| Endpoint | Auth | Mô tả |
|---|---|---|
| `GET /api/notifications/me` | Bắt buộc | Danh sách phân trang |
| `GET /api/notifications/me/unread-count` | Bắt buộc | Số chưa đọc |
| `PUT /api/notifications/{id}/read` | Bắt buộc | Đánh dấu đã đọc |
| `PUT /api/notifications/me/read-all` | Bắt buộc | Đánh dấu tất cả đã đọc |

**Query parameters cho `GET /api/notifications/me`:**

| Param | Kiểu | Mặc định |
|---|---|---|
| `isRead` | `bool \| null` | — |
| `type` | `string \| null` | — |
| `pageNumber` | `int` | `1` |
| `pageSize` | `int` | **`10`** (khác các endpoint khác) |

### 22.2 `NotificationResponse` — chỉ 6 field

```json
{
  "id": "a1b2c3d4-e5f6-4708-9192-a3b4c5d6e7f8",
  "title": "New comment",
  "content": "Someone commented on \"Tìm 2 người đánh cầu lông tối thứ Sáu\"",
  "type": "post",
  "isRead": false,
  "createdAt": "2026-08-04T15:00:00Z"
}
```

> ⚠️ **KHÔNG có `referenceId` và `referenceType`.** Entity `Notification` không có các cột này.
>
> **Hệ quả nghiêm trọng cho frontend:** **không thể** điều hướng từ notification đến đúng
> bài/phòng chat/đơn xin tham gia. Chỉ deep-link được tới trang danh sách theo `type`.
>
> **Frontend recommendation:**
> ```
> type "post"    → /community            (danh sách, không tới được bài cụ thể)
> type "message" → /messages             (danh sách phòng)
> type "report"  → /community/my-posts
> type "system"  → /community/my-posts
> ```

### 22.3 Notification type constants — KHÔNG có type mới

**Backend contract quan trọng:** module Voucher/Community/Chat **không thêm** bất kỳ
`NotificationTypeConstants` mới nào. Toàn bộ dùng lại 4 giá trị có sẵn:

| `type` | Sự kiện tạo ra nó |
|---|---|
| `"system"` | Có người xin tham gia bài · Đơn được chấp nhận · Đơn bị từ chối · Có người rời hoạt động |
| `"post"` | Có bình luận mới trên bài · Có người trả lời bình luận của bạn |
| `"message"` | Có chat request mới · Chat request được chấp nhận · Có tin nhắn mới |
| `"report"` | Admin ẩn bài của bạn · Admin xóa bài của bạn · Admin ẩn bình luận của bạn |

> ❌ **KHÔNG tồn tại** các type như `community_comment`, `community_reply`, `community_application`,
> `chat_request`, `community_moderated`… Đừng viết code dựa trên chúng.

**Danh sách đầy đủ `NotificationTypeConstants` trong hệ thống:**

```
message | review | follow | payment | package | post | system | report
booking | training_package | training_session | training_plan | wallet
```

### 22.4 Bảng notification chính xác — ai nhận, title/content gì

| Sự kiện | Người nhận | `type` | `title` (tiếng Anh) | `content` |
|---|---|---|---|---|
| Có người xin tham gia | Chủ bài | `system` | `New join request` | `Someone requested to join "<title bài>"` |
| Đơn được chấp nhận | Applicant | `system` | `Your join request was accepted` | `<title bài>` |
| Đơn bị từ chối | Applicant | `system` | `Your join request was declined` | `<title bài>` |
| Người đã accepted rời đi | Chủ bài | `system` | `A participant left` | `A participant left your activity` |
| Bình luận mới | Chủ bài | `post` | `New comment` | `Someone commented on "<title bài>"` |
| Trả lời bình luận | Tác giả comment cha | `post` | `New reply` | `Someone replied to your comment` |
| Chat request mới | Target user | `message` | `New chat request` | `Someone wants to start a conversation with you` |
| Phòng chat active ngay (có booking) | Target user | `message` | `New chat` | `You have a new conversation` |
| Chat request được chấp nhận | Requester | `message` | `Chat request accepted` | `Your chat request was accepted` |
| Tin nhắn mới | Người nhận | `message` | `New message` | 80 ký tự đầu, hoặc `Sent an attachment` |
| Admin ẩn bài | Tác giả bài | `report` | `Your post was hidden` | = `reason` admin nhập |
| Admin xóa bài | Tác giả bài | `report` | `Your post was removed` | `Your post violated community guidelines and was removed by an admin.` |
| Admin ẩn comment | Tác giả comment | `report` | `Your comment was hidden` | = `reason` admin nhập |

**Không gửi notification** trong các trường hợp:
- Người thực hiện = người nhận (backend kiểm `post.AuthorId != userId`, `parent.AuthorId != userId`,
  `receiverId != senderId`).
- Admin **xóa** comment (chỉ ẩn mới có notification).
- Reject chat request.
- Like/unlike bài.
- Voucher (không có notification nào cho voucher).

> ⚠️ **Toàn bộ title/content là tiếng Anh.** Frontend nên map sang tiếng Việt dựa vào `title` string
> hoặc `type`, chứ đừng hiển thị thô.

### 22.5 Notification là best-effort

**Backend contract:** mọi notification community đều gọi
`_notificationRepository.TryAddAndSaveAsync(...)` và **bỏ qua lỗi** (`_ = error;`). Nghiệp vụ chính
**không bao giờ** bị rollback vì notification fail.

→ **Frontend không được coi notification là bằng chứng thao tác thành công.** Luôn dựa vào response
HTTP của chính request đó.

---

## 23. Realtime/SignalR

**Backend contract — kết luận rõ ràng:**

> ## ❌ SignalR / WebSocket / realtime **CHƯA ĐƯỢC TRIỂN KHAI**
>
> Đã kiểm tra toàn bộ `src/`: **không có** `Microsoft.AspNetCore.SignalR` package reference,
> **không có** class kế thừa `Hub`, **không có** `AddSignalR()`, **không có** `MapHub()` trong
> `Program.cs`, **không có** endpoint WebSocket hay SSE nào.
>
> **Frontend PHẢI dùng polling / refetch. KHÔNG được giả định tồn tại bất kỳ event realtime nào.**
> Đừng viết code cho `message:new`, `room:updated`, `chat_request:new`… — chúng **không tồn tại**.

### 23.1 Chiến lược polling khuyến nghị

**Frontend recommendation:**

| Dữ liệu | Cách làm | Tần suất gợi ý |
|---|---|---|
| Danh sách phòng chat | `GET /api/chat/rooms` | 15–30 s khi tab đang mở |
| Tin nhắn trong phòng đang mở | `GET /api/chat/rooms/{id}/messages?pageNumber=1&pageSize=20` | 5–10 s |
| Số notification chưa đọc | `GET /api/notifications/me/unread-count` | 30–60 s |
| Feed community | Refetch khi focus / pull-to-refresh | Không polling nền |
| Trạng thái booking sau PayOS | `POST /api/payments/payos/reconcile` | Xem mục 33 Flow A |

```ts
// Frontend recommendation — TanStack Query
useQuery({
  queryKey: ['chat-messages', roomId, 1],
  queryFn: () => api.getMessages(roomId, { pageNumber: 1, pageSize: 20 }),
  refetchInterval: (q) => (document.visibilityState === 'visible' ? 7000 : false),
  refetchIntervalInBackground: false,
});
```

**Nguyên tắc:**
- **Dừng polling khi tab không hiển thị** (`document.visibilityState`) để tiết kiệm pin/quota.
- **Chỉ poll trang đầu** của tin nhắn (`pageNumber: 1`) — vì sort là `sentAt DESC` nên tin mới luôn
  ở trang 1.
- **Dedupe bằng `message.id`** khi merge kết quả poll với tin vừa gửi optimistic.
- Sau khi gửi tin thành công, invalidate ngay `['chat-messages', roomId]` và `['chat-rooms']`.

### 23.2 Những gì KHÔNG làm được vì thiếu realtime

| Tính năng | Trạng thái |
|---|---|
| Tin nhắn hiện tức thì | ❌ Trễ theo chu kỳ polling |
| Typing indicator ("đang nhập…") | ❌ **Không có API** |
| Read receipt / "đã xem" | ❌ **Không có API** (xem 18.7) |
| Presence (online/offline) | ❌ **Không có API** |
| Badge số tin chưa đọc chính xác | ❌ Xem 18.7 |
| Thông báo đẩy realtime | ❌ Chỉ polling `unread-count` |

---

## 24. State machines

**Backend contract.** Các sơ đồ dưới đây phản ánh chính xác code hiện tại.

### 24.1 Voucher campaign

```mermaid
stateDiagram-v2
    [*] --> draft: POST /api/admin/voucher-campaigns (admin)
    draft --> active: PUT .../activate (admin)
    draft --> paused: PUT .../pause (admin)
    draft --> ended: PUT .../end (admin)
    active --> paused: PUT .../pause (admin)
    active --> ended: PUT .../end (admin)
    paused --> active: PUT .../activate (admin)
    paused --> ended: PUT .../end (admin)
    ended --> [*]: TERMINAL — mọi transition trả 409
```

Mọi transition do **admin** thực hiện. Không có worker nào tự đổi `status` của campaign — campaign
quá `endAt` vẫn giữ `status = "active"`, chỉ bị từ chối lúc validate voucher.

### 24.2 Voucher redemption

```mermaid
stateDiagram-v2
    [*] --> reserved: purchase (learner) — VoucherService.ReserveForBookingAsync
    reserved --> applied: payment paid (webhook / reconcile / voucher 100% / manual)
    reserved --> released: payment cancelled/failed/expired (webhook / reconcile / worker)
    applied --> [*]: TERMINAL — không bao giờ release
    released --> [*]: TERMINAL
```

| Transition | Ai kích hoạt |
|---|---|
| `→ reserved` | Learner (purchase) |
| `reserved → applied` | Webhook PayOS · Reconcile (learner) · Ngay lập tức nếu voucher 100% hoặc manual purchase |
| `reserved → released` | Webhook PayOS · Reconcile · **Worker** `PaymentAndVoucherExpirySweepBackgroundService` (10 phút/lần) |

**Counter tương ứng:**

| Transition | `reservedCount` | `usedCount` | `reservedDiscountAmount` | `usedDiscountAmount` |
|---|---|---|---|---|
| `→ reserved` | `+1` | — | `+discount` | — |
| `reserved → applied` | `−1` (clamp ≥ 0) | `+1` | `−discount` (clamp ≥ 0) | `+discount` |
| `reserved → released` | `−1` (clamp ≥ 0) | — | `−discount` (clamp ≥ 0) | — |

### 24.3 Booking + payment + voucher (luồng PayOS)

```mermaid
flowchart TD
    A[Learner: POST /api/bookings/purchase/payos] --> B{Validate package,<br/>capacity, voucher}
    B -->|fail| Z[4xx error]
    B -->|ok| C[Tạo booking snapshot<br/>status = pending_payment]
    C --> D[Reserve session slots]
    D --> E[Tạo VoucherRedemption<br/>status = reserved]
    E --> F[(COMMIT DB)]
    F --> G{TotalAmount == 0?}
    G -->|Có| H[Payment = paid<br/>Booking = active<br/>Redemption = applied<br/>Tạo sessions]
    H --> I[paymentRequired = false<br/>checkoutUrl = null]
    G -->|Không| J[Gọi PayOS CreatePaymentLink]
    J -->|Lỗi| K[Payment = failed<br/>Booking = cancelled<br/>Release slots + voucher]
    K --> L[500 PAYOS_CREATE_PAYMENT_FAILED]
    J -->|OK| M[Lưu checkoutUrl<br/>paymentRequired = true]
    M --> N[Learner thanh toán trên PayOS]
    N --> O{Kết quả}
    O -->|Thành công| P[Webhook / Reconcile<br/>Payment = paid<br/>Booking = active<br/>Redemption = applied<br/>Tạo sessions + wallet]
    O -->|Hủy / thất bại / hết hạn| Q[Webhook / Reconcile / Worker<br/>Payment = cancelled/failed<br/>Booking = cancelled<br/>Release slots + voucher]
```

**Điểm mấu chốt:** DB **luôn được commit trước** khi gọi PayOS → không bao giờ có checkout PayOS mà
DB không có booking.

### 24.4 Community post

```mermaid
stateDiagram-v2
    [*] --> draft: POST (publish=false)
    [*] --> published: POST (publish=true)
    published --> closed: tác giả PUT .../close
    published --> closed: đủ người (auto, khi accept application)
    closed --> published: người accepted rời đi & chưa diễn ra (auto)
    published --> expired: WORKER (15 phút/lần)
    closed --> expired: WORKER (15 phút/lần)
    published --> hidden: ADMIN PUT .../hide
    closed --> hidden: ADMIN
    expired --> hidden: ADMIN
    draft --> hidden: ADMIN
    hidden --> published: ADMIN PUT .../restore (nếu publishedAt != null)
    hidden --> draft: ADMIN PUT .../restore (nếu publishedAt == null)
    published --> deleted: tác giả DELETE hoặc ADMIN DELETE
    closed --> deleted: tác giả / ADMIN
    expired --> deleted: tác giả / ADMIN
    draft --> deleted: tác giả / ADMIN
    hidden --> deleted: tác giả / ADMIN
    deleted --> published: ADMIN PUT .../restore
```

| Transition | Ai thực hiện |
|---|---|
| `→ draft` / `→ published` | Tác giả |
| `published → closed` | Tác giả (`/close`) **hoặc** tự động khi accept đủ người |
| `closed → published` | **Tự động** khi người đã accepted rời và bài chưa diễn ra |
| `published/closed → expired` | **Worker** `CommunityPostExpiryBackgroundService` |
| `* → hidden` | **Admin** |
| `hidden/deleted → published/draft` | **Admin** |
| `* → deleted` | Tác giả **hoặc** admin |

**Điều kiện worker chuyển sang `expired`** (chạy mỗi **15 phút**, tối đa 200 bài/lần):

```
status ∈ { published, closed }
AND (
    (endAt != null AND endAt < now)
    OR (endAt == null AND startAt != null AND startAt < now - 1 ngày)
)
```

→ Bài không có `startAt` (thảo luận/câu hỏi) **không bao giờ** tự expired. ✅

> ⚠️ **Frontend recommendation:** vì worker chạy 15 phút/lần, một bài đã qua `endAt` vẫn có thể hiện
> `status: "published"` trong tối đa 15 phút. Nếu cần chính xác, hãy tự so sánh
> `endAt < now` ở client để hiển thị nhãn "Đã kết thúc".

### 24.5 Community application

```mermaid
stateDiagram-v2
    [*] --> pending: applicant POST
    pending --> accepted: chủ bài PUT .../accept
    pending --> rejected: chủ bài PUT .../reject
    pending --> cancelled: applicant DELETE .../me
    accepted --> cancelled: applicant DELETE .../me
    rejected --> [*]: TERMINAL
    cancelled --> [*]: TERMINAL
    accepted --> [*]
```

Không có worker nào tác động lên application.

### 24.6 Chat room

```mermaid
stateDiagram-v2
    [*] --> pending: requester POST /api/chat/rooms (không có booking chung)
    [*] --> active: requester POST /api/chat/rooms (đã có booking active/completed)
    pending --> active: RECEIVER PUT .../accept
    pending --> rejected: RECEIVER PUT .../reject
    rejected --> [*]: TERMINAL — vĩnh viễn
    active --> [*]
```

Không có worker nào tác động lên chat room. Block **không** đổi `status`.

---

## 25. Error handling matrix

**Backend contract.** Toàn bộ code dưới đây lấy trực tiếp từ `SporticoApp.Shared/Constants/ErrorCodes.cs`.

### 25.1 Voucher

| Error code | HTTP | Xuất hiện khi | UI message đề xuất | Frontend action |
|---|---:|---|---|---|
| `VOUCHER_NOT_FOUND` | 404 | Mã không tồn tại | "Mã giảm giá không tồn tại." | Giữ ô nhập, highlight đỏ |
| `VOUCHER_NOT_ACTIVE` | 409 | Campaign `draft`/`paused` | "Mã này hiện chưa được kích hoạt." | Như trên |
| `VOUCHER_NOT_STARTED` | 409 | `now < startAt` | "Mã giảm giá chưa đến thời gian sử dụng." | Hiện thêm `startAt` nếu có |
| `VOUCHER_EXPIRED` | 409 | Campaign `ended` **hoặc** `now > endAt` | "Mã giảm giá đã hết hạn." | Xóa mã khỏi state |
| `VOUCHER_MIN_ORDER_NOT_MET` | 409 | `price < minOrderAmount` | "Đơn hàng chưa đạt giá trị tối thiểu để dùng mã." | Hiện mức tối thiểu |
| `VOUCHER_USAGE_LIMIT_REACHED` | 409 | Hết lượt toàn hệ thống | "Mã giảm giá đã hết lượt sử dụng." | Xóa mã, cho mua giá gốc |
| `VOUCHER_LEARNER_LIMIT_REACHED` | 409 | Bạn đã dùng đủ số lần | "Bạn đã sử dụng mã này tối đa số lần cho phép." | Xóa mã |
| `VOUCHER_BUDGET_EXCEEDED` | 409 | Hết ngân sách campaign | "Mã giảm giá đã hết ngân sách." | Xóa mã, cho mua giá gốc |
| `VOUCHER_NOT_APPLICABLE` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa được ném ở đâu** | — | — |
| `VOUCHER_ALREADY_USED_FOR_BOOKING` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa được ném ở đâu** | — | — |
| `VOUCHER_CONCURRENCY_CONFLICT` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa được ném** — tranh chấp thực tế trả `CONCURRENCY_CONFLICT` | — | — |
| `VOUCHER_CAMPAIGN_NOT_FOUND` | 404 | Admin: campaign id sai | "Không tìm thấy chiến dịch." | Quay lại danh sách |
| `VOUCHER_CODE_ALREADY_EXISTS` | 409 | Admin: trùng code | "Mã này đã tồn tại." | Focus ô code |
| `VOUCHER_CAMPAIGN_HAS_REDEMPTIONS` | 409 | Admin: sửa field tài chính khi đã có redemption | "Chiến dịch đã phát sinh lượt dùng — không thể đổi mức giảm." | Reload form, khóa 4 field |
| `VOUCHER_INVALID_DATE_RANGE` | 400 | Admin: `startAt >= endAt` sau merge | "Ngày bắt đầu phải trước ngày kết thúc." | Highlight 2 ô ngày |
| `VOUCHER_CAMPAIGN_ALREADY_ENDED` | 409 | Admin: chuyển trạng thái campaign đã `ended` | "Chiến dịch đã kết thúc, không thể thay đổi." | Disable nút, refetch |

### 25.2 Community

| Error code | HTTP | Xuất hiện khi | UI message đề xuất | Frontend action |
|---|---:|---|---|---|
| `COMMUNITY_POST_NOT_FOUND` | 404 | Bài không tồn tại / `deleted` / (`hidden`\|`draft` với người không phải tác giả) | "Không tìm thấy bài viết." | Điều hướng về `/community` |
| `COMMUNITY_POST_NOT_OWNED` | 403 | Không phải tác giả (sửa/đóng/xóa/xem đơn/duyệt đơn) | "Bạn không có quyền với bài viết này." | Ẩn nút, refetch |
| `COMMUNITY_POST_NOT_PUBLISHED` | 409 | Apply vào bài không `published` | "Bài viết này không nhận yêu cầu tham gia." | Refetch detail |
| `COMMUNITY_POST_FULL` | 409 | Apply/accept khi đã đủ người | "Bài viết đã đủ người tham gia." | Refetch + disable nút |
| `COMMUNITY_POST_EXPIRED` | 409 | Apply khi `startAt <= now` | "Hoạt động đã bắt đầu, không thể tham gia." | Refetch detail |
| `COMMUNITY_POST_INVALID_STATUS` | 409 | Sửa bài không ở `draft`/`published` · Đóng bài không `published` · `maxParticipants < acceptedParticipants` | Dùng `error.message` | Refetch, hiển thị lý do |
| `COMMUNITY_POST_TOO_MANY_MEDIA` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — vượt giới hạn media trả `COMMON_VALIDATION_ERROR` | — | — |
| `COMMUNITY_COMMENT_NOT_FOUND` | 404 | Comment không tồn tại / `deleted` | "Không tìm thấy bình luận." | Refetch danh sách |
| `COMMUNITY_COMMENT_NOT_OWNED` | 403 | Sửa/xóa comment người khác | "Bạn không có quyền với bình luận này." | Ẩn nút |
| `COMMUNITY_COMMENT_NESTING_NOT_ALLOWED` | 409 | Reply vào một reply | "Chỉ có thể trả lời bình luận gốc." | Đổi target sang root comment và gửi lại |
| `COMMUNITY_COMMENTS_DISABLED` | 409 | `post.allowComments == false` | "Bình luận đã bị tắt cho bài viết này." | Ẩn ô nhập |
| `COMMUNITY_APPLICATION_NOT_FOUND` | 404 | Không có đơn / id sai | "Không tìm thấy yêu cầu tham gia." | Refetch |
| `COMMUNITY_APPLICATION_ALREADY_EXISTS` | 409 | Đã từng apply (mọi trạng thái) | "Bạn đã gửi yêu cầu cho bài viết này." | Refetch detail để lấy trạng thái thật |
| `COMMUNITY_APPLICATION_NOT_ALLOWED` | **403** | Apply vào bài của chính mình | "Bạn không thể tham gia bài viết của chính mình." | Ẩn nút |
| `COMMUNITY_APPLICATION_NOT_ALLOWED` | **409** | `postType` không nhận đơn | "Loại bài viết này không nhận yêu cầu tham gia." | Ẩn nút |
| `COMMUNITY_APPLICATION_NOT_PENDING` | 409 | Accept/reject đơn không `pending` · Hủy đơn đã `rejected` | Dùng `error.message` | Refetch danh sách đơn |
| `COMMUNITY_CONCURRENCY_CONFLICT` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — tranh chấp thật trả `CONCURRENCY_CONFLICT` | — | — |
| `SPORT_NOT_FOUND` | 404 | `sportId` không tồn tại | "Môn thể thao không hợp lệ." | Reload danh sách môn |
| `COMMON_ACCOUNT_NOT_ACTIVE` | 403 | Tài khoản không `active` khi tạo bài/comment | "Tài khoản của bạn chưa được kích hoạt." | Điều hướng trang hỗ trợ |

### 25.3 Report

| Error code | HTTP | Xuất hiện khi | UI message đề xuất | Frontend action |
|---|---:|---|---|---|
| `REPORT_NOT_FOUND` | **404** | Report id sai | "Không tìm thấy báo cáo." | Quay lại danh sách |
| `REPORT_NOT_FOUND` | **409** ⚠️ | Report đã `resolved`/`rejected` | "Báo cáo này đã được xử lý." | Refetch danh sách |
| `REPORT_INVALID_TARGET` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa được ném** | — | — |

> ⚠️ Nhắc lại: `REPORT_NOT_FOUND` có **2 HTTP status**. Phải phân biệt bằng `status`/`error.type`.

### 25.4 Chat và block

| Error code | HTTP | Xuất hiện khi | UI message đề xuất | Frontend action |
|---|---:|---|---|---|
| `CHAT_CANNOT_MESSAGE_SELF` | 403 | `targetUserId == currentUserId` | "Bạn không thể nhắn tin cho chính mình." | Ẩn nút nhắn tin ở profile của mình |
| `CHAT_TARGET_USER_NOT_FOUND` | 404 | Target không tồn tại | "Không tìm thấy người dùng." | Đóng modal |
| `CHAT_TARGET_USER_INACTIVE` | 409 | `target.status != "active"` | "Người dùng này hiện không hoạt động." | Disable nút nhắn tin |
| `CHAT_USER_BLOCKED` | 403 | Bị chặn (một trong hai chiều) khi tạo phòng / gửi tin | "Không thể nhắn tin với người dùng này." | Disable ô nhập, hiện banner |
| `CHAT_ROOM_NOT_PENDING` | 409 | Accept/reject phòng không `pending` · Receiver gửi tin khi phòng `pending` | Dùng `error.message` | Refetch phòng |
| `CHAT_ROOM_REJECTED` | 409 | Gửi tin vào phòng `rejected` | "Cuộc trò chuyện đã bị từ chối." | Chuyển phòng sang read-only |
| `CHAT_NOT_ALLOWED` | **404** | Phòng không tồn tại | "Không tìm thấy cuộc trò chuyện." | Về `/messages` |
| `CHAT_NOT_ALLOWED` | **403** | Không phải thành viên · Tự accept request của mình | Dùng `error.message` | Về `/messages` |
| `CHAT_EMPTY_MESSAGE` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — trả `COMMON_VALIDATION_ERROR` | — | — |
| `CHAT_TOO_MANY_ATTACHMENTS` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — trả `COMMON_VALIDATION_ERROR` | — | — |
| `USER_BLOCK_CANNOT_BLOCK_SELF` | 403 | Tự chặn mình | "Bạn không thể tự chặn chính mình." | Ẩn nút |
| `USER_BLOCK_ALREADY_BLOCKED` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — block là idempotent, trả `200` | — | — |
| `USER_BLOCK_NOT_FOUND` | — | ⚠️ Có trong `ErrorCodes` nhưng **chưa dùng** — unblock là idempotent, trả `200` | — | — |
| `USER_NOT_FOUND` | 404 | Block user không tồn tại | "Không tìm thấy người dùng." | Đóng modal |

> ⚠️ **9 error code trong bảng trên tồn tại trong `ErrorCodes.cs` nhưng KHÔNG BAO GIỜ được ném ra.**
> Đừng viết nhánh xử lý riêng cho chúng. Chúng được liệt kê để bạn biết là chúng vô dụng.

### 25.5 Booking / payment liên quan voucher

| Error code | HTTP | Xuất hiện khi | UI message đề xuất |
|---|---:|---|---|
| `TRAINING_PACKAGE_NOT_FOUND` | 404 | Package id sai | "Không tìm thấy gói tập." |
| `TRAINING_PACKAGE_NOT_PUBLISHED` | 409 | Gói chưa xuất bản | "Gói tập này hiện không khả dụng." |
| `MANUAL_PURCHASE_DISABLED` | 403 | Feature flag tắt | "Chức năng này không khả dụng." |
| `PAYOS_CREATE_PAYMENT_FAILED` | 500 | Không tạo được link PayOS (booking **đã bị hủy**, slot & voucher **đã được release**) | "Không thể tạo liên kết thanh toán. Vui lòng thử lại." |
| `INVALID_COMMISSION_RATE` | 500 | Cấu hình commission sai (`< 0` hoặc `> 1`) | "Có lỗi cấu hình hệ thống. Vui lòng liên hệ hỗ trợ." |
| `COMMON_FORBIDDEN` | 403 | Coach tự mua gói của mình | "Bạn không thể mua gói của chính mình." |

### 25.6 Lỗi dùng chung

| Error code | HTTP | Xuất hiện khi | Frontend action |
|---|---:|---|---|
| `COMMON_VALIDATION_ERROR` | 400 | FluentValidation / model binding fail | Hiển thị `error.details[]` dưới đúng field nếu map được, hoặc toast danh sách |
| `CONCURRENCY_CONFLICT` | 409 | `DbUpdateConcurrencyException` (tranh chấp slot cuối / lượt voucher cuối) | **Không auto-retry.** Refetch + báo user thử lại |
| `AUTH_INVALID_CREDENTIALS` | 401 | Token thiếu/sai | Refresh token, nếu fail → logout |
| `COMMON_INTERNAL_SERVER_ERROR` | 500 | Exception không lường trước | Toast lỗi chung + nút thử lại |

### 25.7 Handler chung khuyến nghị

**Frontend recommendation:**

```ts
const VI_MESSAGES: Record<string, string> = {
  VOUCHER_NOT_FOUND: 'Mã giảm giá không tồn tại.',
  VOUCHER_NOT_ACTIVE: 'Mã này hiện chưa được kích hoạt.',
  VOUCHER_NOT_STARTED: 'Mã giảm giá chưa đến thời gian sử dụng.',
  VOUCHER_EXPIRED: 'Mã giảm giá đã hết hạn.',
  VOUCHER_MIN_ORDER_NOT_MET: 'Đơn hàng chưa đạt giá trị tối thiểu để dùng mã.',
  VOUCHER_USAGE_LIMIT_REACHED: 'Mã giảm giá đã hết lượt sử dụng.',
  VOUCHER_LEARNER_LIMIT_REACHED: 'Bạn đã sử dụng mã này tối đa số lần cho phép.',
  VOUCHER_BUDGET_EXCEEDED: 'Mã giảm giá đã hết ngân sách.',
  COMMUNITY_POST_NOT_FOUND: 'Không tìm thấy bài viết.',
  COMMUNITY_POST_FULL: 'Bài viết đã đủ người tham gia.',
  COMMUNITY_POST_EXPIRED: 'Hoạt động đã bắt đầu, không thể tham gia.',
  COMMUNITY_APPLICATION_ALREADY_EXISTS: 'Bạn đã gửi yêu cầu cho bài viết này.',
  COMMUNITY_COMMENTS_DISABLED: 'Bình luận đã bị tắt cho bài viết này.',
  COMMUNITY_COMMENT_NESTING_NOT_ALLOWED: 'Chỉ có thể trả lời bình luận gốc.',
  CHAT_USER_BLOCKED: 'Không thể nhắn tin với người dùng này.',
  CHAT_ROOM_REJECTED: 'Cuộc trò chuyện đã bị từ chối.',
  CHAT_CANNOT_MESSAGE_SELF: 'Bạn không thể nhắn tin cho chính mình.',
  CHAT_TARGET_USER_INACTIVE: 'Người dùng này hiện không hoạt động.',
  CONCURRENCY_CONFLICT: 'Dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại.',
};

export function toViMessage(err: ApiError): string {
  // 2 code có nhiều nghĩa tuỳ HTTP status
  if (err.code === 'REPORT_NOT_FOUND')
    return err.status === 409 ? 'Báo cáo này đã được xử lý.' : 'Không tìm thấy báo cáo.';
  if (err.code === 'COMMUNITY_APPLICATION_NOT_ALLOWED')
    return err.status === 403
      ? 'Bạn không thể tham gia bài viết của chính mình.'
      : 'Loại bài viết này không nhận yêu cầu tham gia.';
  if (err.code === 'CHAT_NOT_ALLOWED')
    return err.status === 404 ? 'Không tìm thấy cuộc trò chuyện.' : 'Bạn không có quyền truy cập cuộc trò chuyện này.';

  return VI_MESSAGES[err.code] ?? err.message ?? 'Đã có lỗi xảy ra.';
}
```

---

## 26. Frontend route proposal

**Frontend recommendation** — toàn bộ mục này là gợi ý.

```
/community                              Feed công khai (SSR được)
/community/create                       Form tạo bài
/community/posts/[id]                   Chi tiết bài + comment
/community/posts/[id]/edit              Form sửa bài
/community/posts/[id]/applications      Chủ bài quản lý người tham gia
/community/my-posts                     GET /api/community/posts/me

/messages                               Danh sách phòng (tab Tin nhắn / Lời mời)
/messages/[roomId]                      Cửa sổ chat
/settings/blocked                       GET /api/users/me/blocked

/checkout/packages/[packageId]          Trang thanh toán có ô voucher
/checkout/payos/return                  Trang xử lý callback PayOS (gọi reconcile)
/bookings/[id]                          Chi tiết booking sau khi mua

/admin/vouchers                         Danh sách campaign
/admin/vouchers/create                  Form tạo campaign
/admin/vouchers/[id]                    Chi tiết + redemptions + nút activate/pause/end
/admin/community                        Danh sách bài (moderation)
/admin/community/posts/[id]             Chi tiết bài + comment (moderation)
/admin/reports                          Danh sách + xử lý report
```

**Bảo vệ route:**

| Nhóm route | Điều kiện |
|---|---|
| `/community`, `/community/posts/[id]` | Public — render được không token |
| `/community/create`, `/community/my-posts`, `/messages/**`, `/settings/blocked` | Cần token |
| `/checkout/**`, `/bookings/**` | Cần token + role `learner` |
| `/admin/**` | Cần token + role `admin` |

**URL PayOS return:** cấu hình phía backend (`PayOs:ReturnUrl`, `PayOs:CancelUrl`) — frontend phải
thống nhất giá trị này với backend team. Trang return **phải** gọi reconcile, xem mục 33 Flow A.

---

## 27. Component proposal

**Frontend recommendation.**

### 27.1 Voucher

| Component | Trách nhiệm |
|---|---|
| `VoucherInput` | Ô nhập + nút Áp dụng/Xóa, quản lý state `idle/validating/valid/invalid/removed/purchase_rejected` |
| `PriceSummary` | Bảng Giá gốc / Giảm giá / Tổng thanh toán |
| `CheckoutButton` | Gọi purchase, xử lý `paymentRequired`, disable khi đang xử lý |
| `VoucherErrorBanner` | Hiện khi purchase fail vì voucher, có nút "Thử lại" / "Mua giá gốc" |

### 27.2 Community

| Component | Trách nhiệm |
|---|---|
| `CommunityFeedFilters` | postType, sportId, keyword, city, level, hasAvailableSlots, sortBy |
| `CommunityPostCard` | Card feed, truncate `content`, badge status, `slotsRemaining` |
| `CommunityPostDetail` | Nội dung đầy đủ + media gallery |
| `PostMediaGallery` | Lightbox ảnh/video từ mảng `media` |
| `JoinButton` | Bảng logic ở mục 15.7 |
| `ApplicationList` | Chủ bài xem/duyệt đơn, có filter theo `status` |
| `CommentThread` | Root comment + `replies` nhúng, che comment `hidden` |
| `CommentComposer` | Ô nhập, dùng chung cho comment và reply |
| `LikeButton` | Optimistic, `currentUserReacted` + `reactionCount` |
| `ReportDialog` | Chọn `reason` từ tập cố định + `description` |
| `PostTypeBadge`, `PostStatusBadge` | Hiển thị nhãn tiếng Việt |

### 27.3 Chat

| Component | Trách nhiệm |
|---|---|
| `ChatRoomList` | 2 tab (Tin nhắn / Lời mời), sort theo `lastMessageAt` |
| `ChatRequestCard` | Nút Chấp nhận/Từ chối, chỉ khi `requestedByUserId !== me` |
| `MessageList` | Đảo mảng (`sentAt DESC` → hiển thị ASC), infinite scroll ngược |
| `MessageComposer` | Text + attachment, disable theo `status` phòng và block |
| `AttachmentUploader` | Upload lên storage → trả URL https |
| `BlockedBanner` | Hiện khi `otherUserId` nằm trong danh sách đã chặn |

### 27.4 Admin

| Component | Trách nhiệm |
|---|---|
| `VoucherCampaignForm` | Khóa 4 field tài chính khi đã có redemption |
| `VoucherCampaignStatusActions` | Nút activate/pause/end theo bảng mục 8.6 |
| `RedemptionTable` | Filter theo `status`, hiển thị `releaseReason` |
| `AdminPostTable` | Filter + `reportCount` + action theo mục 16.9 |
| `ModerationDialog` | Nhập `reason` (bắt buộc cho hide) |
| `ReportResolveDialog` | Giới hạn `actionTaken` theo `targetType` (mục 17.5) |

---

## 28. API service/type proposal

**Frontend recommendation.**

### 28.1 Cấu trúc thư mục

```
src/
  features/
    vouchers/       { api/ components/ hooks/ schemas/ types/ }
    community/      { api/ components/ hooks/ schemas/ types/ }
    chat/           { api/ components/ hooks/ types/ }
    user-blocks/    { api/ hooks/ types/ }
    admin-vouchers/ { api/ components/ hooks/ schemas/ }
    admin-community/{ api/ components/ hooks/ schemas/ }
  lib/
    api-client.ts   fetch wrapper + unwrap() + ApiError
    types/shared.ts ApiResult, PagedResult, Error
```

### 28.2 Function signatures đề xuất

```ts
// features/vouchers/api
validateVoucher(body: ValidateVoucherRequest): Promise<VoucherQuoteResponse>;

// features/checkout/api
purchaseTrainingPackagePayOs(body: PurchaseTrainingPackageRequest): Promise<PurchaseTrainingPackagePayOsResponse>;
purchaseTrainingPackageManual(body: PurchaseTrainingPackageRequest): Promise<BookingResponse>;
reconcilePayOs(body: ReconcilePayOsRequest): Promise<ReconcilePayOsResponse>;

// features/community/api
getCommunityPosts(q: CommunityPostFilterQuery): Promise<PagedResult<CommunityPostResponse>>;
getMyCommunityPosts(q: CommunityPostFilterQuery): Promise<PagedResult<CommunityPostResponse>>;
getCommunityPost(id: string): Promise<CommunityPostResponse>;
createCommunityPost(body: CreateCommunityPostRequest): Promise<CommunityPostResponse>;
updateCommunityPost(id: string, body: UpdateCommunityPostRequest): Promise<CommunityPostResponse>;
closeCommunityPost(id: string): Promise<CommunityPostResponse>;
deleteCommunityPost(id: string): Promise<{ deleted: boolean }>;
likeCommunityPost(id: string): Promise<{ liked: boolean }>;
unlikeCommunityPost(id: string): Promise<{ liked: boolean }>;

getComments(postId: string, q: PageQuery): Promise<PagedResult<CommunityCommentResponse>>;
createComment(postId: string, body: CreateCommentRequest): Promise<CommunityCommentResponse>;
createReply(commentId: string, body: CreateReplyRequest): Promise<CommunityCommentResponse>;
updateComment(commentId: string, body: UpdateCommentRequest): Promise<CommunityCommentResponse>;
deleteComment(commentId: string): Promise<{ deleted: boolean }>;

applyToCommunityPost(postId: string, body: CreateApplicationRequest): Promise<CommunityApplicationResponse>;
cancelMyApplication(postId: string): Promise<{ cancelled: boolean }>;
getApplications(postId: string, q: CommunityApplicationFilterQuery): Promise<PagedResult<CommunityApplicationResponse>>;
acceptCommunityApplication(id: string): Promise<CommunityApplicationResponse>;
rejectCommunityApplication(id: string): Promise<CommunityApplicationResponse>;

createReport(body: CreateReportRequest): Promise<ReportResponse>;

// features/chat/api
createOrGetChatRoom(body: CreateChatRoomRequest): Promise<ChatRoomResponse>;
getChatRooms(): Promise<ChatRoomResponse[]>;
acceptChatRoom(roomId: string): Promise<ChatRoomResponse>;
rejectChatRoom(roomId: string): Promise<ChatRoomResponse>;
getChatMessages(roomId: string, q: PageQuery): Promise<PagedResult<ChatMessageResponse>>;
sendChatMessage(roomId: string, body: SendMessageRequest): Promise<ChatMessageResponse>;

// features/user-blocks/api
blockUser(userId: string, body?: BlockUserRequest): Promise<{ blocked: boolean }>;
unblockUser(userId: string): Promise<{ blocked: boolean }>;
getBlockedUsers(): Promise<BlockedUserResponse[]>;

// features/admin-vouchers/api
createVoucherCampaign(body: CreateVoucherCampaignRequest): Promise<VoucherCampaignResponse>;
getVoucherCampaigns(q: VoucherCampaignFilterQuery): Promise<PagedResult<VoucherCampaignResponse>>;
getVoucherCampaign(id: string): Promise<VoucherCampaignResponse>;
updateVoucherCampaign(id: string, body: UpdateVoucherCampaignRequest): Promise<VoucherCampaignResponse>;
activateVoucherCampaign(id: string): Promise<VoucherCampaignResponse>;
pauseVoucherCampaign(id: string): Promise<VoucherCampaignResponse>;
endVoucherCampaign(id: string): Promise<VoucherCampaignResponse>;
getVoucherRedemptions(id: string, q: VoucherRedemptionFilterQuery): Promise<PagedResult<VoucherRedemptionResponse>>;

// features/admin-community/api
getAdminCommunityPosts(q: AdminCommunityPostFilterQuery): Promise<PagedResult<AdminCommunityPostResponse>>;
getAdminCommunityPost(id: string): Promise<CommunityPostResponse>;
hideCommunityPost(id: string, body: HideContentRequest): Promise<CommunityPostResponse>;
restoreCommunityPost(id: string): Promise<CommunityPostResponse>;
adminDeleteCommunityPost(id: string): Promise<{ deleted: boolean }>;
getAdminPostComments(id: string, q: PageQuery): Promise<PagedResult<CommunityCommentResponse>>;
hideComment(id: string, body: HideContentRequest): Promise<CommunityCommentResponse>;
restoreComment(id: string): Promise<CommunityCommentResponse>;
adminDeleteComment(id: string): Promise<{ deleted: boolean }>;
getAdminReports(q: AdminReportFilterQuery): Promise<PagedResult<ReportResponse>>;
resolveReport(id: string, body: ResolveReportRequest): Promise<ReportResponse>;
```

---

## 29. Cache invalidation

**Frontend recommendation.**

### 29.1 Query keys

```ts
['community-posts', filters]                 // feed
['community-post', postId]                   // detail
['my-community-posts', filters]
['community-comments', postId, pageNumber]
['community-applications', postId, filters]
['chat-rooms']
['chat-messages', roomId, pageNumber]
['blocked-users']
['notifications', filters]
['notifications-unread-count']
['admin-voucher-campaigns', filters]
['admin-voucher-campaign', campaignId]
['admin-voucher-redemptions', campaignId, filters]
['admin-community-posts', filters]
['admin-community-post', postId]
['admin-community-comments', postId, pageNumber]
['admin-reports', filters]
['booking', bookingId]
```

> **Voucher validate KHÔNG cache.** Dùng `useMutation`, không `useQuery`. Kết quả có thể hết hiệu
> lực bất cứ lúc nào.

### 29.2 Bảng invalidation

| Mutation | Query cần invalidate |
|---|---|
| Tạo bài | `['community-posts']`, `['my-community-posts']` |
| Sửa bài | `['community-post', id]`, `['community-posts']`, `['my-community-posts']` |
| Đóng bài | `['community-post', id]`, `['community-posts']`, `['my-community-posts']` |
| Xóa bài | `['community-posts']`, `['my-community-posts']`; **remove** `['community-post', id]` |
| Like / Unlike | `['community-post', id]`, `['community-posts']` |
| Thêm comment | `['community-comments', postId]`, `['community-post', postId]` (đổi `commentCount`) |
| Thêm reply | `['community-comments', postId]`, `['community-post', postId]` |
| Sửa comment | `['community-comments', postId]` |
| Xóa comment | `['community-comments', postId]`, `['community-post', postId]` |
| Apply | `['community-post', postId]`, `['community-posts']` |
| Hủy đơn | `['community-post', postId]`, `['community-posts']` |
| Accept đơn | `['community-applications', postId]`, `['community-post', postId]`, `['community-posts']` |
| Reject đơn | `['community-applications', postId]` |
| Tạo report | `['admin-reports']` (nếu là admin đang mở) |
| Tạo/lấy phòng chat | `['chat-rooms']` |
| Accept/reject phòng | `['chat-rooms']`, `['chat-messages', roomId]` |
| Gửi tin nhắn | `['chat-messages', roomId]`, `['chat-rooms']` |
| Block / Unblock | `['blocked-users']`, `['chat-rooms']` |
| Purchase (PayOS/manual) | `['booking']`, danh sách booking của learner |
| Reconcile | `['booking', bookingId]`, danh sách booking |
| Tạo campaign | `['admin-voucher-campaigns']` |
| Sửa campaign | `['admin-voucher-campaigns']`, `['admin-voucher-campaign', id]` |
| Activate/pause/end | `['admin-voucher-campaigns']`, `['admin-voucher-campaign', id]` |
| Admin hide/restore/delete bài | `['admin-community-posts']`, `['admin-community-post', id]`, `['community-posts']`, `['community-post', id]` |
| Admin hide/restore/delete comment | `['admin-community-comments', postId]`, `['community-comments', postId]` |
| Resolve report | `['admin-reports']`, `['admin-community-posts']`, `['community-posts']` |

### 29.3 Lưu ý về `viewCount`

`GET /api/community/posts/{id}` **tăng view count mỗi lần gọi**. Vì vậy:

```ts
useQuery({
  queryKey: ['community-post', postId],
  queryFn: () => api.getCommunityPost(postId),
  refetchOnWindowFocus: false,   // BẮT BUỘC
  refetchOnMount: false,
  staleTime: 60_000,
});
```

Sau mutation, dùng `invalidateQueries` có chủ đích thay vì để tự động refetch liên tục.

---

## 30. Optimistic UI

**Frontend recommendation.**

### 30.1 ✅ Nên optimistic

| Mutation | Lý do an toàn |
|---|---|
| **Like / Unlike bài** | Idempotent tuyệt đối; counter được clamp `>= 0` server-side; không có business rule nào có thể fail bất ngờ ngoài `404` |
| **Thêm comment** (tạm hiện) | Chỉ append vào danh sách, rollback dễ. Cần chờ response để lấy `id` thật trước khi cho sửa/xóa |
| **Đánh dấu notification đã đọc** | Idempotent |

### 30.2 ❌ KHÔNG optimistic

| Mutation | Lý do |
|---|---|
| **Accept application** | Có thể fail vì `COMMUNITY_POST_FULL` hoặc `CONCURRENCY_CONFLICT`. Optimistic sẽ hiện sai `acceptedParticipants` và trạng thái `closed` |
| **Reject application** | Có thể fail `COMMUNITY_APPLICATION_NOT_PENDING` (người kia vừa hủy) |
| **Apply / hủy đơn** | Nhiều rule server-side (`full`, `expired`, `already_exists`) |
| **Validate voucher** | Là mutation đọc, kết quả **phải** từ server |
| **Purchase** | Giao dịch tiền thật, tuyệt đối không đoán trước |
| **Reserve voucher** | Có tranh chấp lượt cuối |
| **Mọi thao tác admin moderation** | Có audit trail, cần nguồn sự thật từ server |
| **Activate/pause/end campaign** | Có thể `409 VOUCHER_CAMPAIGN_ALREADY_ENDED` |
| **Sửa campaign** | Có thể `409 VOUCHER_CAMPAIGN_HAS_REDEMPTIONS` |
| **Gửi tin nhắn** | Có thể `403 CHAT_USER_BLOCKED` hoặc `409` theo trạng thái phòng |
| **Accept/reject chat request** | Có thể `409 CHAT_ROOM_NOT_PENDING` |
| **Đếm participant** (`acceptedParticipants`) | Server là nguồn sự thật duy nhất, có concurrency token |

### 30.3 Mẫu "pending message" cho chat

Thay vì optimistic thật, dùng **pending state riêng** để UX vẫn mượt mà không sai dữ liệu:

```ts
// Frontend recommendation
type PendingMessage = { tempId: string; content: string; sentAt: string; status: 'sending' | 'failed' };

// Render: [...serverMessages, ...pendingMessages]
// - 'sending': hiện mờ + spinner nhỏ
// - 'failed'  : hiện đỏ + nút "Gửi lại"
// Khi API trả về thành công → xoá pending theo tempId, invalidate ['chat-messages', roomId]
```

Không merge pending vào cache của server → không bao giờ hiển thị tin "đã gửi" khi thực ra bị `403`.

---

## 31. Form validation

**Frontend recommendation** — các schema dưới đây **bám sát** validator backend. Nguyên tắc:
**frontend không được lỏng hơn backend ở field bắt buộc.**

### 31.1 Voucher (learner)

```ts
export const validateVoucherSchema = z.object({
  code: z.string().trim().min(1, 'Vui lòng nhập mã giảm giá').max(64, 'Mã tối đa 64 ký tự'),
  trainingPackageId: z.string().uuid(),
});
```

### 31.2 Purchase

```ts
export const purchaseSchema = z.object({
  trainingPackageId: z.string().uuid(),
  voucherCode: z.string().trim().max(64).nullable().optional(),
});
```

### 31.3 Tạo voucher campaign (admin)

```ts
export const createVoucherCampaignSchema = z.object({
  code: z.string().trim().min(1, 'Bắt buộc').max(64)
    .regex(/^[A-Za-z0-9_-]+$/, 'Chỉ được dùng chữ, số, "-" và "_"'),
  name: z.string().trim().min(1, 'Bắt buộc').max(200),
  description: z.string().max(2000).nullable().optional(),
  discountType: z.enum(['fixed_amount', 'percentage']),
  discountValue: z.number().positive('Phải lớn hơn 0'),
  maxDiscountAmount: z.number().positive().nullable().optional(),
  minOrderAmount: z.number().min(0).nullable().optional(),
  startAt: z.string().datetime().nullable().optional(),
  endAt: z.string().datetime().nullable().optional(),
  maxUsesTotal: z.number().int().positive().nullable().optional(),
  maxUsesPerLearner: z.number().int().positive().nullable().optional(),
  budgetAmount: z.number().positive().nullable().optional(),
})
  .refine(v => v.discountType !== 'percentage' || v.discountValue <= 100,
    { message: 'Phần trăm giảm phải trong khoảng 0–100', path: ['discountValue'] })
  .refine(v => !v.startAt || !v.endAt || new Date(v.startAt) < new Date(v.endAt),
    { message: 'Ngày bắt đầu phải trước ngày kết thúc', path: ['endAt'] });
```

### 31.4 Tạo community post

```ts
const RECRUITMENT = ['looking_for_players','looking_for_team','training_partner','friendly_match'] as const;

export const mediaItemSchema = z.object({
  mediaType: z.enum(['image', 'video']),
  url: z.string().url().startsWith('https://', 'Chỉ chấp nhận URL https'),
  thumbnailUrl: z.string().url().nullable().optional(),
  mimeType: z.string().nullable().optional(),
  fileSize: z.number().int().positive().nullable().optional(),
  width: z.number().int().positive().nullable().optional(),
  height: z.number().int().positive().nullable().optional(),
  durationSeconds: z.number().int().positive().nullable().optional(),
});

export const createCommunityPostSchema = z.object({
  postType: z.enum(['looking_for_players','looking_for_team','training_partner',
                    'friendly_match','event','discussion','question']),
  sportId: z.number().int().positive().nullable().optional(),
  title: z.string().trim().min(1, 'Bắt buộc').max(200),
  content: z.string().trim().min(1, 'Bắt buộc').max(5000),
  locationName: z.string().max(200).nullable().optional(),
  address: z.string().max(300).nullable().optional(),
  latitude: z.number().min(-90).max(90).nullable().optional(),   // FE chặt hơn BE (BE không kiểm)
  longitude: z.number().min(-180).max(180).nullable().optional(),
  startAt: z.string().datetime().nullable().optional(),
  endAt: z.string().datetime().nullable().optional(),
  maxParticipants: z.number().int().nullable().optional(),
  level: z.enum(['beginner','intermediate','advanced','all']).nullable().optional(), // FE chặt hơn BE
  feePerPerson: z.number().min(0).nullable().optional(),
  allowComments: z.boolean().default(true),
  publish: z.boolean().default(true),
  media: z.array(mediaItemSchema).max(8, 'Tối đa 8 media')
    .refine(m => m.filter(x => x.mediaType === 'video').length <= 1, 'Tối đa 1 video')
    .nullable().optional(),
})
  .refine(v => !v.startAt || !v.endAt || new Date(v.startAt) < new Date(v.endAt),
    { message: 'Thời gian bắt đầu phải trước thời gian kết thúc', path: ['endAt'] })
  .refine(v => !RECRUITMENT.includes(v.postType as never) || v.sportId != null,
    { message: 'Vui lòng chọn môn thể thao', path: ['sportId'] })
  .refine(v => !RECRUITMENT.includes(v.postType as never) || v.startAt != null,
    { message: 'Vui lòng chọn thời gian bắt đầu', path: ['startAt'] })
  .refine(v => !RECRUITMENT.includes(v.postType as never) || (v.maxParticipants != null && v.maxParticipants >= 2),
    { message: 'Số người tối đa phải từ 2 trở lên (tính cả bạn)', path: ['maxParticipants'] });
```

> ⚠️ `latitude`/`longitude` và `level`: schema trên **chặt hơn backend** (backend không kiểm phạm vi
> tọa độ và cho `level` là string ≤ 30 tự do). Đây là lựa chọn có chủ đích để dữ liệu sạch — chấp nhận
> được vì không nới lỏng field bắt buộc nào.

### 31.5 Sửa community post

```ts
export const updateCommunityPostSchema = z.object({
  title: z.string().trim().min(1).max(200).nullable().optional(),
  content: z.string().trim().min(1).max(5000).nullable().optional(),
  locationName: z.string().max(200).nullable().optional(),
  address: z.string().max(300).nullable().optional(),
  latitude: z.number().min(-90).max(90).nullable().optional(),
  longitude: z.number().min(-180).max(180).nullable().optional(),
  startAt: z.string().datetime().nullable().optional(),
  endAt: z.string().datetime().nullable().optional(),
  maxParticipants: z.number().int().min(1).nullable().optional(),  // BE: >= 1 khi update
  level: z.enum(['beginner','intermediate','advanced','all']).nullable().optional(),
  feePerPerson: z.number().min(0).nullable().optional(),
  allowComments: z.boolean().nullable().optional(),
  media: z.array(mediaItemSchema).max(8)
    .refine(m => m.filter(x => x.mediaType === 'video').length <= 1, 'Tối đa 1 video')
    .nullable().optional(),
}).refine(v => !v.startAt || !v.endAt || new Date(v.startAt) < new Date(v.endAt),
  { message: 'Thời gian bắt đầu phải trước thời gian kết thúc', path: ['endAt'] });
```

> ⚠️ Nhớ: gửi `media: null` khi user **không** chỉnh media (gửi `[]` sẽ **xóa hết**).

### 31.6 Comment, reply, application, chat, report, moderation

```ts
export const commentSchema = z.object({
  content: z.string().trim().min(1, 'Vui lòng nhập nội dung').max(2000, 'Tối đa 2000 ký tự'),
});
export const replySchema = commentSchema;
export const updateCommentSchema = commentSchema;

export const createApplicationSchema = z.object({
  message: z.string().trim().max(500, 'Tối đa 500 ký tự').nullable().optional(),
});

export const createChatRoomSchema = z.object({
  targetUserId: z.string().uuid(),
  sourceType: z.enum(['booking', 'community_post']).nullable().optional(),  // FE chặt hơn BE
  sourceId: z.string().uuid().nullable().optional(),
});

export const sendMessageSchema = z.object({
  content: z.string().trim().max(2000, 'Tối đa 2000 ký tự').nullable().optional(),
  attachments: z.array(z.object({
    fileUrl: z.string().url().startsWith('https://', 'Chỉ chấp nhận URL https'), // FE chặt hơn BE (BE cho http)
    fileType: z.enum(['image', 'video', 'file', 'audio']).nullable().optional(),
  })).max(5, 'Tối đa 5 tệp đính kèm').nullable().optional(),
}).refine(
  v => (v.content && v.content.trim().length > 0) || (v.attachments && v.attachments.length > 0),
  { message: 'Vui lòng nhập nội dung hoặc đính kèm tệp', path: ['content'] },
);

export const blockUserSchema = z.object({
  reason: z.string().trim().max(500, 'Tối đa 500 ký tự').nullable().optional(), // BE KHÔNG kiểm — FE bắt buộc chặn
});

export const createReportSchema = z.object({
  targetType: z.enum(['community_post', 'community_comment', 'chat_message']),
  targetId: z.string().uuid(),
  reason: z.enum(['spam','harassment','inappropriate_content','fake_information','scam','other']), // FE chặt hơn BE
  description: z.string().trim().max(1000).nullable().optional(),
});

export const hideContentSchema = z.object({
  reason: z.string().trim().min(1, 'Vui lòng nhập lý do').max(1000),
});

export const resolveReportSchema = z.object({
  status: z.enum(['resolved', 'rejected']),
  resolutionNote: z.string().trim().max(1000).nullable().optional(),
  actionTaken: z.enum(['none','post_hidden','post_deleted','comment_hidden','comment_deleted']),
});
```

### 31.7 Bảng đối chiếu Backend vs Frontend

| Field | Backend | Frontend recommendation | Chặt hơn? |
|---|---|---|---|
| `blockUser.reason` | ❌ **Không validate** (DB 500 ký tự) | ≤ 500 | ✅ Bắt buộc chặt hơn |
| `report.reason` | String tự do ≤ 200 | Enum 6 giá trị | ✅ |
| `post.level` | String tự do ≤ 30 | Enum 4 giá trị | ✅ |
| `latitude`/`longitude` | Không kiểm phạm vi | `-90..90` / `-180..180` | ✅ |
| `chat attachment fileUrl` | `http` hoặc `https` | Chỉ `https` | ✅ |
| `chat attachment fileType` | Không validate | Enum 4 giá trị | ✅ |
| `sourceType` | String tự do ≤ 30 | Enum 2 giá trị | ✅ |
| `admin report pageSize` | ❌ **Không validate** | ≤ 100 | ✅ Bắt buộc chặt hơn |
| `admin filter status/postType/sortBy` | ❌ Không validate | Enum theo constants | ✅ |
| Mọi field bắt buộc khác | Có validate | Giống hệt | — |

---

## 32. Empty/loading/error states

**Frontend recommendation.**

### 32.1 Community Feed

```
Loading (lần đầu):
- 6 skeleton card (ảnh 16:9 + 2 dòng text + hàng badge).

Loading (đổi filter/trang):
- Giữ danh sách cũ ở opacity 0.5 + spinner nhỏ ở góc. KHÔNG unmount.

Empty (có filter):
- "Không tìm thấy bài viết phù hợp."
- Nút [Xóa bộ lọc]
- Nút [Tạo bài viết]

Empty (không filter):
- "Chưa có bài viết nào trong cộng đồng."
- Nút [Tạo bài viết đầu tiên]

Error:
- "Không tải được danh sách bài viết."
- Nút [Thử lại] — GIỮ NGUYÊN filter hiện tại.
```

### 32.2 Community Post Detail

```
Loading:  skeleton toàn trang (header + gallery + nội dung + comment).
404 (COMMUNITY_POST_NOT_FOUND):
- "Bài viết không tồn tại hoặc đã bị gỡ."
- Nút [Về trang cộng đồng]
Comment loading:      3 skeleton dòng.
Comment empty:        "Chưa có bình luận nào. Hãy là người đầu tiên!"
Comment bị tắt:       "Tác giả đã tắt bình luận cho bài viết này." (khi allowComments = false)
Comment hidden:       "Bình luận đã bị ẩn bởi quản trị viên." (BẮT BUỘC — xem mục 13.2)
Comment deleted:      "Bình luận đã bị xóa" (backend đã trả sẵn chuỗi này)
```

### 32.3 Voucher / Checkout

| Trạng thái | Hiển thị |
|---|---|
| Đang validate voucher | Spinner trong nút "Áp dụng", input disabled |
| Voucher không hợp lệ | Text đỏ dưới input = `toViMessage(err)`, giá giữ nguyên giá gốc |
| Voucher đã hết hạn | Text đỏ + tự xóa mã khỏi state sau 3 s |
| Voucher hết lượt **khi purchase** | Banner vàng: "Mã giảm giá vừa hết lượt. Bạn có muốn tiếp tục với giá gốc?" + [Mua giá gốc] [Thử mã khác] |
| Đang purchase | Nút "Thanh toán" → spinner + disabled, khóa cả ô voucher |
| Tạo checkout URL thất bại (`PAYOS_CREATE_PAYMENT_FAILED`) | "Không thể tạo liên kết thanh toán. Đơn hàng đã được hủy, bạn có thể thử lại." + [Thử lại] |
| Đang chờ xác nhận thanh toán | Spinner + "Đang xác nhận thanh toán…" + đếm số lần thử |
| Booking `pending_payment` quá lâu | "Thanh toán chưa hoàn tất. Liên kết sẽ hết hạn lúc {expiredAt}." + [Mở lại liên kết] |

### 32.4 Chat

```
Danh sách phòng — Loading:  5 skeleton row.
Danh sách phòng — Empty:    "Bạn chưa có cuộc trò chuyện nào."
                            Nút [Khám phá cộng đồng] → /community
Tab Lời mời — Empty:        "Không có lời mời trò chuyện nào."

Cửa sổ chat — Loading:      skeleton bong bóng chat.
Cửa sổ chat — Empty:        "Chưa có tin nhắn. Hãy gửi lời chào!"

Phòng pending (bạn là requester):
- Banner xanh nhạt: "Đang chờ đối phương chấp nhận lời mời trò chuyện."
- Ô nhập BẬT (bạn vẫn gửi được).

Phòng pending (bạn là receiver):
- Banner: "{Tên} muốn trò chuyện với bạn."
- Nút [Chấp nhận] [Từ chối]
- Ô nhập TẮT.

Phòng rejected:
- Banner xám: "Cuộc trò chuyện đã bị từ chối."
- Ô nhập TẮT vĩnh viễn. Vẫn xem được lịch sử.

Bị chặn (bạn chặn họ — biết được từ /api/users/me/blocked):
- Banner đỏ nhạt: "Bạn đã chặn người này." + nút [Bỏ chặn]
- Ô nhập TẮT.

Bị chặn (họ chặn bạn — chỉ biết sau khi gửi thất bại):
- Toast: "Không thể nhắn tin với người dùng này."
- Ô nhập TẮT sau lỗi đầu tiên.

Gửi tin thất bại:
- Bong bóng đỏ + icon cảnh báo + nút [Gửi lại] / [Xóa]

Upload attachment thất bại:
- Thumbnail có overlay đỏ + [Thử lại] [Xóa]
- KHÔNG gửi message cho tới khi mọi attachment upload xong.
```

### 32.5 Admin

```
Danh sách campaign — Empty: "Chưa có chiến dịch voucher nào." + [Tạo chiến dịch]
Redemptions — Empty:        "Chiến dịch chưa có lượt sử dụng nào."
Danh sách bài — Empty:      "Không có bài viết phù hợp bộ lọc."
reportedOnly + Empty:       "Không có bài viết nào đang bị báo cáo." 🎉
Danh sách report — Empty:   "Không có báo cáo nào cần xử lý." 🎉

409 VOUCHER_CAMPAIGN_HAS_REDEMPTIONS:
- Modal: "Chiến dịch đã phát sinh lượt sử dụng nên không thể đổi mức giảm."
- Nút [Chỉ lưu thông tin khác] (gửi lại không kèm 4 field tài chính)
- Nút [Kết thúc & tạo chiến dịch mới]

409 CONCURRENCY_CONFLICT:
- Toast: "Dữ liệu vừa thay đổi. Đang tải lại…"
- Tự invalidate query, KHÔNG auto-retry mutation.

409 REPORT_NOT_FOUND (đã xử lý):
- Toast: "Báo cáo này đã được xử lý trước đó."
- Đóng dialog + refetch danh sách.
```

---

## 33. End-to-end user flows

### Flow A — Mua gói có voucher qua PayOS

```mermaid
sequenceDiagram
    autonumber
    participant U as Learner
    participant FE as Frontend
    participant BE as Backend
    participant P as PayOS

    U->>FE: Mở /checkout/packages/{packageId}
    FE->>BE: GET /api/training-packages/{id}
    BE-->>FE: Thông tin gói (price)
    U->>FE: Nhập "WELCOME10" → bấm Áp dụng
    FE->>BE: POST /api/vouchers/validate
    BE-->>FE: 200 { originalAmount, discountAmount, totalAmount }
    FE-->>U: Hiển thị Giá gốc / Giảm giá / Tổng thanh toán
    U->>FE: Bấm Thanh toán
    FE->>BE: POST /api/bookings/purchase/payos { trainingPackageId, voucherCode }
    Note over BE: Tính LẠI voucher server-side<br/>Tạo booking + reserve slots + reserve voucher<br/>COMMIT rồi mới gọi PayOS
    BE->>P: CreatePaymentLink(amount = totalAmount)
    P-->>BE: checkoutUrl, orderCode, expiredAt
    BE-->>FE: 200 { paymentRequired: true, checkoutUrl, orderCode, bookingId }
    FE->>U: Redirect tới checkoutUrl
    U->>P: Thanh toán
    P->>BE: POST /api/payments/payos/webhook (server-to-server)
    P-->>U: Redirect về ReturnUrl
    U->>FE: Mở /checkout/payos/return?orderCode=...
    FE->>BE: POST /api/payments/payos/reconcile { orderCode }
    BE-->>FE: 200 { paymentStatus, bookingStatus, activated }
    alt activated = true
        FE->>U: Điều hướng /bookings/{bookingId} + toast thành công
    else paymentStatus = pending
        FE->>FE: Thử lại sau 3s (tối đa ~5 lần)
    else cancelled / failed
        FE->>U: Trang "Thanh toán không thành công" + [Thử lại]
    end
```

**Quy tắc bắt buộc:**

> ⚠️ **Frontend TUYỆT ĐỐI KHÔNG được tin query param do PayOS trả về** (`status=PAID`, `code=00`…)
> để kết luận thanh toán thành công. **Phải gọi `POST /api/payments/payos/reconcile`** và chỉ tin
> `activated` / `bookingStatus` từ backend.

```ts
// Frontend recommendation — trang /checkout/payos/return
async function confirmPayment(orderCode: number) {
  for (let attempt = 0; attempt < 5; attempt++) {
    const r = await api.reconcilePayOs({ orderCode });
    if (r.activated) return { ok: true, bookingId: r.bookingId };
    if (r.paymentStatus === 'cancelled' || r.paymentStatus === 'failed')
      return { ok: false, reason: r.paymentStatus };
    await new Promise(res => setTimeout(res, 3000));   // webhook có thể tới chậm
  }
  return { ok: false, reason: 'timeout' };  // → hiện "Đang xử lý, vui lòng kiểm tra lại sau"
}
```

**Xử lý trường hợp voucher hết lượt giữa validate và purchase:**

```ts
try {
  const res = await api.purchaseTrainingPackagePayOs({ trainingPackageId, voucherCode });
  // …
} catch (e) {
  if (e instanceof ApiError && e.code.startsWith('VOUCHER_')) {
    setVoucherState('purchase_rejected');
    setVoucherError(toViMessage(e));
    // KHÔNG tự động mua lại — để user chọn [Mua giá gốc] hoặc [Thử mã khác]
  }
}
```

### Flow B — Voucher giảm 100%

```
1. FE: POST /api/vouchers/validate  → totalAmount = 0
2. FE: hiển thị "Miễn phí", nút "Nhận gói miễn phí"
3. FE: POST /api/bookings/purchase/payos { trainingPackageId, voucherCode }
4. BE: tạo booking → payment (method="voucher", status="paid") → redemption applied
       → tạo training sessions → tạo wallet coach → gửi notification
       KHÔNG gọi PayOS
5. BE → FE: 200 { paymentRequired: false, checkoutUrl: null, orderCode: null,
                  bookingStatus: "active", paymentStatus: "paid" }
6. FE: KIỂM TRA paymentRequired === false → điều hướng thẳng /bookings/{bookingId}
       KHÔNG mở checkoutUrl (nó là null)
       KHÔNG gọi reconcile (không có orderCode)
```

> ⚠️ Đừng dùng `totalAmount === 0` để phát hiện trường hợp này — response purchase **không có**
> field `totalAmount`. Dùng `paymentRequired`.

### Flow C — Community: tuyển người chơi

```mermaid
sequenceDiagram
    autonumber
    participant O as Chủ bài
    participant A as Người xin tham gia
    participant FE as Frontend
    participant BE as Backend

    O->>BE: POST /api/community/posts (looking_for_players, maxParticipants=4)
    Note over BE: acceptedParticipants = 1 (tính cả chủ bài)<br/>status = published
    BE-->>O: 200 CommunityPostResponse (slotsRemaining = 3)

    A->>BE: GET /api/community/posts/{id}
    BE-->>A: canApply = true
    A->>BE: POST /api/community/posts/{id}/applications { message }
    BE-->>A: 200 { status: "pending" }
    BE->>O: Notification (type=system, "New join request")

    O->>BE: GET /api/community/posts/{id}/applications?status=pending
    BE-->>O: PagedResult<CommunityApplicationResponse>
    O->>BE: PUT /api/community/applications/{appId}/accept
    Note over BE: acceptedParticipants++ (=2)<br/>version++<br/>nếu đạt max → status = closed
    BE-->>O: 200 { status: "accepted" }
    BE->>A: Notification (type=system, "Your join request was accepted")

    Note over FE: Invalidate ['community-applications', postId],<br/>['community-post', postId], ['community-posts']

    A->>BE: POST /api/chat/rooms { targetUserId: ownerId, sourceType: "community_post", sourceId: postId }
    Note over BE: Không có booking chung → status = pending
    BE-->>A: 200 { status: "pending" }
    BE->>O: Notification (type=message, "New chat request")
    O->>BE: PUT /api/chat/rooms/{roomId}/accept
    BE-->>O: 200 { status: "active" }
    Note over A,O: Giờ cả hai mới nhắn tin qua lại được
```

**Khi bài đủ người:** request accept cuối cùng tự động đặt `status = "closed"`. Feed vẫn hiển thị
bài nhưng `canApply = false` và `slotsRemaining = 0`.

**Khi một người đã accepted rời đi:** `DELETE /api/community/posts/{postId}/applications/me` →
`acceptedParticipants--`, và nếu bài đang `closed` + chưa diễn ra → tự về `published`.

### Flow D — Chat request

```
1. User A: POST /api/chat/rooms { targetUserId: B }
   → Nếu A và B đã có booking active/completed chung: status = "active" NGAY
   → Ngược lại: status = "pending"

2. (pending) User A vẫn gửi được tin nhắn:
   POST /api/chat/rooms/{roomId}/messages { content: "Chào bạn" }  → 200

3. (pending) User B KHÔNG gửi được:
   POST /api/chat/rooms/{roomId}/messages  → 409 CHAT_ROOM_NOT_PENDING
   Nhưng B ĐỌC được toàn bộ tin của A: GET .../messages → 200

4. User B chấp nhận:
   PUT /api/chat/rooms/{roomId}/accept  → 200 { status: "active" }
   → Notification cho A: "Chat request accepted"

5. Cả hai nhắn tin bình thường.

   HOẶC User B từ chối:
   PUT /api/chat/rooms/{roomId}/reject  → 200 { status: "rejected" }
   → Cả hai KHÔNG gửi được tin nữa (409 CHAT_ROOM_REJECTED), vĩnh viễn.
   → Lịch sử vẫn đọc được.
   → A gọi lại POST /api/chat/rooms sẽ nhận LẠI phòng rejected đó (không tạo mới).
```

### Flow E — Admin moderation

```mermaid
sequenceDiagram
    autonumber
    participant AD as Admin
    participant BE as Backend
    participant AU as Tác giả bài

    AD->>BE: GET /api/admin/community/posts?reportedOnly=true&pageSize=20
    BE-->>AD: PagedResult<AdminCommunityPostResponse> (có reportCount)
    AD->>BE: GET /api/admin/community/posts/{id}
    BE-->>AD: CommunityPostResponse đầy đủ (canModerate = true)
    AD->>BE: GET /api/admin/community/posts/{id}/comments
    BE-->>AD: Toàn bộ comment (phẳng, gồm hidden/deleted)
    AD->>BE: GET /api/admin/community/reports?status=pending
    BE-->>AD: PagedResult<ReportResponse>

    AD->>BE: PUT /api/admin/community/reports/{reportId}/resolve<br/>{ status:"resolved", resolutionNote:"...", actionTaken:"post_hidden" }
    Note over BE: 1) Lưu report (status/handledBy/handledAt/note/action)<br/>2) Gọi HidePostAsync(targetId, reason = resolutionNote)
    BE-->>AD: 200 ReportResponse
    BE->>AU: Notification (type=report, "Your post was hidden")

    Note over AD: Invalidate ['admin-reports'], ['admin-community-posts'],<br/>['community-posts'], ['community-post', id]
```

**Sau moderation, feed công khai phải được invalidate** vì bài `hidden`/`deleted` biến mất khỏi
`GET /api/community/posts`.

> ⚠️ Nếu `actionTaken` không khớp `targetType` (ví dụ `post_hidden` cho report `community_comment`),
> report **vẫn được đánh dấu resolved** rồi request trả `404`. Frontend phải invalidate danh sách
> report kể cả khi gặp lỗi.

---

## 34. Testing checklist

**Frontend recommendation** — checklist để QA/agent tự kiểm.

### 34.1 Voucher

- [ ] Mua gói **không nhập voucher** → `totalAmount = originalAmount`, `discountAmount = 0`
- [ ] Voucher `fixed_amount` 50.000₫ trên gói 1.000.000₫ → giảm đúng 50.000₫
- [ ] Voucher `fixed_amount` 2.000.000₫ trên gói 1.000.000₫ → giảm **tối đa 1.000.000₫** (clamp)
- [ ] Voucher `percentage` 10% → giảm 100.000₫
- [ ] Voucher `percentage` 50% có `maxDiscountAmount = 100.000` → giảm đúng 100.000₫ (bị cap)
- [ ] Voucher có `minOrderAmount` cao hơn giá gói → `VOUCHER_MIN_ORDER_NOT_MET`
- [ ] Nhập mã sai → `VOUCHER_NOT_FOUND`, hiển thị message tiếng Việt
- [ ] Mã campaign `draft`/`paused` → `VOUCHER_NOT_ACTIVE`
- [ ] Mã campaign `ended` hoặc quá `endAt` → `VOUCHER_EXPIRED`
- [ ] Mã chưa tới `startAt` → `VOUCHER_NOT_STARTED`
- [ ] **Mã chữ thường** `welcome10` → vẫn hợp lệ (citext), response trả `"WELCOME10"`
- [ ] Nhập mã có khoảng trắng thừa → vẫn hợp lệ (backend trim)
- [ ] Bấm "Xóa mã" → tổng tiền quay về giá gốc, không gửi `voucherCode` khi purchase
- [ ] Voucher hết lượt **sau khi validate thành công** → purchase trả `VOUCHER_USAGE_LIMIT_REACHED`,
      UI hiện banner "Mua giá gốc / Thử mã khác"
- [ ] **Voucher 100%** → `paymentRequired = false`, `checkoutUrl = null`, điều hướng thẳng booking
- [ ] Redirect PayOS bình thường khi `paymentRequired = true`
- [ ] Refresh trang `/checkout/payos/return` nhiều lần → reconcile idempotent, không tạo booking trùng
- [ ] Hủy trên PayOS → reconcile trả `cancelled`, hiện trang thất bại
- [ ] Booking response hiển thị `voucherCode`, `originalAmount`, `discountAmount` đúng
- [ ] Booking cũ (không voucher) → `originalAmount = totalAmount`, `discountAmount = 0`, `voucherCode = null`
- [ ] Màn hình learner **không** hiển thị `platformFeeAmount` / `coachReceiveAmount`

### 34.2 Community

- [ ] Tạo bài từng loại: 7 `postType`
- [ ] Bài recruitment thiếu `sportId`/`startAt`/`maxParticipants` → `400` với 3 message rõ ràng
- [ ] `maxParticipants = 1` cho bài recruitment → `400` (yêu cầu ≥ 2)
- [ ] `startAt >= endAt` → `400`
- [ ] Tạo bài với `publish: false` → `status = "draft"`, chỉ thấy ở `/community/my-posts`
- [ ] User `inactive`/`banned` tạo bài → `403 COMMON_ACCOUNT_NOT_ACTIVE`
- [ ] Sửa bài của mình → OK
- [ ] Sửa bài người khác → `403 COMMUNITY_POST_NOT_OWNED`
- [ ] Sửa bài `closed`/`expired` → `409 COMMUNITY_POST_INVALID_STATUS`
- [ ] Giảm `maxParticipants` xuống dưới `acceptedParticipants` → `409`
- [ ] Update với `media: null` → media giữ nguyên
- [ ] Update với `media: []` → **media bị xóa hết**
- [ ] Update với `media: [a,b]` → thay thế toàn bộ, `orderIndex` = 0,1
- [ ] Media URL `http://` → `400`; `https://` → OK
- [ ] 9 media → `400`; 2 video → `400`
- [ ] Xóa mềm bài → biến mất khỏi feed, `GET detail` trả `404` **kể cả tác giả**
- [ ] Feed **không** chứa bài `draft`/`hidden`/`deleted`
- [ ] Feed **có** chứa bài `closed`/`expired` → badge hiển thị đúng
- [ ] `/me` trả cả `draft` và `hidden`
- [ ] `/me`: `author.fullName` là `""` → UI dùng thông tin session thay thế
- [ ] Filter `city` khớp `locationName` (không khớp `address`)
- [ ] `sortBy=upcoming` → bài không có `startAt` xuống cuối
- [ ] `sortBy` sai → `400`
- [ ] Comment gốc → OK; reply cấp 1 → OK
- [ ] Reply vào một reply → `409 COMMUNITY_COMMENT_NESTING_NOT_ALLOWED`, FE tự đổi sang root và gửi lại
- [ ] Comment trên bài `allowComments=false` → `409 COMMUNITY_COMMENTS_DISABLED`
- [ ] Xóa comment của mình → hiện "Bình luận đã bị xóa" (nếu là reply) / biến mất (nếu là root)
- [ ] Comment `hidden` → **FE tự che** bằng "Bình luận đã bị ẩn bởi quản trị viên"
- [ ] Like 2 lần liên tiếp → `reactionCount` chỉ tăng 1
- [ ] Unlike 2 lần liên tiếp → `reactionCount` không âm
- [ ] Apply vào bài của chính mình → `403`
- [ ] Apply vào bài `discussion` → `409`
- [ ] Apply vào bài đã đủ người → `409 COMMUNITY_POST_FULL`
- [ ] Apply vào bài đã bắt đầu → `409 COMMUNITY_POST_EXPIRED`
- [ ] Apply 2 lần → `409 COMMUNITY_APPLICATION_ALREADY_EXISTS`
- [ ] Sau `rejected`/`cancelled` → apply lại vẫn `409` (không thể apply lại)
- [ ] Chỉ chủ bài xem được `GET .../applications`; người khác → `403`
- [ ] Accept đơn → `acceptedParticipants++`; đủ người → `status = "closed"`
- [ ] 2 tab cùng accept slot cuối → một thành công, một `409` (`CONCURRENCY_CONFLICT` hoặc `COMMUNITY_POST_FULL`)
- [ ] Người accepted hủy đơn → `acceptedParticipants--`, bài `closed` tự về `published`
- [ ] Admin hide → biến mất khỏi feed, tác giả vẫn xem detail được
- [ ] Admin delete → tác giả **không** xem detail được nữa
- [ ] Admin restore bài `closed` bị ẩn → trở thành `published` (**không** về `closed`)
- [ ] Report bài/comment → `200`; report lại lần 2 → trả về report cũ (idempotent)

### 34.3 Chat

- [ ] Learner ↔ learner tạo phòng → `status = "pending"`
- [ ] Learner ↔ coach **đã có booking** → `status = "active"` ngay
- [ ] Tạo phòng với chính mình → `403 CHAT_CANNOT_MESSAGE_SELF`
- [ ] Tạo phòng 2 lần với cùng người → trả **cùng một `roomId`**, không tạo mới
- [ ] Gửi `coachId` (legacy) → vẫn tạo phòng được
- [ ] Gửi cả `targetUserId` và `coachId` → `targetUserId` thắng
- [ ] Target `inactive` → `409 CHAT_TARGET_USER_INACTIVE`
- [ ] (pending) requester gửi tin → `200`
- [ ] (pending) receiver gửi tin → `409 CHAT_ROOM_NOT_PENDING`
- [ ] (pending) receiver **đọc** tin → `200`
- [ ] Requester tự accept → `403 CHAT_NOT_ALLOWED`
- [ ] Receiver accept → `status = "active"`, requester nhận notification
- [ ] Receiver reject → cả hai gửi tin đều `409 CHAT_ROOM_REJECTED`
- [ ] Gửi tin chỉ text → OK
- [ ] Gửi tin chỉ attachment → OK, `content` trả về `""`
- [ ] Gửi tin rỗng (không text, không attachment) → `400`
- [ ] 6 attachment → `400`
- [ ] Block user → tạo phòng mới `403 CHAT_USER_BLOCKED`
- [ ] Block user → gửi tin trong phòng cũ `403 CHAT_USER_BLOCKED`
- [ ] Block user → **vẫn đọc được** lịch sử chat
- [ ] Block user → **vẫn xin tham gia community post được** (limitation đã biết)
- [ ] Unblock → gửi tin lại được ngay
- [ ] Block chính mình → `403 USER_BLOCK_CANNOT_BLOCK_SELF`
- [ ] Block 2 lần → idempotent `200`
- [ ] Unblock người chưa chặn → idempotent `200`
- [ ] `isRead` **luôn `false`** — UI không dựa vào nó
- [ ] Danh sách phòng sort theo `lastMessageAt` giảm dần
- [ ] Tin nhắn trả `sentAt` giảm dần — UI đảo lại khi render
- [ ] Chat coach ↔ learner cũ (trước migration) vẫn hoạt động (`status = "active"`)
- [ ] Polling chạy khi tab visible, dừng khi ẩn
- [ ] Dedupe tin nhắn theo `message.id` khi merge poll + optimistic

### 34.4 Admin

- [ ] Tạo campaign → `status = "draft"`
- [ ] Trùng code (khác hoa/thường) → `409 VOUCHER_CODE_ALREADY_EXISTS`
- [ ] `code` có ký tự đặc biệt → `400`
- [ ] `percentage` với `discountValue = 150` → `400`
- [ ] Activate → `active`; Pause → `paused`; End → `ended`
- [ ] Sau `ended`, mọi transition → `409 VOUCHER_CAMPAIGN_ALREADY_ENDED`
- [ ] Sửa campaign đã có redemption với `discountValue` → `409 VOUCHER_CAMPAIGN_HAS_REDEMPTIONS`
- [ ] Sửa campaign đã có redemption chỉ với `name` → `200`
- [ ] Danh sách redemptions hiển thị `releaseReason` khi `status = "released"`
- [ ] Admin list `reportedOnly=true` → chỉ bài có report `pending`/`reviewing`
- [ ] `reportCount` khớp số report đang mở
- [ ] Hide không có `reason` → `400`
- [ ] Resolve report với `actionTaken` sai loại target → hiện lỗi + refetch (không crash)
- [ ] Resolve report đã xử lý → `409` (code `REPORT_NOT_FOUND`) → message "đã được xử lý"
- [ ] `pageSize` admin ≤ 100 (frontend tự chặn — backend không validate ở report list)

---

## 35. Known limitations

Đây là danh sách **trung thực** các hạn chế của backend hiện tại. Frontend phải biết để xử lý hoặc
chấp nhận.

### 35.1 Bảo mật và dữ liệu

| # | Hạn chế | Ảnh hưởng | Cách xử lý phía frontend |
|---|---|---|---|
| 1 | **Media không có ownership validation.** Backend chỉ kiểm URL là absolute HTTPS. Không whitelist host, không kiểm MIME/kích thước thật, không kiểm file tồn tại. | Bất kỳ URL HTTPS nào cũng render được trên feed công khai | Chỉ gửi URL từ storage chính thức. Không cho user dán URL. Đặt CSP `img-src`/`media-src`. |
| 2 | **Chat attachment chấp nhận cả `http://`** (community media chỉ `https`) | Mixed content | Frontend chỉ gửi `https` |
| 3 | **Report `chat_message` không xác minh gì** — không kiểm message tồn tại, không kiểm reporter thuộc phòng | Có thể tạo report rác | Chỉ cho report từ UI phòng chat, lấy `targetId` từ message có sẵn |
| 4 | **Không chặn tự report chính mình** | User report nội dung của mình | Ẩn nút Báo cáo trên nội dung của chính mình |
| 5 | **`BlockUserRequest.reason` không có validator.** Cột DB 500 ký tự | Gửi > 500 ký tự → `500` từ Postgres | Frontend bắt buộc giới hạn ≤ 500 |
| 6 | **`GET /api/admin/community/reports` không validate `pageNumber`/`pageSize`** | `pageSize=100000` không bị chặn | Frontend tự giới hạn ≤ 100 |
| 7 | **Backend trả mọi field tài chính cho mọi role** (`platformFeeAmount`, `coachReceiveAmount`) | Learner có thể xem được qua DevTools | Ẩn ở UI; **không coi là bảo mật** |

### 35.2 Chức năng thiếu

| # | Hạn chế | Ảnh hưởng |
|---|---|---|
| 8 | **Không có SignalR / WebSocket / SSE.** | Chat, notification phải polling. Không có typing indicator, presence, realtime |
| 9 | **Không có mark-as-read cho chat.** `Message.IsRead` **không bao giờ** được set `true`; không có `unreadCount` | Không làm được badge tin chưa đọc chính xác, không có read receipt. Chỉ workaround bằng localStorage |
| 10 | **`ChatRoomResponse` không có `otherUser` object, `lastMessage`, `unreadCount`** — chỉ có `otherUserId` và `lastMessageAt` | Phải gọi API user riêng để lấy tên/avatar; không hiển thị được preview tin cuối |
| 11 | **`NotificationResponse` không có `referenceId`/`referenceType`** | **Không deep-link được** từ notification đến bài/phòng chat cụ thể. Chỉ tới trang danh sách |
| 12 | **Không có endpoint "đơn xin tham gia của tôi"** | Applicant chỉ biết trạng thái qua `currentUserApplicationStatus` trên từng bài |
| 13 | **Không có upload endpoint** | Client phải tự tích hợp storage |
| 14 | **Không có endpoint mở lại bài `closed`** thủ công (chỉ tự động khi participant rời) | Chủ bài đóng nhầm thì không mở lại được |
| 15 | **Không có endpoint đổi `draft` → `published`** (`UpdateCommunityPostRequest` không có `publish`) | Bài nháp không xuất bản được sau khi tạo |

### 35.3 Hành vi cần lưu ý

| # | Hạn chế | Ảnh hưởng |
|---|---|---|
| 16 | **Block chỉ có tác dụng với chat.** Không chặn apply community post, không lọc feed/comment | Người bị chặn vẫn xin tham gia và bình luận được. Frontend tự lọc client-side nếu cần |
| 17 | **Comment `hidden` vẫn trả nội dung gốc** ở endpoint công khai | **Frontend BẮT BUỘC tự che** |
| 18 | **Admin xóa comment không giảm `post.commentCount`** | Số comment hiển thị cao hơn thực tế |
| 19 | **Restore bài không khôi phục status trước đó** — luôn về `published`/`draft` | Bài `closed`/`expired` bị ẩn rồi restore sẽ thành `published` |
| 20 | **Hide bài `deleted` sẽ đổi status sang `hidden`** nhưng `deletedAt` vẫn còn | Trạng thái hỗn hợp. Frontend nên disable nút Hide khi `deleted` |
| 21 | **Không apply lại được sau `rejected`/`cancelled`** (unique constraint) | Trạng thái vĩnh viễn |
| 22 | **Phòng chat `rejected` là vĩnh viễn** — không tạo lại được | Hai user không bao giờ chat lại được |
| 23 | **`GET /api/community/posts/{id}` tăng `viewCount` mỗi lần gọi**, không chống trùng | Refetch/refresh làm tăng view. Đặt `refetchOnWindowFocus: false` |
| 24 | **`viewCount` trong response là giá trị TRƯỚC khi tăng** | Lệch 1 so với DB |
| 25 | **`/me` không `Include(Author)`** → `author.fullName = ""` | Dùng thông tin session thay thế |
| 26 | **Application community đã accepted KHÔNG làm phòng chat active ngay** — chỉ booking mới có tác dụng | Người được duyệt vẫn phải qua chat request |
| 27 | **Worker expiry chạy 15 phút/lần** | Bài quá `endAt` vẫn hiện `published` tối đa 15 phút. Frontend tự so `endAt < now` để hiển thị |
| 28 | **`sourceType`/`sourceId` chỉ lưu lần tạo phòng đầu tiên**, không cập nhật khi mở lại | Ngữ cảnh có thể lỗi thời |
| 29 | **Admin không xem được nội dung gốc của comment đã xóa mềm** — API trả placeholder cho mọi caller | Cần truy vấn DB trực tiếp nếu cần điều tra |
| 30 | **9 error code tồn tại trong `ErrorCodes.cs` nhưng không bao giờ được ném:** `VOUCHER_NOT_APPLICABLE`, `VOUCHER_ALREADY_USED_FOR_BOOKING`, `VOUCHER_CONCURRENCY_CONFLICT`, `COMMUNITY_POST_TOO_MANY_MEDIA`, `COMMUNITY_CONCURRENCY_CONFLICT`, `REPORT_INVALID_TARGET`, `CHAT_EMPTY_MESSAGE`, `CHAT_TOO_MANY_ATTACHMENTS`, `USER_BLOCK_ALREADY_BLOCKED`, `USER_BLOCK_NOT_FOUND` | Đừng viết nhánh xử lý cho chúng |
| 31 | **`REPORT_NOT_FOUND` và `COMMUNITY_APPLICATION_NOT_ALLOWED` và `CHAT_NOT_ALLOWED` mỗi code có 2 HTTP status khác nghĩa** | Phải phân biệt bằng `status`/`error.type`, không chỉ bằng `code` |
| 32 | **Nhiều filter admin không được validate** (`status`, `postType`, `sortBy`, redemption `status`) | Giá trị sai → danh sách rỗng thay vì `400`. Frontend tự giới hạn dropdown |
| 33 | **Toàn bộ `title`/`content` của notification là tiếng Anh** | Frontend nên tự sinh chuỗi tiếng Việt theo `type` |
| 34 | **`message` trong `ReconcilePayOsResponse` là tiếng Anh** | Map theo `paymentStatus`/`activated` thay vì hiển thị thô |

### 35.4 Hiệu năng

| # | Hạn chế | Ảnh hưởng |
|---|---|---|
| 35 | **N+1 query trong feed community khi đã đăng nhập** — `MapListAsync` gọi 2 query cho mỗi bài | `pageSize = 100` → ~200 query phụ. **Dùng `pageSize` ≤ 20** |
| 36 | **N+1 query trong admin post list** — `reportCount` tính riêng cho từng bài | **Dùng `pageSize` ≤ 20** |
| 37 | **Search dùng `ILIKE '%kw%'`, không có index trgm được sử dụng, không full-text, không ranking** | Chậm khi dữ liệu lớn; không có fuzzy match |
| 38 | **Không có preview tin nhắn cuối trong danh sách phòng** | Muốn có phải gọi `GET messages?pageSize=1` cho từng phòng |

### 35.5 Backward compatibility tạm thời

| # | Contract | Trạng thái |
|---|---|---|
| 39 | `CreateChatRoomRequest.coachId` | **Legacy**, vẫn nhận. Frontend mới **chỉ nên dùng `targetUserId`**. Có thể bị bỏ trong tương lai |
| 40 | `PurchaseTrainingPackagePayOsResponse.status` | Trùng lặp với `paymentStatus`. Frontend mới dùng `paymentStatus` |
| 41 | `PaymentStatisticsResponse.totalRevenue` / `platformRevenue` | Alias của `netCollected` / `platformGrossFee`. Frontend mới dùng tên mới + `platformNetRevenue` |
| 42 | `CommunityPostResponse.canModerate` | Luôn `false` ở endpoint community. Đừng dựa vào nó ngoài trang admin |

---

## 36. Exact TypeScript interfaces

**Backend contract.** Toàn bộ type dưới đây khớp chính xác với DTO C# và Swagger schema.
Copy nguyên vào `src/lib/types/` là dùng được.

```typescript
// ─────────────────────────────────────────────────────────────
// Shared envelope
// ─────────────────────────────────────────────────────────────

export type ErrorTypeName =
  | 'Validation'
  | 'NotFound'
  | 'Unauthorized'
  | 'Forbidden'
  | 'Conflict'
  | 'Failure';

export interface ApiError {
  code: string;
  message: string;
  type: ErrorTypeName;
  details: string[] | null;
}

export interface ApiResult<T> {
  isSuccess: boolean;
  data: T | null;
  error: ApiError | null;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface PageQuery {
  pageNumber?: number;
  pageSize?: number;
}

// ─────────────────────────────────────────────────────────────
// Voucher — enums
// ─────────────────────────────────────────────────────────────

export type VoucherDiscountType = 'fixed_amount' | 'percentage';

export type VoucherCampaignStatus = 'draft' | 'active' | 'paused' | 'ended';

export type VoucherRedemptionStatus = 'reserved' | 'applied' | 'released';

// ─────────────────────────────────────────────────────────────
// Voucher — learner
// ─────────────────────────────────────────────────────────────

export interface ValidateVoucherRequest {
  code: string;
  trainingPackageId: string;
}

export interface VoucherQuoteResponse {
  code: string;
  originalAmount: number;
  discountAmount: number;
  totalAmount: number;
  discountType: VoucherDiscountType;
  discountValue: number;
  maxDiscountAmount: number | null;
}

// ─────────────────────────────────────────────────────────────
// Booking purchase
// ─────────────────────────────────────────────────────────────

/** Dùng chung cho POST /api/bookings/purchase/payos và /manual. */
export interface PurchaseTrainingPackageRequest {
  trainingPackageId: string;
  voucherCode?: string | null;
}

export type PaymentStatus = 'pending' | 'paid' | 'failed' | 'cancelled';

export type BookingStatus =
  | 'pending_payment'
  | 'active'
  | 'completed'
  | 'cancelled'
  | 'refunded';

export interface PurchaseTrainingPackagePayOsResponse {
  bookingId: string;
  paymentId: string;
  /** null khi voucher giảm 100% — PayOS không được gọi. */
  orderCode: number | null;
  /** null khi voucher giảm 100%. */
  checkoutUrl: string | null;
  /** Legacy; luôn bằng paymentStatus. */
  status: PaymentStatus;
  /** Read-only alias của status. */
  paymentStatus: PaymentStatus;
  /** true → phải redirect tới checkoutUrl. false → booking đã active. */
  paymentRequired: boolean;
  bookingStatus: BookingStatus;
  expiredAt: string | null;
}

export interface BookingResponse {
  id: string;
  learnerId: string;
  coachId: string;
  trainingPackageId: string;
  trainingPackageTitle: string;
  totalAmount: number;
  originalAmount: number;
  discountAmount: number;
  voucherCampaignId: string | null;
  voucherCode: string | null;
  platformFeeRate: number;
  platformFeeAmount: number;
  coachReceiveAmount: number;
  perSessionCoachAmount: number;
  totalSessions: number;
  completedSessions: number;
  usedSessions: number;
  remainingSessions: number;
  canBookSession: boolean;
  sessionCountsByStatus: Record<string, number> | null;
  status: BookingStatus;
  paidAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
  expiresAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ReconcilePayOsRequest {
  orderCode?: number | null;
  paymentId?: string | null;
}

export interface ReconcilePayOsResponse {
  paymentId: string;
  orderCode: number | null;
  paymentStatus: PaymentStatus;
  bookingId: string | null;
  bookingStatus: BookingStatus | null;
  activated: boolean;
  payOsStatus: string | null;
  /** Tiếng Anh — nên map lại phía frontend. */
  message: string;
}

// ─────────────────────────────────────────────────────────────
// Voucher — admin
// ─────────────────────────────────────────────────────────────

export interface CreateVoucherCampaignRequest {
  code: string;
  name: string;
  description?: string | null;
  discountType: VoucherDiscountType;
  discountValue: number;
  maxDiscountAmount?: number | null;
  minOrderAmount?: number | null;
  startAt?: string | null;
  endAt?: string | null;
  maxUsesTotal?: number | null;
  maxUsesPerLearner?: number | null;
  budgetAmount?: number | null;
}

export interface UpdateVoucherCampaignRequest {
  name?: string | null;
  description?: string | null;
  /** Bị từ chối (409) nếu campaign đã có redemption. */
  discountType?: VoucherDiscountType | null;
  /** Bị từ chối (409) nếu campaign đã có redemption. */
  discountValue?: number | null;
  /** Bị từ chối (409) nếu campaign đã có redemption. */
  maxDiscountAmount?: number | null;
  /** Bị từ chối (409) nếu campaign đã có redemption. */
  minOrderAmount?: number | null;
  startAt?: string | null;
  endAt?: string | null;
  maxUsesTotal?: number | null;
  maxUsesPerLearner?: number | null;
  budgetAmount?: number | null;
}

export interface VoucherCampaignFilterQuery extends PageQuery {
  status?: VoucherCampaignStatus;
  keyword?: string;
}

export interface VoucherRedemptionFilterQuery extends PageQuery {
  status?: VoucherRedemptionStatus;
}

export interface VoucherCampaignResponse {
  id: string;
  code: string;
  name: string;
  description: string | null;
  discountType: VoucherDiscountType;
  discountValue: number;
  maxDiscountAmount: number | null;
  minOrderAmount: number | null;
  startAt: string | null;
  endAt: string | null;
  status: VoucherCampaignStatus;
  maxUsesTotal: number | null;
  maxUsesPerLearner: number | null;
  reservedCount: number;
  usedCount: number;
  budgetAmount: number | null;
  reservedDiscountAmount: number;
  usedDiscountAmount: number;
  createdByUserId: string;
  updatedByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface VoucherRedemptionResponse {
  id: string;
  voucherCampaignId: string;
  bookingId: string;
  learnerId: string;
  paymentId: string | null;
  status: VoucherRedemptionStatus;
  originalAmount: number;
  discountAmount: number;
  reservedAt: string;
  expiresAt: string | null;
  appliedAt: string | null;
  releasedAt: string | null;
  /** vd: "payment_expired", "payos_link_creation_failed". String tự do. */
  releaseReason: string | null;
}

// ─────────────────────────────────────────────────────────────
// Community — enums
// ─────────────────────────────────────────────────────────────

export type CommunityPostType =
  | 'looking_for_players'
  | 'looking_for_team'
  | 'training_partner'
  | 'friendly_match'
  | 'event'
  | 'discussion'
  | 'question';

export type CommunityPostStatus =
  | 'draft'
  | 'published'
  | 'closed'
  | 'expired'
  | 'hidden'
  | 'deleted';

export type CommunityCommentStatus = 'active' | 'hidden' | 'deleted';

export type CommunityApplicationStatus =
  | 'pending'
  | 'accepted'
  | 'rejected'
  | 'cancelled';

export type CommunityMediaType = 'image' | 'video';

export type CommunityPostSortBy = 'latest' | 'upcoming' | 'most_discussed';

/**
 * Backend KHÔNG validate `level` theo enum — nó là string tự do ≤ 30 ký tự.
 * Đây là tập giá trị do frontend quy ước.
 */
export type CommunityLevel = 'beginner' | 'intermediate' | 'advanced' | 'all';

// ─────────────────────────────────────────────────────────────
// Community — shared shapes
// ─────────────────────────────────────────────────────────────

export interface CommunityPostAuthorResponse {
  id: string;
  /** Chuỗi rỗng "" ở GET /api/community/posts/me (repository không Include Author). */
  fullName: string;
  avatarUrl: string | null;
}

export interface CommunityPostMediaRequest {
  mediaType: CommunityMediaType;
  /** Bắt buộc absolute https URL. */
  url: string;
  thumbnailUrl?: string | null;
  mimeType?: string | null;
  fileSize?: number | null;
  width?: number | null;
  height?: number | null;
  durationSeconds?: number | null;
}

/** Response CHỈ có 5 field — mimeType/fileSize/width/height/durationSeconds không được trả về. */
export interface CommunityPostMediaResponse {
  id: string;
  mediaType: CommunityMediaType;
  url: string;
  thumbnailUrl: string | null;
  orderIndex: number;
}

// ─────────────────────────────────────────────────────────────
// Community — post
// ─────────────────────────────────────────────────────────────

/** Dùng cho CẢ feed lẫn detail — backend không có schema riêng. */
export interface CommunityPostResponse {
  id: string;
  author: CommunityPostAuthorResponse;
  sportId: number | null;
  sportName: string | null;
  postType: CommunityPostType;
  title: string;
  /** FULL content — không có contentPreview. Frontend tự truncate cho card. */
  content: string;
  locationName: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  startAt: string | null;
  endAt: string | null;
  maxParticipants: number | null;
  acceptedParticipants: number;
  /** null khi maxParticipants == null. */
  slotsRemaining: number | null;
  level: string | null;
  feePerPerson: number | null;
  status: CommunityPostStatus;
  allowComments: boolean;
  commentCount: number;
  reactionCount: number;
  applicationCount: number;
  viewCount: number;
  media: CommunityPostMediaResponse[];
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
  /** boolean, KHÔNG phải string. MVP chỉ có "like". */
  currentUserReacted: boolean;
  currentUserApplicationStatus: CommunityApplicationStatus | null;
  canApply: boolean;
  canEdit: boolean;
  /** Luôn false ở endpoint community; true chỉ ở endpoint admin. */
  canModerate: boolean;
}

export interface CreateCommunityPostRequest {
  postType: CommunityPostType;
  sportId?: number | null;
  title: string;
  content: string;
  locationName?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  startAt?: string | null;
  endAt?: string | null;
  maxParticipants?: number | null;
  level?: string | null;
  feePerPerson?: number | null;
  /** Mặc định true. */
  allowComments?: boolean;
  /** Mặc định true. false → status = "draft". */
  publish?: boolean;
  media?: CommunityPostMediaRequest[] | null;
}

export interface UpdateCommunityPostRequest {
  title?: string | null;
  content?: string | null;
  locationName?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  startAt?: string | null;
  endAt?: string | null;
  maxParticipants?: number | null;
  level?: string | null;
  feePerPerson?: number | null;
  allowComments?: boolean | null;
  /** null = giữ nguyên · [] = XÓA HẾT · [..] = thay thế toàn bộ. */
  media?: CommunityPostMediaRequest[] | null;
}

export interface CommunityPostFilterQuery extends PageQuery {
  postType?: CommunityPostType;
  sportId?: number;
  keyword?: string;
  /** Thực tế lọc ILIKE trên locationName, không phải address. */
  city?: string;
  fromDate?: string;
  toDate?: string;
  level?: string;
  hasAvailableSlots?: boolean;
  authorId?: string;
  followingOnly?: boolean;
  sortBy?: CommunityPostSortBy;
}

// ─────────────────────────────────────────────────────────────
// Community — comment
// ─────────────────────────────────────────────────────────────

export interface CommunityCommentResponse {
  id: string;
  postId: string;
  author: CommunityPostAuthorResponse;
  parentCommentId: string | null;
  /** "Bình luận đã bị xóa" khi status === "deleted". Nội dung THẬT khi status === "hidden". */
  content: string;
  status: CommunityCommentStatus;
  replyCount: number;
  /** Reply nhúng trực tiếp, sort createdAt ASC. Luôn [] ở endpoint admin. */
  replies: CommunityCommentResponse[];
  canEdit: boolean;
  /** false ở endpoint community; true ở endpoint admin. */
  canModerate: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommentRequest {
  content: string;
}

export interface CreateReplyRequest {
  content: string;
}

export interface UpdateCommentRequest {
  content: string;
}

// ─────────────────────────────────────────────────────────────
// Community — application
// ─────────────────────────────────────────────────────────────

export interface CreateApplicationRequest {
  message?: string | null;
}

export interface CommunityApplicationFilterQuery extends PageQuery {
  status?: CommunityApplicationStatus;
}

export interface CommunityApplicationResponse {
  id: string;
  postId: string;
  applicant: CommunityPostAuthorResponse;
  message: string | null;
  status: CommunityApplicationStatus;
  createdAt: string;
  respondedAt: string | null;
  cancelledAt: string | null;
}

// ─────────────────────────────────────────────────────────────
// Community — action results (Result<object>)
// ─────────────────────────────────────────────────────────────

export interface LikeResult { liked: boolean; }
export interface DeleteResult { deleted: boolean; }
export interface CancelResult { cancelled: boolean; }
export interface BlockResult { blocked: boolean; }

// ─────────────────────────────────────────────────────────────
// Admin community
// ─────────────────────────────────────────────────────────────

export interface AdminCommunityPostFilterQuery extends PageQuery {
  status?: CommunityPostStatus;
  postType?: CommunityPostType;
  sportId?: number;
  authorId?: string;
  keyword?: string;
  reportedOnly?: boolean;
  /** Lọc theo createdAt (khác feed: feed lọc startAt). */
  fromDate?: string;
  toDate?: string;
  sortBy?: 'latest' | 'most_discussed';
}

/** Schema RÚT GỌN — chỉ 13 field. Muốn đầy đủ phải gọi GET /api/admin/community/posts/{id}. */
export interface AdminCommunityPostResponse {
  id: string;
  author: CommunityPostAuthorResponse;
  postType: CommunityPostType;
  title: string;
  status: CommunityPostStatus;
  moderationReason: string | null;
  /** Số report có status pending hoặc reviewing. */
  reportCount: number;
  commentCount: number;
  reactionCount: number;
  applicationCount: number;
  createdAt: string;
  publishedAt: string | null;
  hiddenAt: string | null;
  deletedAt: string | null;
}

export interface HideContentRequest {
  /** Bắt buộc, ≤ 1000 ký tự. */
  reason: string;
}

// ─────────────────────────────────────────────────────────────
// Report
// ─────────────────────────────────────────────────────────────

/** POST /api/reports chỉ chấp nhận 3 giá trị này (user/review có endpoint riêng). */
export type ReportableTargetType =
  | 'community_post'
  | 'community_comment'
  | 'chat_message';

export type ReportTargetType = 'user' | 'review' | ReportableTargetType;

export type ReportStatus = 'pending' | 'reviewing' | 'resolved' | 'rejected';

export type ReportAction =
  | 'none'
  | 'review_hidden'
  | 'review_deleted'
  | 'post_hidden'
  | 'post_deleted'
  | 'comment_hidden'
  | 'comment_deleted';

export interface CreateReportRequest {
  targetType: ReportableTargetType;
  targetId: string;
  /** String tự do ≤ 200. Frontend nên dùng tập cố định. */
  reason: string;
  description?: string | null;
}

export interface AdminReportFilterQuery extends PageQuery {
  targetType?: ReportTargetType;
  status?: ReportStatus;
}

export interface ResolveReportRequest {
  status: 'resolved' | 'rejected';
  resolutionNote?: string | null;
  /** KHÔNG được backend validate. Frontend phải tự giới hạn theo targetType. */
  actionTaken: ReportAction;
}

export interface ReportResponse {
  id: string;
  /** Chỉ có id — không có object reporter với tên/avatar. */
  reporterId: string;
  targetType: ReportTargetType;
  targetId: string | null;
  reason: string;
  description: string | null;
  status: ReportStatus;
  handledByUserId: string | null;
  handledAt: string | null;
  resolutionNote: string | null;
  actionTaken: ReportAction | null;
  createdAt: string;
}

// ─────────────────────────────────────────────────────────────
// Chat
// ─────────────────────────────────────────────────────────────

export type ChatRoomStatus = 'pending' | 'active' | 'rejected';

/** Backend nhận string tự do ≤ 30; đây là tập giá trị quy ước. */
export type ChatSourceType = 'booking' | 'community_post';

export interface CreateChatRoomRequest {
  /** Field ưu tiên. Thắng coachId khi gửi cả hai. */
  targetUserId?: string | null;
  /** Legacy — vẫn được chấp nhận. Frontend mới không nên dùng. */
  coachId?: string | null;
  sourceType?: string | null;
  sourceId?: string | null;
}

/**
 * KHÔNG có otherUser (object), unreadCount, lastMessage.
 * Chỉ có otherUserId và lastMessageAt.
 */
export interface ChatRoomResponse {
  id: string;
  user1Id: string;
  user2Id: string;
  /** Người còn lại so với caller. Luôn có giá trị vì mọi endpoint chat đều cần auth. */
  otherUserId: string | null;
  status: ChatRoomStatus;
  requestedByUserId: string | null;
  requestedAt: string | null;
  acceptedAt: string | null;
  rejectedAt: string | null;
  lastMessageAt: string | null;
  sourceType: string | null;
  sourceId: string | null;
  createdAt: string;
}

export interface ChatMessageAttachmentResponse {
  id: string;
  fileUrl: string;
  fileType: string | null;
}

export interface ChatMessageResponse {
  id: string;
  roomId: string;
  senderId: string;
  /** "" khi chỉ gửi attachment (không bao giờ null). */
  content: string;
  /** LUÔN false — backend không bao giờ set true. Không dùng cho badge chưa đọc. */
  isRead: boolean;
  sentAt: string;
  attachments: ChatMessageAttachmentResponse[];
}

export interface SendMessageAttachmentRequest {
  /** Backend chấp nhận http hoặc https. Frontend nên chỉ gửi https. */
  fileUrl: string;
  fileType?: string | null;
}

export interface SendMessageRequest {
  /** Optional khi có ít nhất một attachment. ≤ 2000 ký tự. */
  content?: string | null;
  /** Tối đa 5. */
  attachments?: SendMessageAttachmentRequest[] | null;
}

// ─────────────────────────────────────────────────────────────
// User block
// ─────────────────────────────────────────────────────────────

export interface BlockUserRequest {
  /** Backend KHÔNG validate độ dài. Cột DB 500 ký tự — frontend phải tự giới hạn. */
  reason?: string | null;
}

export interface BlockedUserResponse {
  userId: string;
  fullName: string;
  avatarUrl: string | null;
  /** Thời điểm chặn. */
  createdAt: string;
  reason: string | null;
}

// ─────────────────────────────────────────────────────────────
// Notification (module có sẵn, không đổi)
// ─────────────────────────────────────────────────────────────

export type NotificationType =
  | 'message'
  | 'review'
  | 'follow'
  | 'payment'
  | 'package'
  | 'post'
  | 'system'
  | 'report'
  | 'booking'
  | 'training_package'
  | 'training_session'
  | 'training_plan'
  | 'wallet';

/** KHÔNG có referenceId / referenceType → không deep-link được. */
export interface NotificationResponse {
  id: string;
  /** Tiếng Anh. */
  title: string;
  /** Tiếng Anh. */
  content: string | null;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationFilterQuery {
  isRead?: boolean;
  type?: NotificationType;
  pageNumber?: number;
  /** Mặc định 10 (khác các endpoint khác dùng 20). */
  pageSize?: number;
}

// ─────────────────────────────────────────────────────────────
// Admin payment statistics (field mới do voucher)
// ─────────────────────────────────────────────────────────────

export interface PaymentStatisticsVoucherFields {
  /** SUM(booking.OriginalAmount) — giá gói trước voucher. */
  grossPackageValue: number;
  /** SUM(booking.DiscountAmount) — nền tảng tài trợ. */
  totalDiscount: number;
  /** = totalRevenue. grossPackageValue - totalDiscount. */
  netCollected: number;
  /** = platformRevenue. SUM(booking.PlatformFeeAmount) trên giá GỐC. */
  platformGrossFee: number;
  /** LỢI NHUẬN THẬT: netCollected - coachRevenue. */
  platformNetRevenue: number;
}
```

### 36.1 Type guard tiện dụng

```typescript
export const RECRUITMENT_POST_TYPES: readonly CommunityPostType[] = [
  'looking_for_players',
  'looking_for_team',
  'training_partner',
  'friendly_match',
];

export const isRecruitmentPost = (t: CommunityPostType): boolean =>
  RECRUITMENT_POST_TYPES.includes(t);

/** Trạng thái hiển thị trong feed công khai. */
export const PUBLICLY_VISIBLE_STATUSES: readonly CommunityPostStatus[] = [
  'published',
  'closed',
  'expired',
];

/** Root comment id — luôn dùng khi gửi reply (backend reject reply-của-reply). */
export const rootCommentId = (c: CommunityCommentResponse): string =>
  c.parentCommentId ?? c.id;

/** actionTaken hợp lệ theo targetType — backend KHÔNG kiểm, frontend phải kiểm. */
export const allowedActions = (t: ReportTargetType): ReportAction[] => {
  if (t === 'community_post') return ['none', 'post_hidden', 'post_deleted'];
  if (t === 'community_comment') return ['none', 'comment_hidden', 'comment_deleted'];
  return ['none'];
};
```

---

## Phụ lục — Bảng tra cứu nhanh toàn bộ endpoint

| # | Method | Endpoint | Auth | Role | Body | Response `data` |
|---:|---|---|:---:|---|---|---|
| 1 | POST | `/api/vouchers/validate` | ✅ | any | `ValidateVoucherRequest` | `VoucherQuoteResponse` |
| 2 | POST | `/api/bookings/purchase/payos` | ✅ | learner | `PurchaseTrainingPackageRequest` | `PurchaseTrainingPackagePayOsResponse` |
| 3 | POST | `/api/bookings/purchase/manual` | ✅ | learner | `PurchaseTrainingPackageRequest` | `BookingResponse` |
| 4 | POST | `/api/payments/payos/reconcile` | ✅ | learner | `ReconcilePayOsRequest` | `ReconcilePayOsResponse` |
| 5 | POST | `/api/payments/payos/{orderCode}/reconcile` | ✅ | learner | — | `ReconcilePayOsResponse` |
| 6 | POST | `/api/admin/voucher-campaigns` | ✅ | admin | `CreateVoucherCampaignRequest` | `VoucherCampaignResponse` |
| 7 | GET | `/api/admin/voucher-campaigns` | ✅ | admin | — | `PagedResult<VoucherCampaignResponse>` |
| 8 | GET | `/api/admin/voucher-campaigns/{id}` | ✅ | admin | — | `VoucherCampaignResponse` |
| 9 | PUT | `/api/admin/voucher-campaigns/{id}` | ✅ | admin | `UpdateVoucherCampaignRequest` | `VoucherCampaignResponse` |
| 10 | PUT | `/api/admin/voucher-campaigns/{id}/activate` | ✅ | admin | — | `VoucherCampaignResponse` |
| 11 | PUT | `/api/admin/voucher-campaigns/{id}/pause` | ✅ | admin | — | `VoucherCampaignResponse` |
| 12 | PUT | `/api/admin/voucher-campaigns/{id}/end` | ✅ | admin | — | `VoucherCampaignResponse` |
| 13 | GET | `/api/admin/voucher-campaigns/{id}/redemptions` | ✅ | admin | — | `PagedResult<VoucherRedemptionResponse>` |
| 14 | GET | `/api/community/posts` | ❌ | — | — | `PagedResult<CommunityPostResponse>` |
| 15 | GET | `/api/community/posts/me` | ✅ | any | — | `PagedResult<CommunityPostResponse>` |
| 16 | GET | `/api/community/posts/{id}` | ❌ | — | — | `CommunityPostResponse` |
| 17 | POST | `/api/community/posts` | ✅ | any | `CreateCommunityPostRequest` | `CommunityPostResponse` |
| 18 | PUT | `/api/community/posts/{id}` | ✅ | any | `UpdateCommunityPostRequest` | `CommunityPostResponse` |
| 19 | PUT | `/api/community/posts/{id}/close` | ✅ | any | — | `CommunityPostResponse` |
| 20 | DELETE | `/api/community/posts/{id}` | ✅ | any | — | `DeleteResult` |
| 21 | PUT | `/api/community/posts/{id}/like` | ✅ | any | — | `LikeResult` |
| 22 | DELETE | `/api/community/posts/{id}/like` | ✅ | any | — | `LikeResult` |
| 23 | POST | `/api/community/posts/{id}/applications` | ✅ | any | `CreateApplicationRequest` | `CommunityApplicationResponse` |
| 24 | DELETE | `/api/community/posts/{id}/applications/me` | ✅ | any | — | `CancelResult` |
| 25 | GET | `/api/community/posts/{id}/applications` | ✅ | owner | — | `PagedResult<CommunityApplicationResponse>` |
| 26 | PUT | `/api/community/applications/{id}/accept` | ✅ | owner | — | `CommunityApplicationResponse` |
| 27 | PUT | `/api/community/applications/{id}/reject` | ✅ | owner | — | `CommunityApplicationResponse` |
| 28 | GET | `/api/community/posts/{postId}/comments` | ❌ | — | — | `PagedResult<CommunityCommentResponse>` |
| 29 | POST | `/api/community/posts/{postId}/comments` | ✅ | any | `CreateCommentRequest` | `CommunityCommentResponse` |
| 30 | POST | `/api/community/comments/{commentId}/replies` | ✅ | any | `CreateReplyRequest` | `CommunityCommentResponse` |
| 31 | PUT | `/api/community/comments/{commentId}` | ✅ | any | `UpdateCommentRequest` | `CommunityCommentResponse` |
| 32 | DELETE | `/api/community/comments/{commentId}` | ✅ | any | — | `DeleteResult` |
| 33 | POST | `/api/reports` | ✅ | any | `CreateReportRequest` | `ReportResponse` |
| 34 | GET | `/api/admin/community/posts` | ✅ | admin | — | `PagedResult<AdminCommunityPostResponse>` |
| 35 | GET | `/api/admin/community/posts/{id}` | ✅ | admin | — | `CommunityPostResponse` |
| 36 | PUT | `/api/admin/community/posts/{id}/hide` | ✅ | admin | `HideContentRequest` | `CommunityPostResponse` |
| 37 | PUT | `/api/admin/community/posts/{id}/restore` | ✅ | admin | — | `CommunityPostResponse` |
| 38 | DELETE | `/api/admin/community/posts/{id}` | ✅ | admin | — | `DeleteResult` |
| 39 | GET | `/api/admin/community/posts/{id}/comments` | ✅ | admin | — | `PagedResult<CommunityCommentResponse>` |
| 40 | PUT | `/api/admin/community/comments/{id}/hide` | ✅ | admin | `HideContentRequest` | `CommunityCommentResponse` |
| 41 | PUT | `/api/admin/community/comments/{id}/restore` | ✅ | admin | — | `CommunityCommentResponse` |
| 42 | DELETE | `/api/admin/community/comments/{id}` | ✅ | admin | — | `DeleteResult` |
| 43 | GET | `/api/admin/community/reports` | ✅ | admin | — | `PagedResult<ReportResponse>` |
| 44 | PUT | `/api/admin/community/reports/{id}/resolve` | ✅ | admin | `ResolveReportRequest` | `ReportResponse` |
| 45 | POST | `/api/chat/rooms` | ✅ | any | `CreateChatRoomRequest` | `ChatRoomResponse` |
| 46 | GET | `/api/chat/rooms` | ✅ | any | — | `ChatRoomResponse[]` |
| 47 | PUT | `/api/chat/rooms/{roomId}/accept` | ✅ | receiver | — | `ChatRoomResponse` |
| 48 | PUT | `/api/chat/rooms/{roomId}/reject` | ✅ | receiver | — | `ChatRoomResponse` |
| 49 | GET | `/api/chat/rooms/{roomId}/messages` | ✅ | member | — | `PagedResult<ChatMessageResponse>` |
| 50 | POST | `/api/chat/rooms/{roomId}/messages` | ✅ | member | `SendMessageRequest` | `ChatMessageResponse` |
| 51 | PUT | `/api/users/{userId}/block` | ✅ | any | `BlockUserRequest` (optional) | `BlockResult` |
| 52 | DELETE | `/api/users/{userId}/block` | ✅ | any | — | `BlockResult` |
| 53 | GET | `/api/users/me/blocked` | ✅ | any | — | `BlockedUserResponse[]` |

**Tổng: 53 endpoint.** Tất cả trả HTTP `200` khi thành công, bọc trong `Result<T>`
(`{ isSuccess, data, error }`). Không có `201`, không có `204`.
