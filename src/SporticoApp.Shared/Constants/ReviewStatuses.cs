namespace SporticoApp.Shared.Constants
{
    /// <summary>
    /// Lifecycle status of a coach review. Reviews are never hard-deleted so admin
    /// moderation stays auditable.
    /// </summary>
    public static class ReviewStatuses
    {
        public const string Active = "active";
        public const string Hidden = "hidden";
        public const string Deleted = "deleted";

        public static readonly string[] All = { Active, Hidden, Deleted };
    }
}
