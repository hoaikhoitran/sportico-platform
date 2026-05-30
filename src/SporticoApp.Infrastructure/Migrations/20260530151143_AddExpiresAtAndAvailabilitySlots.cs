using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiresAtAndAvailabilitySlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "withdrawal_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pay_os_payout_id",
                table: "withdrawal_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pay_os_payout_status",
                table: "withdrawal_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pay_os_raw_response",
                table: "withdrawal_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pay_os_reference_id",
                table: "withdrawal_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_at",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "availability_slot_id",
                table: "training_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "coach_availability_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'available'::character varying", comment: "available | booked | cancelled | expired"),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    meeting_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
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

            migrationBuilder.CreateIndex(
                name: "IX_training_sessions_availability_slot_id",
                table: "training_sessions",
                column: "availability_slot_id");

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

            migrationBuilder.AddForeignKey(
                name: "fk_training_sessions_availability_slot",
                table: "training_sessions",
                column: "availability_slot_id",
                principalTable: "coach_availability_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_training_sessions_availability_slot",
                table: "training_sessions");

            migrationBuilder.DropTable(
                name: "coach_availability_slots");

            migrationBuilder.DropIndex(
                name: "IX_training_sessions_availability_slot_id",
                table: "training_sessions");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "pay_os_payout_id",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "pay_os_payout_status",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "pay_os_raw_response",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "pay_os_reference_id",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "processing_at",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "availability_slot_id",
                table: "training_sessions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "bookings");
        }
    }
}
