namespace SporticoApp.Shared.Constants
{
    /// <summary>
    /// pending: request awaiting the OTHER participant's response (spam-reduction gate).
    /// active: both participants can exchange messages.
    /// rejected: the target declined the request — read-only, no new messages.
    /// </summary>
    public static class ChatRoomStatuses
    {
        public const string Pending = "pending";
        public const string Active = "active";
        public const string Rejected = "rejected";
    }

    /// <summary>Where a chat room/first-contact was opened from — for context only, never authorization.</summary>
    public static class ChatSourceTypes
    {
        public const string Booking = "booking";
        public const string CommunityPost = "community_post";
    }
}
