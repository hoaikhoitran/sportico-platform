using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    public partial class UpdatePaymentMethodConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'draft'::character varying",
                comment: "draft | pending | published | archived | rejected",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'draft'::character varying",
                oldComment: "draft | published | archived | rejected");

            migrationBuilder.Sql(
                "ALTER TABLE payments DROP CONSTRAINT IF EXISTS chk_payments_method;");

            migrationBuilder.Sql(
                "ALTER TABLE payments ADD CONSTRAINT chk_payments_method CHECK (method IN ('manual', 'payos'));");

            migrationBuilder.Sql(
                "ALTER TABLE payments DROP CONSTRAINT IF EXISTS chk_payments_status;");

            migrationBuilder.Sql(
                "ALTER TABLE payments ADD CONSTRAINT chk_payments_status CHECK (status IN ('pending', 'paid', 'failed', 'cancelled'));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE payments DROP CONSTRAINT IF EXISTS chk_payments_method;");

            migrationBuilder.Sql(
                "ALTER TABLE payments DROP CONSTRAINT IF EXISTS chk_payments_status;");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'draft'::character varying",
                comment: "draft | published | archived | rejected",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'draft'::character varying",
                oldComment: "draft | pending | published | archived | rejected");
        }
    }
}