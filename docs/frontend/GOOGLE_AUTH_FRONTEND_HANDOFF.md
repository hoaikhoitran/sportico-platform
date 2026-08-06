# Bàn giao Google Authentication cho Frontend

> **Nguồn sự thật:** tài liệu này được viết sau khi triển khai xong, bằng cách đọc lại controller,
> DTO, validator, `ErrorCodes`, service thật, và đối chiếu với **Swagger JSON lấy từ API đang chạy**
> cùng các smoke test đã thực thi. Nếu kế hoạch ban đầu khác code, code thắng.
>
> Ngày đối chiếu: 2026-08-05 · Build: 0 error · Test: 479/480 pass (1 fail có sẵn từ trước, không
> liên quan Google).

---

## Mục lục

1. [Tổng quan hai flow](#1-tổng-quan-hai-flow)
2. [Environment variables của frontend](#2-environment-variables-của-frontend)
3. [Quy ước API chung](#3-quy-ước-api-chung)
4. [Exact TypeScript types](#4-exact-typescript-types)
5. [POST /api/auth/google](#5-post-apiauthgoogle)
6. [GET /api/auth/google](#6-get-apiauthgoogle)
7. [Google callback phía backend](#7-google-callback-phía-backend)
8. [POST /api/auth/google/exchange](#8-post-apiauthgoogleexchange)
9. [Refresh token sau Google login](#9-refresh-token-sau-google-login)
10. [GET /api/auth/me](#10-get-apiauthme)
11. [Flow frontend khuyến nghị](#11-flow-frontend-khuyến-nghị)
12. [Next.js App Router implementation](#12-nextjs-app-router-implementation)
13. [Error handling matrix](#13-error-handling-matrix)
14. [Loading / error / edge states](#14-loading--error--edge-states)
15. [Testing checklist](#15-testing-checklist)
16. [Known limitations](#16-known-limitations)

---

## 1. Tổng quan hai flow

Backend hỗ trợ **hai** cách đăng nhập Google. Cả hai dùng chung một logic tạo/liên kết tài khoản.

### Flow A — Google Identity Services (ID token) ⭐ **KHUYẾN NGHỊ**

```
Frontend load Google Identity Services
→ user bấm nút Google → Google trả CredentialResponse.credential (ID token)
→ POST /api/auth/google { idToken }
→ Backend xác minh chữ ký/issuer/audience/expiry với Google
→ Backend trả { accessToken, refreshToken, expiresAt }
```

### Flow B — Redirect OAuth (dự phòng)

```
window.location.assign(`${API}/api/auth/google`)
→ 302 tới Google consent
→ Google gọi về backend /api/auth/google/callback
→ backend redirect tới /api/auth/google/complete
→ backend redirect: {FRONTEND_URL}/auth/google/callback?code=<one-time-code>
→ POST /api/auth/google/exchange { code }
→ Backend trả { accessToken, refreshToken, expiresAt }
```

### Nên dùng flow nào?

| | Flow A (ID token) | Flow B (redirect) |
|---|---|---|
| Trải nghiệm | Popup/inline, **không rời trang** | Full-page redirect, mất state SPA |
| Số HTTP request | 1 | 2 (+2 redirect) |
| Cần `GOOGLE_CLIENT_SECRET` | ❌ Không | ✅ Có (backend) |
| Rủi ro lộ token qua URL | Không có URL nào | Có `code` trong URL (đã thiết kế an toàn) |
| Phù hợp | **SPA Next.js hiện tại** | Khi bị chặn third-party cookie, hoặc muốn full redirect |

> **Khuyến nghị: dùng Flow A làm mặc định**, giữ Flow B làm dự phòng. Cả hai đều đã hoạt động
> trên backend.

---

## 2. Environment variables của frontend

```env
NEXT_PUBLIC_API_BASE_URL=https://sportico.click
NEXT_PUBLIC_GOOGLE_CLIENT_ID=<google-oauth-client-id>.apps.googleusercontent.com
```

| Biến | Public? | Ghi chú |
|---|---|---|
| `NEXT_PUBLIC_GOOGLE_CLIENT_ID` | ✅ **Được phép** ở frontend | Google Client ID là giá trị công khai theo thiết kế của OAuth. Nó xuất hiện trong URL authorize mà ai cũng thấy được. |
| `GOOGLE_CLIENT_SECRET` | ❌ **TUYỆT ĐỐI KHÔNG** | Chỉ tồn tại trong environment của backend. Không đưa vào `NEXT_PUBLIC_*`, không đưa vào bundle, không đưa vào tài liệu frontend, không đưa vào file `.env` của frontend. |

> ⚠️ Nếu bạn thấy `GOOGLE_CLIENT_SECRET` ở bất kỳ đâu trong frontend repo — đó là sự cố bảo mật,
> phải rotate secret ngay.

Client ID dùng ở frontend **phải trùng** với `GOOGLE_CLIENT_ID` của backend, vì backend kiểm tra
`aud` của ID token phải bằng đúng client id đó.

---

## 3. Quy ước API chung

### Response envelope

Giống toàn bộ API Sportico — field là **`isSuccess`**, không phải `success`:

```json
{ "isSuccess": true, "data": { }, "error": null }
```

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_INVALID_TOKEN",
    "message": "Google authentication failed",
    "type": "Unauthorized",
    "details": null
  }
}
```

> ⚠️ **`error.details` là `string[] | null`** — nó **là `null`** trong hầu hết lỗi Google
> (đã kiểm chứng bằng response thật). Chỉ có giá trị khi:
> - lỗi validation → mảng message,
> - lỗi `AUTH_GOOGLE_CONFIGURATION_MISSING` → mảng **tên** biến môi trường còn thiếu.
>
> Đừng viết `error.details.map(...)` mà không kiểm tra null.

### Quy ước khác

| Mục | Giá trị |
|---|---|
| JSON naming | `camelCase` |
| DateTime | UTC ISO-8601, ví dụ `"2026-08-05T18:13:34.7010828Z"` |
| `accessToken` | JWT **của Sportico** (không phải Google token) |
| `refreshToken` | Chuỗi opaque của Sportico |
| Success status | `200` cho mọi endpoint thành công (không có 201/204) |
| Route casing | Google endpoints là `/api/auth/google` (chữ thường). Các endpoint auth cũ hiển thị trong Swagger là `/api/Auth/login` (chữ A hoa) do dùng `[Route("api/[controller]")]`. **Routing của ASP.NET Core không phân biệt hoa thường**, nên `/api/auth/login` vẫn chạy đúng. |

### ⚠️ Quy tắc token quan trọng nhất

> **Google ID token KHÔNG dùng để gọi API Sportico.** Nó chỉ dùng đúng một lần, trong body của
> `POST /api/auth/google`. Sau đó mọi request có `[Authorize]` phải dùng **`accessToken` của
> Sportico** trong header `Authorization: Bearer <sportico-access-token>`.

---

## 4. Exact TypeScript types

```typescript
// ─────────── Shared envelope ───────────

export type ErrorTypeName =
  | 'Validation'
  | 'NotFound'
  | 'Unauthorized'
  | 'Forbidden'
  | 'Conflict'
  | 'Failure'
  | 'ServiceUnavailable';   // mới: dùng cho 503 khi Google chưa được cấu hình

export interface ApiError {
  code: string;
  message: string;
  type: ErrorTypeName;
  /** null trong hầu hết lỗi Google. Chỉ có mảng khi validation hoặc thiếu cấu hình. */
  details: string[] | null;
}

export interface ApiResult<T> {
  isSuccess: boolean;
  data: T | null;
  error: ApiError | null;
}

// ─────────── Google auth ───────────

export interface GoogleIdTokenLoginRequest {
  /** CredentialResponse.credential từ Google Identity Services. Tối đa 8192 ký tự. */
  idToken: string;
}

export interface GoogleExchangeCodeRequest {
  /** Mã một lần lấy từ query string ?code=. Tối đa 256 ký tự. */
  code: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  /** UTC ISO-8601 — thời điểm accessToken hết hạn. */
  expiresAt: string;
}

export interface RefreshTokenRequest {
  email: string;
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

// ─────────── /api/auth/me ───────────

export interface CoachProfileSummary {
  id: string;
  bio: string | null;
  experienceYears: number | null;
  hourlyRate: number | null;
  status: string;
}

export interface LearnerProfileSummary {
  id: string;
  goal: string | null;
  level: string | null;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  avatarUrl: string | null;
  /** Chỉ ngày, dạng "2001-05-20" hoặc null. */
  dateOfBirth: string | null;
  /** active | inactive | banned | pending */
  status: string;
  /** Ví dụ ["learner"]. Tài khoản Google mới luôn chỉ có ["learner"]. */
  roles: string[];
  coachProfile: CoachProfileSummary | null;
  learnerProfile: LearnerProfileSummary | null;
}

// ─────────── Error codes ───────────

export type GoogleAuthErrorCode =
  | 'AUTH_GOOGLE_INVALID_TOKEN'
  | 'AUTH_GOOGLE_EMAIL_NOT_VERIFIED'
  | 'AUTH_GOOGLE_ACCOUNT_CONFLICT'
  | 'AUTH_GOOGLE_LOGIN_FAILED'
  | 'AUTH_GOOGLE_CONFIGURATION_MISSING'
  | 'AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID'
  | 'AUTH_GOOGLE_EXCHANGE_CODE_INVALID'
  | 'AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED'
  | 'AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED'
  | 'AUTH_PASSWORD_NOT_SET';

/** Các code dùng chung có thể gặp trong luồng Google. */
export type SharedAuthErrorCode =
  | 'COMMON_VALIDATION_ERROR'
  | 'COMMON_ACCOUNT_NOT_ACTIVE'
  | 'COMMON_INTERNAL_SERVER_ERROR'
  | 'AUTH_INVALID_CREDENTIALS';
```

> `coachProfile` / `learnerProfile`: với tài khoản Google mới, **cả hai đều `null`** — backend
> không tự tạo `LearnerProfile` khi đăng ký qua Google (giống hệt luồng register thường).

---

## 5. POST /api/auth/google

| Mục | Giá trị |
|---|---|
| Method / URL | `POST {API_BASE_URL}/api/auth/google` |
| Auth | Không cần (`[AllowAnonymous]`) |
| Content-Type | `application/json` |
| Success | `200` |

### Request

```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjkxNGZiOWIwOD..."
}
```

### Success response

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "kR8vQ2xN7pL4mZ1aB6cD9eF3gH5jK0nT",
    "expiresAt": "2026-08-05T18:13:34.7010828Z"
  },
  "error": null
}
```

### Error response (đã kiểm chứng bằng curl thật)

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_INVALID_TOKEN",
    "message": "Google authentication failed",
    "type": "Unauthorized",
    "details": null
  }
}
```
HTTP `401`.

### Validation lỗi (`idToken` rỗng)

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "COMMON_VALIDATION_ERROR",
    "message": "Invalid request data",
    "type": "Validation",
    "details": ["idToken is required"]
  }
}
```
HTTP `400`.

### Quy tắc bắt buộc

- `idToken` phải là **ID token** (JWT) từ Google Identity Services — chính là
  `CredentialResponse.credential`. **Không** gửi OAuth access token.
- **Không** gửi kèm `email`, `fullName`, `avatarUrl`, `googleId`. Backend bỏ qua mọi thứ ngoài
  `idToken` và tự đọc identity từ token đã xác minh.
- Backend tự tạo hoặc liên kết tài khoản:
  - Chưa có tài khoản → tạo mới, `status = "active"`, role `learner`, không có mật khẩu.
  - Email đã tồn tại → **liên kết**, không tạo tài khoản thứ hai.
  - Tài khoản `inactive` (chưa verify email) → được **kích hoạt** vì Google đã xác minh email.
  - Tài khoản `banned` → **từ chối đăng nhập** (`403`).
- Google email chưa verified → `401 AUTH_GOOGLE_EMAIL_NOT_VERIFIED`.
- Tên và ảnh đại diện người dùng đã tự chỉnh **không bị Google ghi đè**. Chỉ điền vào khi đang trống.

---

## 6. GET /api/auth/google

| Mục | Giá trị |
|---|---|
| Method / URL | `GET {API_BASE_URL}/api/auth/google` |
| Auth | Không cần |
| Response | `302 Found` → `https://accounts.google.com/...` |

### Response thật (đã verify)

```http
HTTP/1.1 302 Found
Location: https://accounts.google.com/o/oauth2/v2/auth?client_id=<client-id>&scope=openid%20email%20profile&response_type=code&redirect_uri=https%3A%2F%2Fsportico.click%2Fapi%2Fauth%2Fgoogle%2Fcallback&code_challenge=<pkce>&code_challenge_method=S256&state=<correlation>
```

Đặc điểm đã kiểm chứng: scope đúng `openid email profile`, có PKCE, có `state`, và
**không bao giờ chứa `client_secret`**.

### ⚠️ Đây là browser navigation, KHÔNG phải fetch

```ts
// ĐÚNG
window.location.assign(`${process.env.NEXT_PUBLIC_API_BASE_URL}/api/auth/google`);

// SAI — sẽ nhận CORS error hoặc HTML của Google, không phải JSON
const res = await fetch(`${API}/api/auth/google`);
```

- **Không** thêm header `Authorization`.
- **Không** truyền `redirectUrl` tùy ý — backend không nhận và luôn quay về
  `{FRONTEND_URL}/auth/google/callback`.
- Backend tự quản lý `state`/correlation cookie; frontend không tự dựng URL authorize của Google.

### Khi backend chưa cấu hình Google

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_CONFIGURATION_MISSING",
    "message": "Google sign-in is not configured on this environment.",
    "type": "ServiceUnavailable",
    "details": ["GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_CALLBACK_URL", "FRONTEND_URL"]
  }
}
```
HTTP `503`. `details` chỉ chứa **tên** biến, không bao giờ chứa giá trị.

---

## 7. Google callback phía backend

```
GET /api/auth/google/callback
```

- Đây là URL **Google gọi về backend**. **Frontend không bao giờ gọi trực tiếp.**
- Path lấy từ `GOOGLE_CALLBACK_URL` của backend và phải khớp redirect URI đăng ký trong Google Cloud.

Sau đó backend chuyển tiếp nội bộ tới `/api/auth/google/complete`, rồi redirect về frontend:

**Thành công:**
```
{FRONTEND_URL}/auth/google/callback?code=<one-time-code>
```

**Thất bại:**
```
{FRONTEND_URL}/auth/google/callback?error=<STABLE_ERROR_CODE>
```

Ví dụ thật (đã verify khi thiếu external cookie):
```
https://sportico-fe.vercel.app/auth/google/callback?error=AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID
```

### Quy tắc về `code`

| Điều | Chi tiết |
|---|---|
| `code` **không phải** accessToken | Nó chỉ là vé đổi token |
| `code` **không phải** refreshToken | |
| Dùng **đúng một lần** | Lần thứ hai → `409 AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED` |
| Hết hạn nhanh | Mặc định **90 giây** (backend clamp 30–300s) |
| Không lưu lâu dài | Không đưa vào localStorage/sessionStorage |
| Không log | Không gửi vào analytics, Sentry breadcrumb, hay URL tracking |
| Xóa khỏi URL sau khi đổi | Dùng `router.replace('/auth/google/callback')` |
| Độ dài | 43 ký tự base64url (`A-Z a-z 0-9 - _`) |

> Backend chỉ lưu **SHA-256 hash** của code. Plaintext không thể khôi phục từ database, và bảng
> `auth_exchange_codes` **không chứa** access/refresh token nào.

---

## 8. POST /api/auth/google/exchange

| Mục | Giá trị |
|---|---|
| Method / URL | `POST {API_BASE_URL}/api/auth/google/exchange` |
| Auth | Không cần |
| Success | `200` |

### Request

```json
{ "code": "kR8vQ2xN7pL4mZ1aB6cD9eF3gH5jK0nTqW2eR4tY6uI" }
```

### Success response

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "pQ7wE2rT5yU8iO1pA4sD6fG9hJ0kL3zX",
    "expiresAt": "2026-08-05T18:13:34.7010828Z"
  },
  "error": null
}
```

### Error — code không tồn tại (đã kiểm chứng bằng curl thật)

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_EXCHANGE_CODE_INVALID",
    "message": "Invalid Google authentication exchange code",
    "type": "Unauthorized",
    "details": null
  }
}
```
HTTP `401`.

### Error — code hết hạn

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED",
    "message": "Google authentication exchange code has expired",
    "type": "Unauthorized",
    "details": null
  }
}
```
HTTP `401`.

### Error — code đã dùng

```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED",
    "message": "Google authentication exchange code was already used",
    "type": "Conflict",
    "details": null
  }
}
```
HTTP `409`.

> ⚠️ **React Strict Mode gọi effect hai lần trong dev** → lần thứ hai sẽ nhận `409`.
> Bắt buộc dùng guard `useRef` (xem code mẫu mục 12), nếu không user sẽ thấy lỗi giả.

---

## 9. Refresh token sau Google login

Google login dùng **đúng hệ thống refresh token sẵn có**, không có cơ chế session song song.

```http
POST /api/auth/refresh-token
Content-Type: application/json
```

```json
{ "email": "user@gmail.com", "refreshToken": "kR8vQ2xN7pL4mZ1aB6cD9eF3gH5jK0nT" }
```

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "<access token mới>",
    "refreshToken": "<refresh token MỚI>",
    "expiresAt": "2026-08-05T19:13:34.0000000Z"
  },
  "error": null
}
```

| Quy tắc | Chi tiết |
|---|---|
| Cần `email` | Endpoint refresh yêu cầu cả email lẫn refreshToken → **phải lưu email** sau khi login |
| Refresh token được **rotate** | Phải ghi đè **cả** accessToken **và** refreshToken sau mỗi lần refresh |
| Mỗi user một refresh token | Login ở thiết bị khác sẽ vô hiệu hoá refresh token cũ |
| **Không** dùng Google ID token để refresh | Google ID token chỉ dùng một lần lúc đăng nhập |
| Không cần đăng nhập lại Google | Khi accessToken hết hạn, chỉ cần gọi refresh-token |

**Lỗi:** `401 AUTH_INVALID_REFRESH_TOKEN`, `401 AUTH_REFRESH_TOKEN_EXPIRED`,
`401 COMMON_ACCOUNT_NOT_ACTIVE` → xoá token local và đưa user về trang đăng nhập.

---

## 10. GET /api/auth/me

```http
GET /api/auth/me
Authorization: Bearer <sportico-access-token>
```

Response thật của một tài khoản Google mới (đã verify qua smoke test):

```json
{
  "isSuccess": true,
  "data": {
    "id": "aa35a070-caec-4e58-8061-f1a12ae8a790",
    "email": "user@gmail.com",
    "fullName": "Nguyễn Văn A",
    "phone": null,
    "avatarUrl": "https://lh3.googleusercontent.com/a/ACg8ocK...",
    "dateOfBirth": null,
    "status": "active",
    "roles": ["learner"],
    "coachProfile": null,
    "learnerProfile": null
  },
  "error": null
}
```

- `avatarUrl` là `null` nếu Google không trả ảnh, hoặc URL không phải HTTPS tuyệt đối (backend loại bỏ).
- `fullName` fallback về phần trước `@` của email nếu Google không trả tên.

---

## 11. Flow frontend khuyến nghị

### Flow A — Google Identity Services

```
1. Load script https://accounts.google.com/gsi/client (afterInteractive).
2. google.accounts.id.initialize({ client_id, callback }).
3. Render nút bằng google.accounts.id.renderButton (hoặc dùng One Tap).
4. Trong callback: lấy response.credential → đó là idToken.
5. POST /api/auth/google { idToken }.
6. Lưu accessToken + refreshToken + email + expiresAt.
7. GET /api/auth/me bằng accessToken.
8. Cập nhật auth store.
9. Điều hướng về trang trước khi login (hoặc trang chủ).
```

### Flow B — Redirect

```
1. User bấm "Tiếp tục với Google".
2. window.location.assign(`${API}/api/auth/google`).
3. Google consent → backend callback → backend complete.
4. Browser landing tại /auth/google/callback?code=... (hoặc ?error=...).
5. Nếu có ?error → hiển thị lỗi, KHÔNG gọi exchange.
6. Nếu có ?code → POST /api/auth/google/exchange { code }  (guard chống gọi 2 lần).
7. Lưu token + email.
8. router.replace('/auth/google/callback') để xoá code khỏi URL.
9. GET /api/auth/me → cập nhật store → điều hướng.
```

---

## 12. Next.js App Router implementation

### Cấu trúc đề xuất

```
src/
  features/
    auth/
      api/auth-api.ts
      components/google-login-button.tsx
      hooks/use-google-login.ts
      storage/auth-storage.ts
      types/auth.types.ts
  app/
    auth/
      google/
        callback/
          page.tsx
```

### `features/auth/storage/auth-storage.ts`

```typescript
import type { LoginResponse } from '../types/auth.types';

const ACCESS_TOKEN_KEY = 'sportico.accessToken';
const REFRESH_TOKEN_KEY = 'sportico.refreshToken';
const EMAIL_KEY = 'sportico.email';
const EXPIRES_AT_KEY = 'sportico.expiresAt';

/**
 * Lưu ở localStorage để hợp với API hiện tại (Bearer token, không phải cookie session).
 * Nếu dự án siết bảo mật hơn, hãy chuyển sang httpOnly cookie do Next.js route handler đặt —
 * backend không phụ thuộc vào nơi frontend lưu token.
 */
export const authStorage = {
  save(tokens: LoginResponse, email: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
    localStorage.setItem(EMAIL_KEY, email);
  },

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  },

  /** Refresh cần CẢ email lẫn refreshToken. */
  getRefreshCredentials(): { email: string; refreshToken: string } | null {
    const email = localStorage.getItem(EMAIL_KEY);
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    return email && refreshToken ? { email, refreshToken } : null;
  },

  clear(): void {
    [ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY, EMAIL_KEY, EXPIRES_AT_KEY]
      .forEach((k) => localStorage.removeItem(k));
  },
};
```

### `features/auth/api/auth-api.ts`

```typescript
import type {
  ApiResult,
  CurrentUserResponse,
  GoogleExchangeCodeRequest,
  GoogleIdTokenLoginRequest,
  LoginResponse,
  RefreshTokenResponse,
} from '../types/auth.types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL!;

export class ApiException extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly status: number,
    public readonly details: string[] | null = null,
  ) {
    super(message);
    this.name = 'ApiException';
  }
}

async function unwrap<T>(response: Response): Promise<T> {
  const result = (await response.json()) as ApiResult<T>;

  if (!response.ok || !result.isSuccess || result.data === null) {
    throw new ApiException(
      result.error?.code ?? 'COMMON_INTERNAL_SERVER_ERROR',
      result.error?.message ?? 'Đã có lỗi xảy ra',
      response.status,
      result.error?.details ?? null,
    );
  }

  return result.data;
}

export async function loginWithGoogleIdToken(idToken: string): Promise<LoginResponse> {
  const body: GoogleIdTokenLoginRequest = { idToken };

  const response = await fetch(`${API_BASE_URL}/api/auth/google`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  return unwrap<LoginResponse>(response);
}

export async function exchangeGoogleCode(code: string): Promise<LoginResponse> {
  const body: GoogleExchangeCodeRequest = { code };

  const response = await fetch(`${API_BASE_URL}/api/auth/google/exchange`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  return unwrap<LoginResponse>(response);
}

export async function refreshTokens(
  email: string,
  refreshToken: string,
): Promise<RefreshTokenResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/refresh-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, refreshToken }),
  });

  return unwrap<RefreshTokenResponse>(response);
}

