namespace SporticoApp.Shared.Constants
{
    /// <summary>What a <c>Report</c> points at. Kept backward-compatible with user reports.</summary>
    public static class ReportTargetTypes
    {
        public const string User = "user";
        public const string Review = "review";
        public const string CommunityPost = "community_post";
        public const string CommunityComment = "community_comment";
        public const string ChatMessage = "chat_message";

        public static readonly string[] All =
        {
            User, Review, CommunityPost, CommunityComment, ChatMessage
        };
    }

    public static class ReportStatuses
    {
        public const string Pending = "pending";
        public const string Reviewing = "reviewing";
        public const string Resolved = "resolved";
        public const string Rejected = "rejected";

        public static readonly string[] All = { Pending, Reviewing, Resolved, Rejected };
    }

    /// <summary>Moderation action recorded on a resolved report (audit trail).</summary>
    public static class ReportActions
    {
        public const string None = "none";
        public const string ReviewHidden = "review_hidden";
        public const string ReviewDeleted = "review_deleted";
        public const string PostHidden = "post_hidden";
        public const string PostDeleted = "post_deleted";
        public const string CommentHidden = "comment_hidden";
        public const string CommentDeleted = "comment_deleted";
    }
}
