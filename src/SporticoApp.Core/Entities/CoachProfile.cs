using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Hồ sơ huấn luyện viên
/// </summary>
public partial class CoachProfile
{
    public Guid UserId { get; set; }

    public string? Bio { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Headline { get; set; }

    /// <summary>
    /// Banner/cover image URL for the coach profile (User.AvatarUrl remains the avatar).
    /// </summary>
    public string? CoverImageUrl { get; set; }

    public string? TeachingAddress { get; set; }

    public string? TeachingCity { get; set; }

    public string? TeachingDistrict { get; set; }

    public decimal? TeachingLatitude { get; set; }

    public decimal? TeachingLongitude { get; set; }

    public bool? IsOnlineAvailable { get; set; }

    public bool? IsOfflineAvailable { get; set; }

    /// <summary>
    /// Short comma/line separated list, e.g. fat loss, muscle gain, football, badminton, rehab.
    /// </summary>
    public string? Specialties { get; set; }

    public string? CertificationsSummary { get; set; }

    public string? AchievementsSummary { get; set; }

    public string? FacebookUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Cache: trung bình Rating từ bảng Review
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>
    /// Cache: tổng số review
    /// </summary>
    public int TotalReviews { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CoachPackage> CoachPackages { get; set; } = new List<CoachPackage>();

    public virtual ICollection<CoachSport> CoachSports { get; set; } = new List<CoachSport>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<TrainingPackage> TrainingPackages { get; set; } = new List<TrainingPackage>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();

    public virtual ICollection<LearnerAssessment> LearnerAssessments { get; set; } = new List<LearnerAssessment>();

    public virtual ICollection<TrainingPlan> TrainingPlans { get; set; } = new List<TrainingPlan>();

    public virtual ICollection<ProgressCheckIn> ProgressCheckIns { get; set; } = new List<ProgressCheckIn>();

    public virtual CoachPayoutAccount? PayoutAccount { get; set; }

    public virtual CoachWallet? Wallet { get; set; }

    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();

    public virtual ICollection<CoachProfileMedia> Media { get; set; } = new List<CoachProfileMedia>();

    public virtual ICollection<CoachTeachingLocation> TeachingLocations { get; set; } = new List<CoachTeachingLocation>();

    public virtual User User { get; set; } = null!;
}
