using System;

namespace SporticoApp.Application.DTOs.Users
{
    public class UpdateMeRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}
