using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// A short-lived, single-use code handed to the frontend after a successful browser-redirect
/// external login. The frontend trades it for real Sportico tokens via
/// <c>POST /api/auth/google/exchange</c>.
/// <para>
/// This exists so access/refresh tokens never travel in a redirect URL (where they would leak into
/// browser history, the Referer header, and server access logs). Only the SHA-256
/// <see cref="CodeHash"/> is persisted — the plaintext code is returned exactly once, at creation,
/// and is never recoverable from the database.
/// </para>
/// </summary>
public partial class AuthExchangeCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Lowercase hex SHA-256 of the plaintext code. The plaintext is never stored.</summary>
    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set the moment the code is consumed; a non-null value permanently blocks reuse.</summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
