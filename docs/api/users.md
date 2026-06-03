# Public Users API

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/users/{id}` | None (AllowAnonymous) | Get basic public profile for any user by id |
| GET | `/api/users/me` | Bearer (any role) | Get the current authenticated user's full profile |
| PUT | `/api/users/me` | Bearer (any role) | Update the current authenticated user's profile |

---

## GET /api/users/{id}

Returns a minimal, safe public profile for any user. No login required.

**Path parameter:** `id` — Guid.

**Response** (`Result<PublicUserResponse>`):

```json
{
  "isSuccess": true,
  "data": {
    "id": "...",
    "fullName": "Coach Nguyen",
    "avatarUrl": "https://cdn.example.com/avatar.jpg",
    "roles": ["coach"],
    "coachProfile": {
      "headline": "Badminton expert",
      "bio": "10 years coaching",
      "experienceYears": 10,
      "coverImageUrl": "https://cdn.example.com/cover.jpg",
      "rating": 4.8,
      "totalReviews": 42
    },
    "learnerProfile": null
  }
}
```

**Fields intentionally excluded:** `email`, `phone`, `dateOfBirth`, `passwordHash`, `status`,
`createdAt`, `updatedAt`, any auth/token fields.

**Errors:**
- `404 USER_NOT_FOUND` — no user with this id.

---

## Security notes

- This endpoint is `[AllowAnonymous]` and must remain so.
- The existing admin endpoint `GET /api/admin/users/{id}` is unchanged and still requires `[Authorize(Roles = "admin")]` — it returns `AdminUserResponse` with full fields (email, phone, dateOfBirth, status, timestamps).
- `PublicUserResponse` is a separate DTO that does **not** expose any sensitive fields. The mapping (`ToPublicUserResponse`) touches only `Id`, `FullName`, `AvatarUrl`, `UserRoles[].Role.Name`, `CoachProfile` summary, and `LearnerProfile.Goal`.
