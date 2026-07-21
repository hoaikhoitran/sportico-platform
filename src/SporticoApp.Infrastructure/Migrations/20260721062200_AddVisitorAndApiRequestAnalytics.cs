using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorAndApiRequestAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "visitor_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    visitor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    browser = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    os = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_new_visitor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    page_view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    api_request_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("visitor_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_visitor_sessions_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "One browsing session by an anonymous or logged-in visitor");

            migrationBuilder.CreateTable(
                name: "api_request_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    visitor_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_request_metrics_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_request_metrics_session",
                        column: x => x.visitor_session_id,
                        principalTable: "visitor_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_request_metrics_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "One tracked backend API request within a visitor session");

            migrationBuilder.CreateTable(
                name: "page_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    visitor_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    referrer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("page_views_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_views_session",
                        column: x => x.visitor_session_id,
                        principalTable: "visitor_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_page_views_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Frontend navigation event submitted by the client, within a visitor session");

            migrationBuilder.CreateIndex(
                name: "idx_api_request_metrics_path",
                table: "api_request_metrics",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "idx_api_request_metrics_requested_at",
                table: "api_request_metrics",
                column: "requested_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_api_request_metrics_session",
                table: "api_request_metrics",
                column: "visitor_session_id");

            migrationBuilder.CreateIndex(
                name: "idx_api_request_metrics_user",
                table: "api_request_metrics",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_page_views_path",
                table: "page_views",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "idx_page_views_session",
                table: "page_views",
                column: "visitor_session_id");

            migrationBuilder.CreateIndex(
                name: "idx_page_views_user",
                table: "page_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_page_views_viewed_at",
                table: "page_views",
                column: "viewed_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_browser",
                table: "visitor_sessions",
                column: "browser");

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_country",
                table: "visitor_sessions",
                column: "country");

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_device",
                table: "visitor_sessions",
                column: "device");

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_first_seen",
                table: "visitor_sessions",
                column: "first_seen_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_last_seen",
                table: "visitor_sessions",
                column: "last_seen_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_user",
                table: "visitor_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_visitor_sessions_visitor_last_seen",
                table: "visitor_sessions",
                columns: new[] { "visitor_id", "last_seen_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_request_metrics");

            migrationBuilder.DropTable(
                name: "page_views");

            migrationBuilder.DropTable(
                name: "visitor_sessions");
        }
    }
}
