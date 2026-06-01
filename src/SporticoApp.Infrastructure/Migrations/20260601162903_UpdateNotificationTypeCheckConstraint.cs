using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationTypeCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "message | review | follow | payment | package | post | system | report | booking | training_package | training_session | training_plan | wallet",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "message | review | follow | payment | package | system | report");

            // The configured PostgreSQL database already has the old chk_notifications_type
            // (allowing only message|review|follow|payment|package|system|report). Drop the
            // existing definition by both known names first so AddCheckConstraint below does not
            // fail with "constraint already exists". DROP IF EXISTS is a no-op on a fresh DB.
            migrationBuilder.Sql("ALTER TABLE notifications DROP CONSTRAINT IF EXISTS chk_notifications_type;");
            migrationBuilder.Sql("ALTER TABLE notifications DROP CONSTRAINT IF EXISTS notifications_type_check;");

            migrationBuilder.AddCheckConstraint(
                name: "chk_notifications_type",
                table: "notifications",
                sql: "type IN ('message','review','follow','payment','package','post','system','report','booking','training_package','training_session','training_plan','wallet')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous (narrower) constraint definition.
            migrationBuilder.Sql("ALTER TABLE notifications DROP CONSTRAINT IF EXISTS chk_notifications_type;");

            migrationBuilder.AddCheckConstraint(
                name: "chk_notifications_type",
                table: "notifications",
                sql: "type IN ('message','review','follow','payment','package','system','report')");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "message | review | follow | payment | package | system | report",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "message | review | follow | payment | package | post | system | report | booking | training_package | training_session | training_plan | wallet");
        }
    }
}
