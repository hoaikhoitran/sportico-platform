using System;
using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.Users
{
    public class AdminCreateUserRequest
    {
        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Password { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public List<string> Roles { get; set; } = new();
    }
}
