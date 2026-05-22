using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<chat_rooms> chat_rooms { get; set; }

    public virtual DbSet<coach_packages> coach_packages { get; set; }

    public virtual DbSet<coach_profiles> coach_profiles { get; set; }

    public virtual DbSet<coach_sports> coach_sports { get; set; }

    public virtual DbSet<follows> follows { get; set; }

    public virtual DbSet<learner_profiles> learner_profiles { get; set; }

    public virtual DbSet<message_attachments> message_attachments { get; set; }

    public virtual DbSet<messages> messages { get; set; }

    public virtual DbSet<notifications> notifications { get; set; }

    public virtual DbSet<packages> packages { get; set; }

    public virtual DbSet<payment_transactions> payment_transactions { get; set; }

    public virtual DbSet<payments> payments { get; set; }

    public virtual DbSet<post_images> post_images { get; set; }

    public virtual DbSet<posts> posts { get; set; }

    public virtual DbSet<reports> reports { get; set; }

    public virtual DbSet<reviews> reviews { get; set; }

    public virtual DbSet<roles> roles { get; set; }

    public virtual DbSet<sports> sports { get; set; }

    public virtual DbSet<user_roles> user_roles { get; set; }

    public virtual DbSet<users> users { get; set; }

    public virtual DbSet<v_coaches> v_coaches { get; set; }

    public virtual DbSet<v_published_posts> v_published_posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("citext")
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<chat_rooms>(entity =>
        {
            entity.HasKey(e => e.id).HasName("chat_rooms_pkey");

            entity.ToTable(tb => tb.HasComment("Phòng chat 1-1 giữa 2 user"));

            entity.HasIndex(e => e.user1_id, "idx_chat_rooms_user1");

            entity.HasIndex(e => e.user2_id, "idx_chat_rooms_user2");

            entity.HasIndex(e => new { e.user1_id, e.user2_id }, "uq_chat_rooms_pair").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user1).WithMany(p => p.chat_roomsuser1)
                .HasForeignKey(d => d.user1_id)
                .HasConstraintName("fk_chat_rooms_user1");

            entity.HasOne(d => d.user2).WithMany(p => p.chat_roomsuser2)
                .HasForeignKey(d => d.user2_id)
                .HasConstraintName("fk_chat_rooms_user2");
        });

        modelBuilder.Entity<coach_packages>(entity =>
        {
            entity.HasKey(e => e.id).HasName("coach_packages_pkey");

            entity.ToTable(tb => tb.HasComment("Lịch sử mua gói của coach"));

            entity.HasIndex(e => e.coach_id, "idx_coach_packages_coach");

            entity.HasIndex(e => new { e.status, e.end_date }, "idx_coach_packages_status").HasFilter("((status)::text = 'active'::text)");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.start_date).HasDefaultValueSql("now()");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasComment("pending | active | expired | cancelled");

            entity.HasOne(d => d.coach).WithMany(p => p.coach_packages)
                .HasForeignKey(d => d.coach_id)
                .HasConstraintName("fk_coach_packages_coach");

            entity.HasOne(d => d.package).WithMany(p => p.coach_packages)
                .HasForeignKey(d => d.package_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_coach_packages_package");
        });

        modelBuilder.Entity<coach_profiles>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("coach_profiles_pkey");

            entity.ToTable(tb => tb.HasComment("Hồ sơ huấn luyện viên"));

            entity.HasIndex(e => new { e.rating, e.total_reviews }, "idx_coach_profiles_rating").IsDescending();

            entity.Property(e => e.user_id).ValueGeneratedNever();
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.experience_years).HasDefaultValue(0);
            entity.Property(e => e.headline).HasMaxLength(255);
            entity.Property(e => e.rating)
                .HasPrecision(3, 2)
                .HasDefaultValueSql("0.00")
                .HasComment("Cache: trung bình rating từ bảng reviews");
            entity.Property(e => e.total_reviews)
                .HasDefaultValue(0)
                .HasComment("Cache: tổng số review");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithOne(p => p.coach_profiles)
                .HasForeignKey<coach_profiles>(d => d.user_id)
                .HasConstraintName("fk_coach_profiles_user");
        });

        modelBuilder.Entity<coach_sports>(entity =>
        {
            entity.HasKey(e => new { e.coach_id, e.sport_id }).HasName("coach_sports_pkey");

            entity.ToTable(tb => tb.HasComment("Many-to-many: coach dạy những môn nào"));

            entity.HasIndex(e => e.sport_id, "idx_coach_sports_sport");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.coach).WithMany(p => p.coach_sports)
                .HasForeignKey(d => d.coach_id)
                .HasConstraintName("fk_coach_sports_coach");

            entity.HasOne(d => d.sport).WithMany(p => p.coach_sports)
                .HasForeignKey(d => d.sport_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_coach_sports_sport");
        });

        modelBuilder.Entity<follows>(entity =>
        {
            entity.HasKey(e => new { e.follower_id, e.following_id }).HasName("follows_pkey");

            entity.ToTable(tb => tb.HasComment("User theo dõi user khác (chủ yếu learner follow coach)"));

            entity.HasIndex(e => e.following_id, "idx_follows_following");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.follower).WithMany(p => p.followsfollower)
                .HasForeignKey(d => d.follower_id)
                .HasConstraintName("fk_follows_follower");

            entity.HasOne(d => d.following).WithMany(p => p.followsfollowing)
                .HasForeignKey(d => d.following_id)
                .HasConstraintName("fk_follows_following");
        });

        modelBuilder.Entity<learner_profiles>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("learner_profiles_pkey");

            entity.ToTable(tb => tb.HasComment("Hồ sơ học viên"));

            entity.Property(e => e.user_id).ValueGeneratedNever();
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithOne(p => p.learner_profiles)
                .HasForeignKey<learner_profiles>(d => d.user_id)
                .HasConstraintName("fk_learner_profiles_user");
        });

        modelBuilder.Entity<message_attachments>(entity =>
        {
            entity.HasKey(e => e.id).HasName("message_attachments_pkey");

            entity.ToTable(tb => tb.HasComment("File đính kèm tin nhắn (image, video, doc...)"));

            entity.HasIndex(e => e.message_id, "idx_message_attachments_message");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.file_type).HasMaxLength(50);

            entity.HasOne(d => d.message).WithMany(p => p.message_attachments)
                .HasForeignKey(d => d.message_id)
                .HasConstraintName("fk_message_attachments_message");
        });

        modelBuilder.Entity<messages>(entity =>
        {
            entity.HasKey(e => e.id).HasName("messages_pkey");

            entity.ToTable(tb => tb.HasComment("Tin nhắn trong phòng chat"));

            entity.HasIndex(e => new { e.room_id, e.sent_at }, "idx_messages_room").IsDescending(false, true);

            entity.HasIndex(e => e.sender_id, "idx_messages_sender");

            entity.HasIndex(e => new { e.room_id, e.is_read }, "idx_messages_unread").HasFilter("(is_read = false)");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.is_read).HasDefaultValue(false);
            entity.Property(e => e.sent_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.room).WithMany(p => p.messages)
                .HasForeignKey(d => d.room_id)
                .HasConstraintName("fk_messages_room");

            entity.HasOne(d => d.sender).WithMany(p => p.messages)
                .HasForeignKey(d => d.sender_id)
                .HasConstraintName("fk_messages_sender");
        });

        modelBuilder.Entity<notifications>(entity =>
        {
            entity.HasKey(e => e.id).HasName("notifications_pkey");

            entity.ToTable(tb => tb.HasComment("Thông báo cho user"));

            entity.HasIndex(e => new { e.user_id, e.is_read }, "idx_notifications_unread").HasFilter("(is_read = false)");

            entity.HasIndex(e => new { e.user_id, e.created_at }, "idx_notifications_user").IsDescending(false, true);

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_read).HasDefaultValue(false);
            entity.Property(e => e.title).HasMaxLength(255);
            entity.Property(e => e.type)
                .HasMaxLength(50)
                .HasComment("message | review | follow | payment | package | system | report");

            entity.HasOne(d => d.user).WithMany(p => p.notifications)
                .HasForeignKey(d => d.user_id)
                .HasConstraintName("fk_notifications_user");
        });

        modelBuilder.Entity<packages>(entity =>
        {
            entity.HasKey(e => e.id).HasName("packages_pkey");

            entity.ToTable(tb => tb.HasComment("Gói dịch vụ dành cho coach (basic, pro, premium...)"));

            entity.HasIndex(e => e.is_active, "idx_packages_active").HasFilter("(is_active = true)");

            entity.HasIndex(e => e.name, "packages_name_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<payment_transactions>(entity =>
        {
            entity.HasKey(e => e.id).HasName("payment_transactions_pkey");

            entity.ToTable(tb => tb.HasComment("Log raw response từ payment gateway (audit trail)"));

            entity.HasIndex(e => e.payment_id, "idx_payment_transactions_payment");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.payment).WithMany(p => p.payment_transactions)
                .HasForeignKey(d => d.payment_id)
                .HasConstraintName("fk_payment_transactions_payment");
        });

        modelBuilder.Entity<payments>(entity =>
        {
            entity.HasKey(e => e.id).HasName("payments_pkey");

            entity.ToTable(tb => tb.HasComment("Giao dịch thanh toán"));

            entity.HasIndex(e => e.created_at, "idx_payments_created_at").IsDescending();

            entity.HasIndex(e => new { e.reference_type, e.reference_id }, "idx_payments_reference");

            entity.HasIndex(e => e.status, "idx_payments_status");

            entity.HasIndex(e => e.user_id, "idx_payments_user");

            entity.HasIndex(e => e.transaction_code, "payments_transaction_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.amount).HasPrecision(12, 2);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.method).HasMaxLength(50);
            entity.Property(e => e.reference_id).HasComment("ID của đối tượng được thanh toán (vd: coach_packages.id)");
            entity.Property(e => e.reference_type)
                .HasMaxLength(50)
                .HasComment("Polymorphic: liên kết với coach_package hoặc đối tượng khác");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying");
            entity.Property(e => e.transaction_code).HasMaxLength(100);

            entity.HasOne(d => d.user).WithMany(p => p.payments)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payments_user");
        });

        modelBuilder.Entity<post_images>(entity =>
        {
            entity.HasKey(e => e.id).HasName("post_images_pkey");

            entity.ToTable(tb => tb.HasComment("Hình ảnh kèm bài đăng"));

            entity.HasIndex(e => new { e.post_id, e.order_index }, "idx_post_images_post");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.order_index).HasDefaultValue(0);

            entity.HasOne(d => d.post).WithMany(p => p.post_images)
                .HasForeignKey(d => d.post_id)
                .HasConstraintName("fk_post_images_post");
        });

        modelBuilder.Entity<posts>(entity =>
        {
            entity.HasKey(e => e.id).HasName("posts_pkey");

            entity.ToTable(tb => tb.HasComment("Bài đăng dịch vụ huấn luyện"));

            entity.HasIndex(e => e.coach_id, "idx_posts_coach");

            entity.HasIndex(e => e.created_at, "idx_posts_created_at")
                .IsDescending()
                .HasFilter("((status)::text = 'published'::text)");

            entity.HasIndex(e => e.sport_id, "idx_posts_sport");

            entity.HasIndex(e => e.status, "idx_posts_status").HasFilter("((status)::text = 'published'::text)");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_online).HasDefaultValue(false);
            entity.Property(e => e.location).HasMaxLength(255);
            entity.Property(e => e.price).HasPrecision(12, 2);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'draft'::character varying")
                .HasComment("draft | published | archived | rejected");
            entity.Property(e => e.title).HasMaxLength(255);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.coach).WithMany(p => p.posts)
                .HasForeignKey(d => d.coach_id)
                .HasConstraintName("fk_posts_coach");

            entity.HasOne(d => d.sport).WithMany(p => p.posts)
                .HasForeignKey(d => d.sport_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_posts_sport");
        });

        modelBuilder.Entity<reports>(entity =>
        {
            entity.HasKey(e => e.id).HasName("reports_pkey");

            entity.ToTable(tb => tb.HasComment("Báo cáo vi phạm"));

            entity.HasIndex(e => e.status, "idx_reports_status").HasFilter("((status)::text = ANY ((ARRAY['pending'::character varying, 'reviewing'::character varying])::text[]))");

            entity.HasIndex(e => e.target_user_id, "idx_reports_target");

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasComment("pending | reviewing | resolved | rejected");

            entity.HasOne(d => d.reporter).WithMany(p => p.reportsreporter)
                .HasForeignKey(d => d.reporter_id)
                .HasConstraintName("fk_reports_reporter");

            entity.HasOne(d => d.target_user).WithMany(p => p.reportstarget_user)
                .HasForeignKey(d => d.target_user_id)
                .HasConstraintName("fk_reports_target");
        });

        modelBuilder.Entity<reviews>(entity =>
        {
            entity.HasKey(e => e.id).HasName("reviews_pkey");

            entity.ToTable(tb => tb.HasComment("Đánh giá từ learner cho coach"));

            entity.HasIndex(e => e.coach_id, "idx_reviews_coach");

            entity.HasIndex(e => e.created_at, "idx_reviews_created_at").IsDescending();

            entity.HasIndex(e => e.learner_id, "idx_reviews_learner");

            entity.HasIndex(e => e.post_id, "idx_reviews_post").HasFilter("(post_id IS NOT NULL)");

            entity.HasIndex(e => new { e.coach_id, e.learner_id }, "uq_reviews_pair").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.coach).WithMany(p => p.reviews)
                .HasForeignKey(d => d.coach_id)
                .HasConstraintName("fk_reviews_coach");

            entity.HasOne(d => d.learner).WithMany(p => p.reviews)
                .HasForeignKey(d => d.learner_id)
                .HasConstraintName("fk_reviews_learner");

            entity.HasOne(d => d.post).WithMany(p => p.reviews)
                .HasForeignKey(d => d.post_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reviews_post");
        });

        modelBuilder.Entity<roles>(entity =>
        {
            entity.HasKey(e => e.id).HasName("roles_pkey");

            entity.ToTable(tb => tb.HasComment("Danh sách vai trò: admin, coach, learner"));

            entity.HasIndex(e => e.name, "roles_name_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.name).HasMaxLength(50);
        });

        modelBuilder.Entity<sports>(entity =>
        {
            entity.HasKey(e => e.id).HasName("sports_pkey");

            entity.ToTable(tb => tb.HasComment("Danh mục môn thể thao"));

            entity.HasIndex(e => e.is_active, "idx_sports_active").HasFilter("(is_active = true)");

            entity.HasIndex(e => e.name, "sports_name_key").IsUnique();

            entity.HasIndex(e => e.slug, "sports_slug_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.slug)
                .HasMaxLength(120)
                .HasComment("URL-friendly identifier, vd: cau-long, bong-da");
        });

        modelBuilder.Entity<user_roles>(entity =>
        {
            entity.HasKey(e => new { e.user_id, e.role_id }).HasName("user_roles_pkey");

            entity.ToTable(tb => tb.HasComment("Many-to-many giữa users và roles"));

            entity.HasIndex(e => e.role_id, "idx_user_roles_role");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.role).WithMany(p => p.user_roles)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_roles_role");

            entity.HasOne(d => d.user).WithMany(p => p.user_roles)
                .HasForeignKey(d => d.user_id)
                .HasConstraintName("fk_user_roles_user");
        });

        modelBuilder.Entity<users>(entity =>
        {
            entity.HasKey(e => e.id).HasName("users_pkey");

            entity.ToTable(tb => tb.HasComment("Bảng người dùng cốt lõi"));

            entity.HasIndex(e => e.created_at, "idx_users_created_at").IsDescending();

            entity.HasIndex(e => e.status, "idx_users_status").HasFilter("((status)::text <> 'active'::text)");

            entity.HasIndex(e => e.email, "users_email_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasColumnType("citext");
            entity.Property(e => e.full_name).HasMaxLength(150);
            entity.Property(e => e.phone).HasMaxLength(20);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasComment("active | inactive | banned | pending");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<v_coaches>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_coaches");

            entity.Property(e => e.email).HasColumnType("citext");
            entity.Property(e => e.full_name).HasMaxLength(150);
            entity.Property(e => e.headline).HasMaxLength(255);
            entity.Property(e => e.phone).HasMaxLength(20);
            entity.Property(e => e.rating).HasPrecision(3, 2);
            entity.Property(e => e.sports).HasColumnType("character varying[]");
            entity.Property(e => e.status).HasMaxLength(20);
        });

        modelBuilder.Entity<v_published_posts>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_published_posts");

            entity.Property(e => e.coach_name).HasMaxLength(150);
            entity.Property(e => e.coach_rating).HasPrecision(3, 2);
            entity.Property(e => e.location).HasMaxLength(255);
            entity.Property(e => e.price).HasPrecision(12, 2);
            entity.Property(e => e.sport_name).HasMaxLength(100);
            entity.Property(e => e.sport_slug).HasMaxLength(120);
            entity.Property(e => e.title).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
