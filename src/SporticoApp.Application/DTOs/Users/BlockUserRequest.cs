namespace SporticoApp.Application.DTOs.Users
{
    public class BlockUserRequest
    {
        public string? Reason { get; set; }
    }

    public class BlockedUserResponse
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Reason { get; set; }
    }
}
