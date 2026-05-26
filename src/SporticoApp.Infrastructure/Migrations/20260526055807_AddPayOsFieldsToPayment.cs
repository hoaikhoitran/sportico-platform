using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsFieldsToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checkout_url",
                table: "payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expired_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "order_code",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_link_id",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_payments_order_code",
                table: "payments",
                column: "order_code",
                unique: true,
                filter: "(order_code IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_payments_order_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "checkout_url",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "expired_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "order_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "payment_link_id",
                table: "payments");
        }
    }
}