export async function getCurrentUser(accessToken: string): Promise<CurrentUserResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  return unwrap<CurrentUserResponse>(response);
}

/** Flow B: điều hướng cả trang. KHÔNG dùng fetch cho endpoint này. */
export function startGoogleRedirectLogin(): void {
  window.location.assign(`${API_BASE_URL}/api/auth/google`);
}
```

### `features/auth/components/google-login-button.tsx`

```tsx
'use client';

import Script from 'next/script';
import { useCallback, useEffect, useRef, useState } from 'react';
import { getCurrentUser, loginWithGoogleIdToken, ApiException } from '../api/auth-api';
import { authStorage } from '../storage/auth-storage';

interface CredentialResponse {
  credential?: string;
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize(config: {
            client_id: string;
            callback: (response: CredentialResponse) => void;
            auto_select?: boolean;
            cancel_on_tap_outside?: boolean;
          }): void;
          renderButton(parent: HTMLElement, options: Record<string, unknown>): void;
        };
      };
    };
  }
}

interface Props {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function GoogleLoginButton({ onSuccess, onError }: Props) {
  const buttonRef = useRef<HTMLDivElement>(null);
  const [scriptLoaded, setScriptLoaded] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  // Google có thể gọi callback nhiều lần (double click, One Tap) — chặn request chồng.
  const inFlight = useRef(false);

