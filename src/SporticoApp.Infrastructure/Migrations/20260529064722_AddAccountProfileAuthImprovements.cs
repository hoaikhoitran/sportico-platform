using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountProfileAuthImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_of_birth",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "achievements_summary",
                table: "coach_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "certifications_summary",
                table: "coach_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                table: "coach_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_url",
                table: "coach_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram_url",
                table: "coach_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_offline_available",
                table: "coach_profiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_online_available",
                table: "coach_profiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialties",
                table: "coach_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teaching_address",
                table: "coach_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teaching_city",
                table: "coach_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teaching_district",
                table: "coach_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "teaching_latitude",
                table: "coach_profiles",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "teaching_longitude",
                table: "coach_profiles",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                table: "coach_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "idx_coach_profile_media_coach",
                table: "coach_profile_media",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_profile_media_type",
                table: "coach_profile_media",
                column: "media_type");

            migrationBuilder.CreateIndex(
                name: "idx_coach_teaching_locations_coach",
                table: "coach_teaching_locations",
                column: "coach_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_profile_media");

            migrationBuilder.DropTable(
                name: "coach_teaching_locations");

            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "achievements_summary",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "certifications_summary",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "cover_image_url",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "facebook_url",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "instagram_url",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "is_offline_available",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "is_online_available",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "specialties",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "teaching_address",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "teaching_city",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "teaching_district",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "teaching_latitude",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "teaching_longitude",
                table: "coach_profiles");

            migrationBuilder.DropColumn(
                name: "website_url",
                table: "coach_profiles");
        }
    }
}
