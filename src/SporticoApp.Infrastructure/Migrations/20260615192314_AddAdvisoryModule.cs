using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvisoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "advisory_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiator_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "learner | admin"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("advisory_conversations_pkey", x => x.id);
                    table.CheckConstraint("chk_advisory_conversations_initiator_role", "initiator_role IN ('learner','admin')");
                    table.ForeignKey(
                        name: "fk_advisory_conversations_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "AI advisory chatbot conversations started by a learner or admin");

            migrationBuilder.CreateTable(
                name: "advisory_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "user | assistant"),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("advisory_messages_pkey", x => x.id);
                    table.CheckConstraint("chk_advisory_messages_sender", "sender IN ('user','assistant')");
                    table.ForeignKey(
                        name: "fk_advisory_messages_conversation",
                        column: x => x.conversation_id,
                        principalTable: "advisory_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Turns within an advisory conversation");

            migrationBuilder.CreateIndex(
                name: "idx_advisory_conversations_user",
                table: "advisory_conversations",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_advisory_messages_conversation",
                table: "advisory_messages",
                columns: new[] { "conversation_id", "created_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advisory_messages");

            migrationBuilder.DropTable(
                name: "advisory_conversations");
        }
    }
}