  const handleCredential = useCallback(
    async (response: CredentialResponse) => {
      if (inFlight.current) return;
      if (!response.credential) {
        onError('Không nhận được thông tin từ Google. Vui lòng thử lại.');
        return;
      }

      inFlight.current = true;
      setIsSubmitting(true);

      try {
        const tokens = await loginWithGoogleIdToken(response.credential);
        const me = await getCurrentUser(tokens.accessToken);
        authStorage.save(tokens, me.email);
        onSuccess();
      } catch (error) {
        onError(toVietnameseMessage(error));
      } finally {
        inFlight.current = false;
        setIsSubmitting(false);
      }
    },
    [onSuccess, onError],
  );

  useEffect(() => {
    if (!scriptLoaded || !window.google || !buttonRef.current) return;

    window.google.accounts.id.initialize({
      client_id: process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID!,
      callback: handleCredential,
      cancel_on_tap_outside: true,
    });

    window.google.accounts.id.renderButton(buttonRef.current, {
      theme: 'outline',
      size: 'large',
      width: 320,
      text: 'continue_with',
      locale: 'vi',
    });
  }, [scriptLoaded, handleCredential]);

  return (
    <>
      <Script
        src="https://accounts.google.com/gsi/client"
        strategy="afterInteractive"
        onLoad={() => setScriptLoaded(true)}
        onError={() => onError('Không tải được Google Sign-In. Kiểm tra kết nối mạng.')}
      />

      {!scriptLoaded && <div className="h-10 w-80 animate-pulse rounded bg-gray-200" />}

      <div ref={buttonRef} aria-busy={isSubmitting} />

      {isSubmitting && <p className="mt-2 text-sm text-gray-600">Đang đăng nhập…</p>}
    </>
  );
}

export function toVietnameseMessage(error: unknown): string {
  if (!(error instanceof ApiException)) {
    return 'Không thể kết nối máy chủ. Vui lòng thử lại.';
  }

  const messages: Record<string, string> = {
    AUTH_GOOGLE_INVALID_TOKEN: 'Đăng nhập Google thất bại. Vui lòng thử lại.',
    AUTH_GOOGLE_EMAIL_NOT_VERIFIED: 'Email Google của bạn chưa được xác minh.',
    AUTH_GOOGLE_ACCOUNT_CONFLICT:
      'Tài khoản Sportico này đã liên kết với một tài khoản Google khác.',
    AUTH_GOOGLE_LOGIN_FAILED: 'Đăng nhập Google không thành công. Vui lòng thử lại.',
    AUTH_GOOGLE_CONFIGURATION_MISSING:
      'Đăng nhập Google hiện chưa khả dụng. Vui lòng dùng email và mật khẩu.',
    AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID:
      'Phiên đăng nhập Google đã hết hiệu lực. Vui lòng thử lại.',
    AUTH_GOOGLE_EXCHANGE_CODE_INVALID: 'Liên kết đăng nhập không hợp lệ. Vui lòng đăng nhập lại.',
    AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED: 'Liên kết đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
    AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED: 'Liên kết này đã được sử dụng. Vui lòng đăng nhập lại.',
    COMMON_ACCOUNT_NOT_ACTIVE: 'Tài khoản của bạn đang bị khoá hoặc chưa được kích hoạt.',
    COMMON_VALIDATION_ERROR: 'Dữ liệu gửi lên không hợp lệ.',
  };

  return messages[error.code] ?? error.message ?? 'Đã có lỗi xảy ra.';
}
```

### `app/auth/google/callback/page.tsx`

```tsx
'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';
import { exchangeGoogleCode, getCurrentUser } from '@/features/auth/api/auth-api';
import { toVietnameseMessage } from '@/features/auth/components/google-login-button';
import { authStorage } from '@/features/auth/storage/auth-storage';

export default function GoogleCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // React Strict Mode chạy effect hai lần trong dev. Mã đổi token chỉ dùng được MỘT lần,
  // nên lần thứ hai sẽ nhận 409 ALREADY_USED. Guard này là bắt buộc.
  const consumed = useRef(false);

