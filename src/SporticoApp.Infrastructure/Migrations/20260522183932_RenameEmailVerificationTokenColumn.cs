using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    public partial class RenameEmailVerificationTokenColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM information_schema.columns " +
                "WHERE table_name = 'users' AND column_name = 'EmailVerificationToken') THEN " +
                "ALTER TABLE users RENAME COLUMN \"EmailVerificationToken\" TO email_verification_token; " +
                "END IF; " +
                "END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM information_schema.columns " +
                "WHERE table_name = 'users' AND column_name = 'email_verification_token') THEN " +
                "ALTER TABLE users RENAME COLUMN email_verification_token TO \"EmailVerificationToken\"; " +
                "END IF; " +
                "END $$;");
        }
    }
}
