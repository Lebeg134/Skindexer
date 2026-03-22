using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skindexer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionsAndGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GradeId",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_CollectionId",
                table: "items",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_items_GradeId",
                table: "items",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_collections_GameId_Slug",
                table: "collections",
                columns: new[] { "GameId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grades_GameId_Order",
                table: "grades",
                columns: new[] { "GameId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grades_GameId_Slug",
                table: "grades",
                columns: new[] { "GameId", "Slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_items_collections_CollectionId",
                table: "items",
                column: "CollectionId",
                principalTable: "collections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_items_grades_GradeId",
                table: "items",
                column: "GradeId",
                principalTable: "grades",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_collections_CollectionId",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_grades_GradeId",
                table: "items");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropIndex(
                name: "IX_items_CollectionId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_GradeId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "items");
        }
    }
}
