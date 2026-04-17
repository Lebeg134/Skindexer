using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skindexer.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rarity_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rarity_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rarities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: true),
                    rarity_group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rarities", x => x.id);
                    table.ForeignKey(
                        name: "fk_rarities_rarity_groups_rarity_group_id",
                        column: x => x.rarity_group_id,
                        principalTable: "rarity_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    image_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    is_tradeable = table.Column<bool>(type: "boolean", nullable: false),
                    is_marketable = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    added_to_game_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rarity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_items_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_items_rarities_rarity_id",
                        column: x => x.rarity_id,
                        principalTable: "rarities",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_variants_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    price_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    volume = table.Column<int>(type: "integer", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_snapshots_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_collections_game_id_slug",
                table: "collections",
                columns: new[] { "game_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_collection_id",
                table: "items",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_game_id_slug",
                table: "items",
                columns: new[] { "game_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_rarity_id",
                table: "items",
                column: "rarity_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_snapshots_variant_id_source_price_type_recorded_at",
                table: "price_snapshots",
                columns: new[] { "variant_id", "source", "price_type", "recorded_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rarities_rarity_group_id_slug",
                table: "rarities",
                columns: new[] { "rarity_group_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rarity_groups_game_id_slug",
                table: "rarity_groups",
                columns: new[] { "game_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rarity_groups_game_id_type",
                table: "rarity_groups",
                columns: new[] { "game_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_game_id_slug",
                table: "variants",
                columns: new[] { "game_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_item_id",
                table: "variants",
                column: "item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_snapshots");

            migrationBuilder.DropTable(
                name: "variants");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropTable(
                name: "rarities");

            migrationBuilder.DropTable(
                name: "rarity_groups");
        }
    }
}
