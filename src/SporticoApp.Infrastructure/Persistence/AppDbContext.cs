using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SporticoApp.Core.Entities;
using System.Text;

namespace SporticoApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdvisoryConversation> AdvisoryConversations => Set<AdvisoryConversation>();
    public DbSet<AdvisoryMessage> AdvisoryMessages => Set<AdvisoryMessage>();
    public DbSet<ApiRequestMetric> ApiRequestMetrics => Set<ApiRequestMetric>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<CoachAvailabilitySlot> CoachAvailabilitySlots => Set<CoachAvailabilitySlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<CoachPayoutAccount> CoachPayoutAccounts => Set<CoachPayoutAccount>();
    public DbSet<CoachWallet> CoachWallets => Set<CoachWallet>();
    public DbSet<CoachWalletTransaction> CoachWalletTransactions => Set<CoachWalletTransaction>();
    public DbSet<CoachPackage> CoachPackages => Set<CoachPackage>();
    public DbSet<CoachProfile> CoachProfiles => Set<CoachProfile>();
    public DbSet<CoachProfileMedia> CoachProfileMedia => Set<CoachProfileMedia>();
    public DbSet<CoachTeachingLocation> CoachTeachingLocations => Set<CoachTeachingLocation>();
    public DbSet<CoachSport> CoachSports => Set<CoachSport>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<LearnerAssessment> LearnerAssessments => Set<LearnerAssessment>();
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<ProgressCheckIn> ProgressCheckIns => Set<ProgressCheckIn>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<TrainingPackage> TrainingPackages => Set<TrainingPackage>();
    public DbSet<TrainingPackageSessionSlot> TrainingPackageSessionSlots => Set<TrainingPackageSessionSlot>();
    public DbSet<TrainingPlanDay> TrainingPlanDays => Set<TrainingPlanDay>();
    public DbSet<TrainingPlanExercise> TrainingPlanExercises => Set<TrainingPlanExercise>();
    public DbSet<TrainingPlanWeek> TrainingPlanWeeks => Set<TrainingPlanWeek>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<User> Users => Set<User>();
    public DbSet<VCoach> VCoaches => Set<VCoach>();
    public DbSet<VisitorSession> VisitorSessions => Set<VisitorSession>();
    public DbSet<VPublishedPost> VPublishedPosts => Set<VPublishedPost>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplySnakeCaseNames(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entityType.SetTableName(ToSnakeCase(tableName));

                var tableId = StoreObjectIdentifier.Table(
                    entityType.GetTableName()!,
                    entityType.GetSchema());

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(tableId);
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        property.SetColumnName(ToSnakeCase(columnName));
                    }
                }
            }

            var viewName = entityType.GetViewName();
            if (!string.IsNullOrWhiteSpace(viewName))
            {
                entityType.SetViewName(ToSnakeCase(viewName));

                var viewId = StoreObjectIdentifier.View(
                    entityType.GetViewName()!,
                    entityType.GetViewSchema());

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(viewId);
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        property.SetColumnName(ToSnakeCase(columnName));
                    }
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 8);
        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (char.IsUpper(ch))
            {
                if (i > 0 && input[i - 1] != '_')
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
