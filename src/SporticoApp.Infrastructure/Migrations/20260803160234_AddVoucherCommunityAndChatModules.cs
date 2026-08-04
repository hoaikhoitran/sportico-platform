using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherCommunityAndChatModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_at",
                table: "chat_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_message_at",
                table: "chat_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "chat_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "requested_at",
                table: "chat_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "requested_by_user_id",
                table: "chat_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "chat_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "chat_rooms",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "chat_rooms",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValueSql: "'active'::character varying");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_amount",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Safe backfill for existing bookings: at this point every row's original_amount was just
            // defaulted to 0 by the AddColumn above (no prior code path ever wrote to this new
            // column), so this WHERE clause targets exactly — and only — the rows that need it.
            // discount_amount's default of 0 is already correct for pre-voucher bookings; no
            // backfill needed there.
            migrationBuilder.Sql("UPDATE bookings SET original_amount = total_amount WHERE original_amount = 0;");

            migrationBuilder.AddColumn<Guid>(
                name: "voucher_campaign_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voucher_code_snapshot",
                table: "bookings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voucher_discount_type_snapshot",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "voucher_discount_value_snapshot",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "voucher_max_discount_amount_snapshot",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "community_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<int>(type: "integer", nullable: true),
                    post_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    location_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: true),
                    accepted_participants = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    fee_per_person = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'draft'::character varying"),
                    allow_comments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reaction_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    application_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    hidden_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hidden_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_posts_pkey", x => x.id);
                    table.CheckConstraint("chk_community_posts_accepted_non_negative", "accepted_participants >= 0");
                    table.CheckConstraint("chk_community_posts_application_count_non_negative", "application_count >= 0");
                    table.CheckConstraint("chk_community_posts_comment_count_non_negative", "comment_count >= 0");
                    table.CheckConstraint("chk_community_posts_reaction_count_non_negative", "reaction_count >= 0");
                    table.CheckConstraint("chk_community_posts_view_count_non_negative", "view_count >= 0");
                    table.ForeignKey(
                        name: "fk_community_posts_author",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_posts_sport",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Community forum / player-recruitment posts (independent of the legacy post module)");

            migrationBuilder.CreateTable(
                name: "user_blocks",
                columns: table => new
                {
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_blocks_pkey", x => new { x.blocker_id, x.blocked_user_id });
                    table.ForeignKey(
                        name: "fk_user_blocks_blocked_user",
                        column: x => x.blocked_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_blocks_blocker",
                        column: x => x.blocker_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "One user blocking another (one-directional)");

            migrationBuilder.CreateTable(
                name: "voucher_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    min_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'draft'::character varying"),
                    max_uses_total = table.Column<int>(type: "integer", nullable: true),
                    max_uses_per_learner = table.Column<int>(type: "integer", nullable: true),
                    reserved_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    budget_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    reserved_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    used_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("voucher_campaigns_pkey", x => x.id);
                    table.CheckConstraint("chk_voucher_campaigns_reserved_count_non_negative", "reserved_count >= 0");
                    table.CheckConstraint("chk_voucher_campaigns_reserved_discount_non_negative", "reserved_discount_amount >= 0");
                    table.CheckConstraint("chk_voucher_campaigns_used_count_non_negative", "used_count >= 0");
                    table.CheckConstraint("chk_voucher_campaigns_used_discount_non_negative", "used_discount_amount >= 0");
                    table.ForeignKey(
                        name: "fk_voucher_campaigns_created_by",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Admin-managed, platform-funded discount campaigns for TrainingPackage purchases");

            migrationBuilder.CreateTable(
                name: "community_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'active'::character varying"),
                    reply_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reaction_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hidden_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hidden_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_comments_pkey", x => x.id);
                    table.CheckConstraint("chk_community_comments_reply_count_non_negative", "reply_count >= 0");
                    table.ForeignKey(
                        name: "fk_community_comments_author",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_comments_parent",
                        column: x => x.parent_comment_id,
                        principalTable: "community_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_comments_post",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_post_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'pending'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_post_applications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_post_applications_applicant",
                        column: x => x.applicant_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_post_applications_post",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "A user's request to join a recruitment-type community post");

            migrationBuilder.CreateTable(
                name: "community_post_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'active'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_post_media_pkey", x => x.id);
                    table.CheckConstraint("chk_community_post_media_order_index_non_negative", "order_index >= 0");
                    table.ForeignKey(
                        name: "fk_community_post_media_post",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_post_reactions",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_post_reactions_pkey", x => new { x.post_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_community_post_reactions_post",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_community_post_reactions_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Like on a community post; MVP supports only 'like'");

            migrationBuilder.CreateTable(
                name: "voucher_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    voucher_campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reserved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    release_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("voucher_redemptions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_voucher_redemptions_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_voucher_redemptions_campaign",
                        column: x => x.voucher_campaign_id,
                        principalTable: "voucher_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_voucher_redemptions_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "One learner's use of one voucher campaign against exactly one booking");

            migrationBuilder.CreateIndex(
                name: "idx_chat_rooms_status",
                table: "chat_rooms",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_voucher_campaign",
                table: "bookings",
                column: "voucher_campaign_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_comments_author",
                table: "community_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_comments_parent",
                table: "community_comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_comments_post_status",
                table: "community_comments",
                columns: new[] { "post_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_community_post_applications_applicant",
                table: "community_post_applications",
                column: "applicant_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_post_applications_post_status",
                table: "community_post_applications",
                columns: new[] { "post_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_community_post_applications_post_applicant",
                table: "community_post_applications",
                columns: new[] { "post_id", "applicant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_community_post_media_post",
                table: "community_post_media",
                columns: new[] { "post_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_community_post_reactions_user",
                table: "community_post_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_author",
                table: "community_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_created_at",
                table: "community_posts",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_post_type",
                table: "community_posts",
                column: "post_type");

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_sport",
                table: "community_posts",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_start_at",
                table: "community_posts",
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "idx_community_posts_status",
                table: "community_posts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_user_blocks_blocked_user",
                table: "user_blocks",
                column: "blocked_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_campaigns_end_at",
                table: "voucher_campaigns",
                column: "end_at");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_campaigns_start_at",
                table: "voucher_campaigns",
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_campaigns_status",
                table: "voucher_campaigns",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_campaigns_created_by_user_id",
                table: "voucher_campaigns",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_voucher_campaigns_code",
                table: "voucher_campaigns",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_voucher_redemptions_campaign_status",
                table: "voucher_redemptions",
                columns: new[] { "voucher_campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_voucher_redemptions_learner_campaign_status",
                table: "voucher_redemptions",
                columns: new[] { "learner_id", "voucher_campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_voucher_redemptions_status_expires_at",
                table: "voucher_redemptions",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "uq_voucher_redemptions_booking",
                table: "voucher_redemptions",
                column: "booking_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_voucher_campaign",
                table: "bookings",
                column: "voucher_campaign_id",
                principalTable: "voucher_campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_voucher_campaign",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "community_comments");

            migrationBuilder.DropTable(
                name: "community_post_applications");

            migrationBuilder.DropTable(
                name: "community_post_media");

            migrationBuilder.DropTable(
                name: "community_post_reactions");

            migrationBuilder.DropTable(
                name: "user_blocks");

            migrationBuilder.DropTable(
                name: "voucher_redemptions");

            migrationBuilder.DropTable(
                name: "community_posts");

            migrationBuilder.DropTable(
                name: "voucher_campaigns");

            migrationBuilder.DropIndex(
                name: "idx_chat_rooms_status",
                table: "chat_rooms");

            migrationBuilder.DropIndex(
                name: "idx_bookings_voucher_campaign",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "last_message_at",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "requested_by_user_id",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "status",
                table: "chat_rooms");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "original_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "voucher_campaign_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "voucher_code_snapshot",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "voucher_discount_type_snapshot",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "voucher_discount_value_snapshot",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "voucher_max_discount_amount_snapshot",
                table: "bookings");
        }
    }
}
