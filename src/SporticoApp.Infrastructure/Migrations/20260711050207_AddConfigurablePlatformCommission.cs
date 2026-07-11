using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurablePlatformCommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, comment: "Fractional commission rate for NEW bookings: 0.0000 (0%) .. 1.0000 (100%)"),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("platform_settings_pkey", x => x.id);
                    table.CheckConstraint("chk_platform_settings_commission_rate", "commission_rate >= 0 AND commission_rate <= 1");
                },
                comment: "Singleton platform-wide settings (editable platform commission)");

            migrationBuilder.InsertData(
                table: "platform_settings",
                columns: new[] { "id", "commission_rate", "created_at", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), 0m, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_settings");
        }
    }
}
