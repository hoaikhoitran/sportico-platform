using System;
using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.Users
{
    public class AdminUpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Null = keep current roles unchanged. When provided, replaces the user's roles
        /// with exactly these (all must exist).
        /// </summary>
        public List<string>? Roles { get; set; }
    }
}
