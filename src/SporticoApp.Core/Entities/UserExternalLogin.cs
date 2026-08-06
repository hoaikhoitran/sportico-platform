using System;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Links a Sportico <see cref="User"/> to an identity at an external provider (currently only
/// Google). Kept as its own table so provider-specific columns never leak into <see cref="User"/>.
/// <para>
/// The stable identifier is <see cref="ProviderSubject"/> (Google's <c>sub</c> claim), never the
/// email — a Google account can change its email address, but <c>sub</c> is immutable.
/// <see cref="ProviderEmail"/> is metadata/audit only.
/// </para>
/// </summary>
public partial class UserExternalLogin
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>See <c>SporticoApp.Shared.Constants.ExternalAuthProviders</c>, e.g. "google".</summary>
    public string Provider { get; set; } = null!;

    /// <summary>The provider's immutable subject identifier (Google's "sub" claim).</summary>
    public string ProviderSubject { get; set; } = null!;

    /// <summary>Email reported by the provider when the link was created. Audit metadata only.</summary>
    public string? ProviderEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual User User { get; set; } = null!;
}
