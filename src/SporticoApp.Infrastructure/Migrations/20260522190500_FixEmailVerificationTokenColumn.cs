using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SporticoApp.Infrastructure.Persistence;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260522190500_FixEmailVerificationTokenColumn")]
    public partial class FixEmailVerificationTokenColumn : Migration
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
