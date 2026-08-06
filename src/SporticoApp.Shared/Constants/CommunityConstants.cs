namespace SporticoApp.Shared.Constants
{
    public static class CommunityPostTypes
    {
        public const string LookingForPlayers = "looking_for_players";
        public const string LookingForTeam = "looking_for_team";
        public const string TrainingPartner = "training_partner";
        public const string FriendlyMatch = "friendly_match";
        public const string Event = "event";
        public const string Discussion = "discussion";
        public const string Question = "question";

        public static readonly string[] All =
        {
            LookingForPlayers, LookingForTeam, TrainingPartner, FriendlyMatch, Event, Discussion, Question
        };

        /// <summary>
        /// Post types that recruit participants: require SportId/StartAt/MaxParticipants and
        /// support the CommunityPostApplication (xin tham gia) workflow.
        /// </summary>
        public static readonly string[] RecruitmentTypes =
        {
            LookingForPlayers, LookingForTeam, TrainingPartner, FriendlyMatch
        };

        public static bool IsRecruitment(string postType) => RecruitmentTypes.Contains(postType);
    }

    public static class CommunityPostStatuses
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Closed = "closed";
        public const string Expired = "expired";
        public const string Hidden = "hidden";
        public const string Deleted = "deleted";

        /// <summary>Statuses that must never appear in the public feed / public detail.</summary>
        public static readonly string[] PubliclyVisible = { Published, Closed, Expired };
    }

    public static class CommunityMediaTypes
    {
        public const string Image = "image";
        public const string Video = "video";
    }

    public static class CommunityMediaStatuses
    {
        public const string Active = "active";
        public const string Removed = "removed";
    }

    public static class CommunityCommentStatuses
    {
        public const string Active = "active";
        public const string Hidden = "hidden";
        public const string Deleted = "deleted";
    }

    public static class CommunityApplicationStatuses
    {
        public const string Pending = "pending";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string Cancelled = "cancelled";
    }
}
