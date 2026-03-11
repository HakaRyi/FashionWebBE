using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddItemEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "item_embedding",
                schema: "public",
                table: "Item",
                type: "vector(768)",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item_embedding",
                schema: "public",
                table: "Item");
        }
    }
}