  useEffect(() => {
    if (consumed.current) return;
    consumed.current = true;

    const error = searchParams.get('error');
    const code = searchParams.get('code');

    if (error) {
      setErrorMessage(
        toVietnameseMessage(
          Object.assign(new Error(error), { code, name: 'ApiException' }) as never,
        ) || 'Đăng nhập Google không thành công.',
      );
      return;
    }

    if (!code) {
      setErrorMessage('Thiếu mã xác thực. Vui lòng đăng nhập lại.');
      return;
    }

    (async () => {
      try {
        const tokens = await exchangeGoogleCode(code);
        const me = await getCurrentUser(tokens.accessToken);
        authStorage.save(tokens, me.email);

        // Xoá ?code khỏi URL trước khi rời trang: không để mã nằm lại trong history.
        router.replace('/');
      } catch (err) {
        setErrorMessage(toVietnameseMessage(err));
        router.replace('/auth/google/callback');
      }
    })();
  }, [router, searchParams]);

  if (errorMessage) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-6">
        <p className="text-center text-red-600">{errorMessage}</p>
        <button
          className="rounded bg-blue-600 px-4 py-2 text-white"
          onClick={() => router.push('/login')}
        >
          Quay lại đăng nhập
        </button>
      </main>
    );
  }

  return (
    <main className="flex min-h-screen items-center justify-center">
      <p>Đang hoàn tất đăng nhập…</p>
    </main>
  );
}
```

---

## 13. Error handling matrix

| Error code | HTTP | Khi nào | Thông báo tiếng Việt | Frontend làm gì |
|---|---:|---|---|---|
| `AUTH_GOOGLE_INVALID_TOKEN` | 401 | ID token sai chữ ký / sai audience / hết hạn / hỏng | "Đăng nhập Google thất bại. Vui lòng thử lại." | Cho thử lại; nếu lặp lại, gợi ý đăng nhập bằng mật khẩu |
| `AUTH_GOOGLE_EMAIL_NOT_VERIFIED` | 401 | Google báo `email_verified = false` | "Email Google của bạn chưa được xác minh." | Hướng dẫn xác minh email tại Google, không cho retry ngay |
| `AUTH_GOOGLE_ACCOUNT_CONFLICT` | 409 | Tài khoản Sportico đã liên kết Google khác | "Tài khoản này đã liên kết với một tài khoản Google khác." | Gợi ý đăng nhập bằng đúng tài khoản Google đã liên kết |
| `COMMON_ACCOUNT_NOT_ACTIVE` | 403 | Tài khoản bị **banned** | "Tài khoản của bạn đã bị khoá." | Hiện thông tin liên hệ hỗ trợ, **không** cho retry |
| `COMMON_ACCOUNT_NOT_ACTIVE` | 401 | Tài khoản `pending` (đang chờ duyệt) | "Tài khoản chưa được kích hoạt." | Hiện hướng dẫn chờ duyệt |
| `AUTH_GOOGLE_CONFIGURATION_MISSING` | 503 | Backend chưa cấu hình Google | "Đăng nhập Google hiện chưa khả dụng." | **Ẩn nút Google**, chỉ hiện form email/mật khẩu |
| `AUTH_GOOGLE_EXTERNAL_PRINCIPAL_INVALID` | — (redirect) | Cookie tạm hết hạn / thiếu ở bước complete | "Phiên đăng nhập Google đã hết hiệu lực." | Về trang login, cho bấm lại |
| `AUTH_GOOGLE_LOGIN_FAILED` | — (redirect) | User từ chối consent, hoặc Google trả lỗi | "Đăng nhập Google không thành công." | Về trang login |
| `AUTH_GOOGLE_EXCHANGE_CODE_INVALID` | 401 | Code không tồn tại | "Liên kết đăng nhập không hợp lệ." | Về trang login |
| `AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED` | 401 | Quá ~90 giây | "Liên kết đăng nhập đã hết hạn." | Về trang login, cho bấm lại |
| `AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED` | 409 | Gọi exchange lần thứ hai | "Liên kết này đã được sử dụng." | **Kiểm tra guard `useRef`** — thường là bug Strict Mode |
| `AUTH_PASSWORD_NOT_SET` | 409 | Tài khoản Google-only gọi `change-password` | "Tài khoản chưa đặt mật khẩu." | Chuyển hướng sang luồng **quên mật khẩu** để đặt mật khẩu lần đầu |
| `AUTH_INVALID_CREDENTIALS` | 401 | Tài khoản Google-only đăng nhập bằng mật khẩu | "Email hoặc mật khẩu không đúng." | Gợi ý: "Bạn có thể đã đăng ký bằng Google — hãy thử nút Đăng nhập với Google" |
| `COMMON_VALIDATION_ERROR` | 400 | `idToken`/`code` rỗng hoặc quá dài | Hiện `details[0]` | Kiểm tra lại code frontend |

### ⚠️ Tài khoản Google-only và mật khẩu

Tài khoản tạo qua Google có `password_hash = NULL`:

| Hành động | Kết quả |
|---|---|
| Đăng nhập bằng email + mật khẩu | `401 AUTH_INVALID_CREDENTIALS` (**không bao giờ 500** — đã kiểm chứng bằng smoke test) |
| `POST /api/auth/change-password` | `409 AUTH_PASSWORD_NOT_SET` |
| Đặt mật khẩu lần đầu | Dùng **forgot-password → reset-password** |

> **Frontend recommendation:** ở form đăng nhập, khi nhận `AUTH_INVALID_CREDENTIALS`, thêm gợi ý
> "Nếu bạn đã đăng ký bằng Google, hãy dùng nút Đăng nhập với Google." Backend cố tình **không**
> tiết lộ tài khoản đó dùng Google (tránh lộ thông tin tài khoản tồn tại hay không).

---

## 14. Loading / error / edge states

### Nút Google (Flow A)

| Trạng thái | Hiển thị |
|---|---|
| Đang tải script GSI | Skeleton `h-10 w-80`, chưa render nút |
| Script lỗi | "Không tải được Google Sign-In. Kiểm tra kết nối mạng." + nút thử lại |
| Đang gọi `/api/auth/google` | Disable vùng nút, hiện "Đang đăng nhập…" |
| Backend trả 503 | **Ẩn hẳn nút Google**, chỉ còn form email/mật khẩu |
| Lỗi khác | Toast/inline theo bảng mục 13, giữ nút để thử lại |

### Popup bị đóng / user huỷ

Google Identity Services **không gọi callback** khi user đóng popup hoặc bấm ra ngoài — nên
`isSubmitting` sẽ không bao giờ được set. Nghĩa là:

- **Không** đặt `setIsSubmitting(true)` *trước* khi mở popup — chỉ set trong callback.
- Nếu dùng One Tap, cân nhắc `cancel_on_tap_outside: true` và không hiện spinner toàn trang.
- Không cần timeout: không có request nào đang chạy khi user huỷ.

### Trang callback (Flow B)

| Trạng thái | Hiển thị |
|---|---|
| Đang đổi code | "Đang hoàn tất đăng nhập…" (full-page) |
| `?error=...` | Thông báo theo bảng mục 13 + nút "Quay lại đăng nhập". **Không** gọi exchange. |
| Không có `code` lẫn `error` | "Thiếu mã xác thực. Vui lòng đăng nhập lại." |
| Exchange lỗi | Thông báo + nút quay lại; đồng thời `router.replace` để xoá `code` khỏi URL |
| Thành công | `router.replace('/')` — không để `?code=` lại trong history |

### Token hết hạn khi đang dùng app

```
accessToken hết hạn (401 từ API bất kỳ)
→ gọi POST /api/auth/refresh-token { email, refreshToken }
→ thành công: lưu CẢ accessToken và refreshToken mới, retry request cũ
→ thất bại: authStorage.clear() + chuyển về /login
```

Không cần đăng nhập lại bằng Google trong trường hợp này.

---

## 15. Testing checklist

### Flow A

- [ ] Đăng nhập Google lần đầu bằng email chưa có trên Sportico → tạo tài khoản, `roles: ["learner"]`
- [ ] Đăng nhập lại bằng đúng tài khoản Google đó → **không** tạo tài khoản thứ hai
- [ ] Đăng nhập Google bằng email trùng một tài khoản đã đăng ký mật khẩu → liên kết, không nhân bản
- [ ] Tài khoản `inactive` (chưa verify email) đăng nhập Google → được kích hoạt, vào được app
- [ ] Tài khoản bị `banned` → hiện thông báo khoá, không vào được
- [ ] Gửi `idToken` rác → `401`, hiện thông báo tiếng Việt
- [ ] Gửi `idToken` rỗng → `400`, hiện `details[0]`
- [ ] Bấm nút Google hai lần thật nhanh → chỉ một request được gửi (guard `inFlight`)
- [ ] Đóng popup Google giữa chừng → UI không kẹt spinner
- [ ] `avatarUrl` và `fullName` hiển thị đúng sau khi vào app

### Flow B

- [ ] `window.location.assign('/api/auth/google')` → tới trang consent của Google
- [ ] Sau consent → về `/auth/google/callback?code=...`
- [ ] Đổi code thành công → vào app, URL **không còn** `?code=`
- [ ] Refresh trang callback sau khi đã đổi → `409 ALREADY_USED`, hiện thông báo, không crash
- [ ] Chờ > 90 giây rồi mới đổi code → `401 EXPIRED`
- [ ] Từ chối consent trên Google → về callback với `?error=`, hiện thông báo
- [ ] Dev mode (Strict Mode) → **không** xuất hiện lỗi `ALREADY_USED` giả

### Chung

- [ ] `GET /api/auth/me` trả đúng email/name/avatar/roles
- [ ] Sau khi accessToken hết hạn → refresh-token hoạt động, không phải login lại Google
- [ ] Sau refresh → **cả** accessToken **và** refreshToken đều được ghi đè
- [ ] Tài khoản Google-only đăng nhập bằng mật khẩu → `401`, **không phải** `500`
- [ ] Tài khoản Google-only gọi đổi mật khẩu → `409 AUTH_PASSWORD_NOT_SET`
- [ ] Backend chưa cấu hình Google → nút Google bị ẩn, phần còn lại của app vẫn chạy
- [ ] Không có `GOOGLE_CLIENT_SECRET` ở bất kỳ đâu trong frontend bundle
- [ ] Không có accessToken/refreshToken nào xuất hiện trong URL hoặc browser history

---

## 16. Known limitations

| # | Hạn chế | Ảnh hưởng tới frontend |
|---|---|---|
| 1 | **Chưa chạy được luồng consent Google thật đầu-cuối** trong môi trường agent (cần người thật bấm đồng ý trên trang Google). Đã verify: challenge redirect, `redirect_uri`, scope, PKCE, xử lý token sai, exchange code sai/hết hạn/dùng lại, 503 khi thiếu cấu hình, và toàn bộ logic tạo/liên kết tài khoản bằng unit test. | **Người dùng cần tự bấm thử một lần** với tài khoản Google thật để xác nhận end-to-end. |
| 2 | `POST /api/auth/refresh-token` yêu cầu **cả `email`** lẫn `refreshToken` | Bắt buộc lưu email sau khi đăng nhập, nếu không sẽ không refresh được. |
| 3 | Mỗi user chỉ có **một** refresh token | Đăng nhập ở thiết bị thứ hai sẽ đăng xuất thiết bị thứ nhất khi nó refresh. |
| 4 | Backend **không** tự tạo `LearnerProfile` cho tài khoản Google mới | `learnerProfile` là `null` sau khi đăng ký — nếu UI cần, phải gọi API tạo profile riêng. |
| 5 | Tài khoản Google-only **không** đặt được mật khẩu qua `change-password` | Phải đi đường forgot-password → reset-password. |
| 6 | Endpoint auth cũ hiển thị trong Swagger là `/api/Auth/...` (chữ A hoa) | Chỉ là vấn đề hiển thị; routing không phân biệt hoa thường. |
| 7 | Trạng thái `pending` không được Google login tự kích hoạt | Đúng thiết kế (`pending` là trạng thái kiểm duyệt, không phải trạng thái email). |
| 8 | Không có endpoint "hủy liên kết Google" | Chưa nằm trong phạm vi tính năng này. |

---

## Phụ lục — Bảng tra cứu nhanh

| # | Method | Endpoint | Auth | Body | Response `data` |
|---:|---|---|:---:|---|---|
| 1 | POST | `/api/auth/google` | ❌ | `GoogleIdTokenLoginRequest` | `LoginResponse` |
| 2 | GET | `/api/auth/google` | ❌ | — | `302` tới Google |
| 3 | GET | `/api/auth/google/callback` | ❌ | — | `302` (Google gọi, frontend không dùng) |
| 4 | GET | `/api/auth/google/complete` | ❌ | — | `302` tới `{FRONTEND_URL}/auth/google/callback` |
| 5 | POST | `/api/auth/google/exchange` | ❌ | `GoogleExchangeCodeRequest` | `LoginResponse` |
| 6 | POST | `/api/auth/refresh-token` | ❌ | `RefreshTokenRequest` | `RefreshTokenResponse` |
| 7 | GET | `/api/auth/me` | ✅ Bearer | — | `CurrentUserResponse` |

Route frontend bắt buộc phải có: **`/auth/google/callback`**.
