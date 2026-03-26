using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddItemIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Item_wardrobe_id",
                schema: "public",
                table: "Item",
                newName: "IX_Item_WardrobeId");

            migrationBuilder.CreateTable(
                name: "SavedItem",
                schema: "public",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("SavedItem_pkey", x => new { x.account_id, x.item_id });
                    table.ForeignKey(
                        name: "SavedItem_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "SavedItem_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Item_Category",
                schema: "public",
                table: "Item",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_Item_Gender",
                schema: "public",
                table: "Item",
                column: "gender");

            migrationBuilder.CreateIndex(
                name: "IX_Item_IsPublic",
                schema: "public",
                table: "Item",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "IX_Item_IsPublic_Category",
                schema: "public",
                table: "Item",
                columns: new[] { "is_public", "category" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedItem_item_id",
                schema: "public",
                table: "SavedItem",
                column: "item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedItem",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Item_Category",
                schema: "public",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_Gender",
                schema: "public",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_IsPublic",
                schema: "public",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_IsPublic_Category",
                schema: "public",
                table: "Item");

            migrationBuilder.RenameIndex(
                name: "IX_Item_WardrobeId",
                schema: "public",
                table: "Item",
                newName: "IX_Item_wardrobe_id");
        }
    }
}
