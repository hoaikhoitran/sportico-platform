using System;
using System.Collections.Generic;

namespace SporticoApp.Application.DTOs.Users
{
    public class AdminUserResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public CoachProfileSummaryResponse? CoachProfile { get; set; }

        public LearnerProfileSummaryResponse? LearnerProfile { get; set; }
    }
}
