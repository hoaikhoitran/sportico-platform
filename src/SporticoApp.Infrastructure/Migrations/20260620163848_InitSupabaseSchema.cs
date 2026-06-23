using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitSupabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    max_posts = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("packages_pkey", x => x.id);
                },
                comment: "Gói dịch vụ dành cho coach (basic, pro, premium...)");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                },
                comment: "Danh sách vai trò: admin, coach, learner");

            migrationBuilder.CreateTable(
                name: "sports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "URL-friendly identifier, vd: cau-long, bong-da"),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("sports_pkey", x => x.id);
                },
                comment: "Danh mục môn thể thao");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: true),
                    email_verification_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_reset_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_reset_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'active'::character varying", comment: "active | inactive | banned | pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                },
                comment: "Bảng người dùng cốt lõi");

            migrationBuilder.CreateTable(
                name: "v_coaches",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "citext", nullable: true),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    headline = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    experience_years = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    total_reviews = table.Column<int>(type: "integer", nullable: true),
                    coach_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sports = table.Column<List<string>>(type: "character varying[]", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "v_published_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coach_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    coach_avatar = table.Column<string>(type: "text", nullable: true),
                    coach_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    coach_total_reviews = table.Column<int>(type: "integer", nullable: true),
                    sport_id = table.Column<int>(type: "integer", nullable: true),
                    sport_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sport_slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "advisory_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiator_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "learner | admin"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("advisory_conversations_pkey", x => x.id);
                    table.CheckConstraint("chk_advisory_conversations_initiator_role", "initiator_role IN ('learner','admin')");
                    table.ForeignKey(
                        name: "fk_advisory_conversations_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "AI advisory chatbot conversations started by a learner or admin");

            migrationBuilder.CreateTable(
                name: "chat_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_rooms_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_rooms_user1",
                        column: x => x.user1_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_rooms_user2",
                        column: x => x.user2_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Phòng chat 1-1 giữa 2 user");

            migrationBuilder.CreateTable(
                name: "coach_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    experience_years = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    headline = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    teaching_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    teaching_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    teaching_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    teaching_latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    teaching_longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_online_available = table.Column<bool>(type: "boolean", nullable: true),
                    is_offline_available = table.Column<bool>(type: "boolean", nullable: true),
                    specialties = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    certifications_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    achievements_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    website_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false, defaultValueSql: "0.00", comment: "Cache: trung bình rating từ bảng reviews"),
                    total_reviews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Cache: tổng số review"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_profiles_pkey", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_coach_profiles_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Hồ sơ huấn luyện viên");

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    following_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("follows_pkey", x => new { x.follower_id, x.following_id });
                    table.ForeignKey(
                        name: "fk_follows_follower",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_follows_following",
                        column: x => x.following_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "User theo dõi user khác (chủ yếu learner follow coach)");

            migrationBuilder.CreateTable(
                name: "learner_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learner_profiles_pkey", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_learner_profiles_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Hồ sơ học viên");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "message | review | follow | payment | package | post | system | report | booking | training_package | training_session | training_plan | wallet"),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.id);
                    table.CheckConstraint("chk_notifications_type", "type IN ('message','review','follow','payment','package','post','system','report','booking','training_package','training_session','training_plan','wallet')");
                    table.ForeignKey(
                        name: "fk_notifications_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Thông báo cho user");

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Polymorphic: liên kết với coach_package hoặc đối tượng khác"),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "ID của đối tượng được thanh toán (vd: coach_packages.id)"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    transaction_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    order_code = table.Column<long>(type: "bigint", nullable: true),
                    payment_link_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    checkout_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_pkey", x => x.id);
                    table.CheckConstraint("chk_payments_method", "method IN ('manual', 'payos')");
                    table.CheckConstraint("chk_payments_status", "status IN ('pending', 'paid', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_payments_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Giao dịch thanh toán");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'user'::character varying", comment: "user | review"),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | reviewing | resolved | rejected"),
                    handled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    action_taken = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "none | review_hidden | review_deleted"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reports_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_reporter",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_target",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Báo cáo vi phạm");

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_roles_pkey", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_role",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Many-to-many giữa users và roles");

            migrationBuilder.CreateTable(
                name: "advisory_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "user | assistant"),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("advisory_messages_pkey", x => x.id);
                    table.CheckConstraint("chk_advisory_messages_sender", "sender IN ('user','assistant')");
                    table.ForeignKey(
                        name: "fk_advisory_messages_conversation",
                        column: x => x.conversation_id,
                        principalTable: "advisory_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Turns within an advisory conversation");

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("messages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_room",
                        column: x => x.room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_sender",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tin nhắn trong phòng chat");

            migrationBuilder.CreateTable(
                name: "coach_availability_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'available'::character varying", comment: "available | booked | cancelled | expired"),
                    max_participants = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Maximum learners that can book this slot"),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    meeting_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_availability_slots_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_availability_slots_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Time slots a coach publishes as bookable");

            migrationBuilder.CreateTable(
                name: "coach_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remaining_posts = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | active | expired | cancelled"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_packages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_packages_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_coach_packages_package",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Lịch sử mua gói của coach");

            migrationBuilder.CreateTable(
                name: "coach_payout_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bank_bin = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_account_holder = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | verified | rejected"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_payout_accounts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_payout_accounts_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Coach payout account");

            migrationBuilder.CreateTable(
                name: "coach_profile_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    media_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_profile_media_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_profile_media_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Media (image URLs) cho hồ sơ huấn luyện viên: certificate/award/gallery");

            migrationBuilder.CreateTable(
                name: "coach_sports",
                columns: table => new
                {
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_sports_pkey", x => new { x.coach_id, x.sport_id });
                    table.ForeignKey(
                        name: "fk_coach_sports_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_coach_sports_sport",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Many-to-many: coach dạy những môn nào");

            migrationBuilder.CreateTable(
                name: "coach_teaching_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_teaching_locations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_teaching_locations_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Các địa điểm dạy offline của huấn luyện viên");

            migrationBuilder.CreateTable(
                name: "coach_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    pending_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_earned = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_withdrawn = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_wallets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_wallets_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Internal wallet for coach");

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'draft'::character varying", comment: "draft | pending | published | archived | rejected"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("posts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_posts_sport",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Bài đăng dịch vụ huấn luyện");

            migrationBuilder.CreateTable(
                name: "training_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    session_count = table.Column<int>(type: "integer", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | published | rejected | archived"),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_packages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_packages_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_packages_sport",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training packages created by coaches");

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_response = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_transactions_payment",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Log raw response từ payment gateway (audit trail)");

            migrationBuilder.CreateTable(
                name: "message_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_attachments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_attachments_message",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "File đính kèm tin nhắn (image, video, doc...)");

            migrationBuilder.CreateTable(
                name: "coach_wallet_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_wallet_transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_wallet_transactions_wallet",
                        column: x => x.coach_wallet_id,
                        principalTable: "coach_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Coach wallet transactions");

            migrationBuilder.CreateTable(
                name: "withdrawal_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_payout_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | approved | rejected | paid | cancelled"),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pay_os_payout_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pay_os_reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pay_os_payout_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pay_os_raw_response = table.Column<string>(type: "text", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("withdrawal_requests_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_payout_account",
                        column: x => x.coach_payout_account_id,
                        principalTable: "coach_payout_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_wallet",
                        column: x => x.coach_wallet_id,
                        principalTable: "coach_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Withdrawal requests from coach wallet");

            migrationBuilder.CreateTable(
                name: "post_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("post_images_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_images_post",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Hình ảnh kèm bài đăng");

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    platform_fee_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    platform_fee_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    coach_receive_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    per_session_coach_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending_payment'::character varying", comment: "pending_payment | active | completed | cancelled | refunded"),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bookings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_training_package",
                        column: x => x.training_package_id,
                        principalTable: "training_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Bookings for training package purchases");

            migrationBuilder.CreateTable(
                name: "learner_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    goal_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    height_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    current_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    health_notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    injury_notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    training_history = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    available_days_per_week = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferred_session_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    equipment_available = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learner_assessments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_learner_assessments_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learner_assessments_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_learner_assessments_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Learner assessment for personalization");

            migrationBuilder.CreateTable(
                name: "progress_check_ins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    waist_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    energy_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sleep_quality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    learner_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coach_feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("progress_check_ins_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Progress check-ins for bookings");

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'active'::character varying", comment: "active | hidden | deleted"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reviews_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reviews_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_post",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Đánh giá từ learner cho coach");

            migrationBuilder.CreateTable(
                name: "training_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    overview = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_weeks = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'draft'::character varying", comment: "draft | active | completed | cancelled"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plans_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plans_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_training_plans_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_plans_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training plans for bookings");

            migrationBuilder.CreateTable(
                name: "training_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability_slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'requested'::character varying", comment: "requested | scheduled | completed | cancelled | missed"),
                    meeting_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    learner_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coach_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_sessions_availability_slot",
                        column: x => x.availability_slot_id,
                        principalTable: "coach_availability_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_training_sessions_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_training_sessions_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_sessions_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training session schedule for bookings");

            migrationBuilder.CreateTable(
                name: "training_plan_weeks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_weeks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_weeks_plan",
                        column: x => x.training_plan_id,
                        principalTable: "training_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Weekly breakdown for training plans");

            migrationBuilder.CreateTable(
                name: "training_plan_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_week_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_days_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_days_week",
                        column: x => x.training_plan_week_id,
                        principalTable: "training_plan_weeks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Daily breakdown for training plans");

            migrationBuilder.CreateTable(
                name: "training_plan_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    intensity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_exercises_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_exercises_day",
                        column: x => x.training_plan_day_id,
                        principalTable: "training_plan_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Exercises for training plan days");

            migrationBuilder.CreateIndex(
                name: "idx_advisory_conversations_user",
                table: "advisory_conversations",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_advisory_messages_conversation",
                table: "advisory_messages",
                columns: new[] { "conversation_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_bookings_coach",
                table: "bookings",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_created_at",
                table: "bookings",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_bookings_learner",
                table: "bookings",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_training_package",
                table: "bookings",
                column: "training_package_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_rooms_user1",
                table: "chat_rooms",
                column: "user1_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_rooms_user2",
                table: "chat_rooms",
                column: "user2_id");

            migrationBuilder.CreateIndex(
                name: "uq_chat_rooms_pair",
                table: "chat_rooms",
                columns: new[] { "user1_id", "user2_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_coach_availability_slots_coach",
                table: "coach_availability_slots",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_availability_slots_coach_start",
                table: "coach_availability_slots",
                columns: new[] { "coach_id", "start_time" });

            migrationBuilder.CreateIndex(
                name: "idx_coach_availability_slots_start_time",
                table: "coach_availability_slots",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "idx_coach_availability_slots_status",
                table: "coach_availability_slots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_coach_availability_slots_coach_time",
                table: "coach_availability_slots",
                columns: new[] { "coach_id", "start_time", "end_time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_coach_packages_coach",
                table: "coach_packages",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_packages_status",
                table: "coach_packages",
                columns: new[] { "status", "end_date" },
                filter: "((status)::text = 'active'::text)");

            migrationBuilder.CreateIndex(
                name: "IX_coach_packages_package_id",
                table: "coach_packages",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_payout_accounts_status",
                table: "coach_payout_accounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_coach_payout_accounts_coach",
                table: "coach_payout_accounts",
                column: "coach_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_coach_profile_media_coach",
                table: "coach_profile_media",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_profile_media_type",
                table: "coach_profile_media",
                column: "media_type");

            migrationBuilder.CreateIndex(
                name: "idx_coach_profiles_rating",
                table: "coach_profiles",
                columns: new[] { "rating", "total_reviews" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_coach_sports_sport",
                table: "coach_sports",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_teaching_locations_coach",
                table: "coach_teaching_locations",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_coach",
                table: "coach_wallet_transactions",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_created_at",
                table: "coach_wallet_transactions",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_reference",
                table: "coach_wallet_transactions",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_wallet",
                table: "coach_wallet_transactions",
                column: "coach_wallet_id");

            migrationBuilder.CreateIndex(
                name: "uq_coach_wallets_coach",
                table: "coach_wallets",
                column: "coach_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_follows_following",
                table: "follows",
                column: "following_id");

            migrationBuilder.CreateIndex(
                name: "IX_learner_assessments_coach_id",
                table: "learner_assessments",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_learner_assessments_learner_id",
                table: "learner_assessments",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "uq_learner_assessments_booking",
                table: "learner_assessments",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_message_attachments_message",
                table: "message_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_room",
                table: "messages",
                columns: new[] { "room_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_unread",
                table: "messages",
                columns: new[] { "room_id", "is_read" },
                filter: "(is_read = false)");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                columns: new[] { "user_id", "is_read" },
                filter: "(is_read = false)");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_packages_active",
                table: "packages",
                column: "is_active",
                filter: "(is_active = true)");

            migrationBuilder.CreateIndex(
                name: "packages_name_key",
                table: "packages",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_payment_transactions_payment",
                table: "payment_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_created_at",
                table: "payments",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_payments_order_code",
                table: "payments",
                column: "order_code",
                unique: true,
                filter: "(order_code IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_payments_reference",
                table: "payments",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "idx_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_payments_user",
                table: "payments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "payments_transaction_code_key",
                table: "payments",
                column: "transaction_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_post_images_post",
                table: "post_images",
                columns: new[] { "post_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_posts_coach",
                table: "posts",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_posts_created_at",
                table: "posts",
                column: "created_at",
                descending: new bool[0],
                filter: "((status)::text = 'published'::text)");

            migrationBuilder.CreateIndex(
                name: "idx_posts_sport",
                table: "posts",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "idx_posts_status",
                table: "posts",
                column: "status",
                filter: "((status)::text = 'published'::text)");

            migrationBuilder.CreateIndex(
                name: "idx_progress_check_ins_booking_created_at",
                table: "progress_check_ins",
                columns: new[] { "booking_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_progress_check_ins_coach_id",
                table: "progress_check_ins",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_check_ins_learner_id",
                table: "progress_check_ins",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_reports_status",
                table: "reports",
                column: "status",
                filter: "((status)::text = ANY ((ARRAY['pending'::character varying, 'reviewing'::character varying])::text[]))");

            migrationBuilder.CreateIndex(
                name: "idx_reports_target",
                table: "reports",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_reports_target_entity",
                table: "reports",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_reporter_id",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_booking",
                table: "reviews",
                column: "booking_id",
                filter: "(booking_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_coach",
                table: "reviews",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_coach_status_created",
                table: "reviews",
                columns: new[] { "coach_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_reviews_created_at",
                table: "reviews",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_reviews_learner",
                table: "reviews",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_post",
                table: "reviews",
                column: "post_id",
                filter: "(post_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "uq_reviews_pair",
                table: "reviews",
                columns: new[] { "coach_id", "learner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "roles_name_key",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sports_active",
                table: "sports",
                column: "is_active",
                filter: "(is_active = true)");

            migrationBuilder.CreateIndex(
                name: "sports_name_key",
                table: "sports",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sports_slug_key",
                table: "sports",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_coach",
                table: "training_packages",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_created_at",
                table: "training_packages",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_published",
                table: "training_packages",
                column: "status",
                filter: "((status)::text = 'published'::text)");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_sport",
                table: "training_packages",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_status",
                table: "training_packages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_days_week_day",
                table: "training_plan_days",
                columns: new[] { "training_plan_week_id", "day_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_exercises_day_order",
                table: "training_plan_exercises",
                columns: new[] { "training_plan_day_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_weeks_plan_week",
                table: "training_plan_weeks",
                columns: new[] { "training_plan_id", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_coach",
                table: "training_plans",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_learner",
                table: "training_plans",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_status",
                table: "training_plans",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_training_plans_booking",
                table: "training_plans",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_booking",
                table: "training_sessions",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_coach",
                table: "training_sessions",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_coach_time",
                table: "training_sessions",
                columns: new[] { "coach_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_learner",
                table: "training_sessions",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_learner_time",
                table: "training_sessions",
                columns: new[] { "learner_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_status",
                table: "training_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_training_sessions_availability_slot_id",
                table: "training_sessions",
                column: "availability_slot_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_role",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_created_at",
                table: "users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_users_status",
                table: "users",
                column: "status",
                filter: "((status)::text <> 'active'::text)");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_coach",
                table: "withdrawal_requests",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_created_at",
                table: "withdrawal_requests",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_status",
                table: "withdrawal_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_coach_payout_account_id",
                table: "withdrawal_requests",
                column: "coach_payout_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_coach_wallet_id",
                table: "withdrawal_requests",
                column: "coach_wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advisory_messages");

            migrationBuilder.DropTable(
                name: "coach_packages");

            migrationBuilder.DropTable(
                name: "coach_profile_media");

            migrationBuilder.DropTable(
                name: "coach_sports");

            migrationBuilder.DropTable(
                name: "coach_teaching_locations");

            migrationBuilder.DropTable(
                name: "coach_wallet_transactions");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "learner_assessments");

            migrationBuilder.DropTable(
                name: "learner_profiles");

            migrationBuilder.DropTable(
                name: "message_attachments");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "post_images");

            migrationBuilder.DropTable(
                name: "progress_check_ins");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "training_plan_exercises");

            migrationBuilder.DropTable(
                name: "training_sessions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "v_coaches");

            migrationBuilder.DropTable(
                name: "v_published_posts");

            migrationBuilder.DropTable(
                name: "withdrawal_requests");

            migrationBuilder.DropTable(
                name: "advisory_conversations");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "training_plan_days");

            migrationBuilder.DropTable(
                name: "coach_availability_slots");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "coach_payout_accounts");

            migrationBuilder.DropTable(
                name: "coach_wallets");

            migrationBuilder.DropTable(
                name: "chat_rooms");

            migrationBuilder.DropTable(
                name: "training_plan_weeks");

            migrationBuilder.DropTable(
                name: "training_plans");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "training_packages");

            migrationBuilder.DropTable(
                name: "coach_profiles");

            migrationBuilder.DropTable(
                name: "sports");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
