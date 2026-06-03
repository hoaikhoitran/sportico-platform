namespace SporticoApp.Application.DTOs.Users
{
    public class AdminUserFilterRequest
    {
        /// <summary>Case-insensitive match against email, full name or phone.</summary>
        public string? Search { get; set; }

        /// <summary>Filter by role name (e.g. admin | coach | learner).</summary>
        public string? Role { get; set; }

        /// <summary>Filter by user status (active | inactive | banned | pending).</summary>
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
