using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSessionActiveSlotUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_training_sessions_availability_slot_id",
                table: "training_sessions");

            migrationBuilder.CreateIndex(
                name: "uq_training_sessions_active_slot",
                table: "training_sessions",
                column: "availability_slot_id",
                unique: true,
                filter: "availability_slot_id IS NOT NULL AND status IN ('requested', 'scheduled', 'completed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_training_sessions_active_slot",
                table: "training_sessions");

            migrationBuilder.CreateIndex(
                name: "IX_training_sessions_availability_slot_id",
                table: "training_sessions",
                column: "availability_slot_id");
        }
    }
}
