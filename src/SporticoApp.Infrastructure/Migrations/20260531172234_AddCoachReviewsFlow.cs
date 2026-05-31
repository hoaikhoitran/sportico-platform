using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachReviewsFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moderation_reason",
                table: "reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "reviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'active'::character varying",
                comment: "active | hidden | deleted");

            migrationBuilder.AlterColumn<Guid>(
                name: "target_user_id",
                table: "reports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "action_taken",
                table: "reports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                comment: "none | review_hidden | review_deleted");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handled_at",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "handled_by_user_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_note",
                table: "reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                table: "reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'user'::character varying",
                comment: "user | review");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_booking",
                table: "reviews",
                column: "booking_id",
                filter: "(booking_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_reviews_coach_status_created",
                table: "reviews",
                columns: new[] { "coach_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_reports_target_entity",
                table: "reports",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_booking",
                table: "reviews",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reviews_booking",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "idx_reviews_booking",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "idx_reviews_coach_status_created",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "idx_reports_target_entity",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "moderation_reason",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "status",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "action_taken",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "description",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "handled_at",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "handled_by_user_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "resolution_note",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "target_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "target_type",
                table: "reports");

            migrationBuilder.AlterColumn<Guid>(
                name: "target_user_id",
                table: "reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
