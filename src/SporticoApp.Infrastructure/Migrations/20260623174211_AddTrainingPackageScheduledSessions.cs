using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPackageScheduledSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "training_package_session_slot_id",
                table: "training_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "training_packages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "training_packages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "training_package_session_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    meeting_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Maximum learners that can buy a seat on this session"),
                    booked_participants = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'open'::character varying", comment: "open | full | cancelled"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_package_session_slots_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_package_session_slots_package",
                        column: x => x.training_package_id,
                        principalTable: "training_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fixed schedule of sessions defined for a training package");

            migrationBuilder.CreateIndex(
                name: "ix_training_sessions_package_session_slot_id",
                table: "training_sessions",
                column: "training_package_session_slot_id");

            migrationBuilder.CreateIndex(
                name: "uq_training_sessions_booking_package_slot",
                table: "training_sessions",
                columns: new[] { "booking_id", "training_package_session_slot_id" },
                unique: true,
                filter: "training_package_session_slot_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_training_package_session_slots_package",
                table: "training_package_session_slots",
                column: "training_package_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_package_session_slots_status",
                table: "training_package_session_slots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_training_package_session_slots_package_number",
                table: "training_package_session_slots",
                columns: new[] { "training_package_id", "session_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_training_sessions_package_session_slot",
                table: "training_sessions",
                column: "training_package_session_slot_id",
                principalTable: "training_package_session_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_training_sessions_package_session_slot",
                table: "training_sessions");

            migrationBuilder.DropTable(
                name: "training_package_session_slots");

            migrationBuilder.DropIndex(
                name: "ix_training_sessions_package_session_slot_id",
                table: "training_sessions");

            migrationBuilder.DropIndex(
                name: "uq_training_sessions_booking_package_slot",
                table: "training_sessions");

            migrationBuilder.DropColumn(
                name: "training_package_session_slot_id",
                table: "training_sessions");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "training_packages");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "training_packages");
        }
    }
}
