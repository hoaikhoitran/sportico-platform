using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_training_sessions_active_slot",
                table: "training_sessions");

            migrationBuilder.AddColumn<int>(
                name: "max_participants",
                table: "coach_availability_slots",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "Maximum learners that can book this slot");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "coach_availability_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_training_sessions_availability_slot_id",
                table: "training_sessions",
                column: "availability_slot_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_training_sessions_availability_slot_id",
                table: "training_sessions");

            migrationBuilder.DropColumn(
                name: "max_participants",
                table: "coach_availability_slots");

            migrationBuilder.DropColumn(
                name: "version",
                table: "coach_availability_slots");

            migrationBuilder.CreateIndex(
                name: "uq_training_sessions_active_slot",
                table: "training_sessions",
                column: "availability_slot_id",
                unique: true,
                filter: "availability_slot_id IS NOT NULL AND status IN ('requested', 'scheduled', 'completed')");
        }
    }
}
