using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skindexer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "current_prices",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    price_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    game_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    volume = table.Column<int>(type: "integer", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_current_prices", x => new { x.variant_id, x.source, x.price_type });
                });

            migrationBuilder.CreateIndex(
                name: "ix_current_prices_game_id",
                table: "current_prices",
                column: "game_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_prices");
        }
    }
}
