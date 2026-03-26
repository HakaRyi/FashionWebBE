using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Item_wardrobe_id_fkey",
                schema: "public",
                table: "Item");

            migrationBuilder.DropTable(
                name: "Item_Category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "placement",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "style_score",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "texture",
                schema: "public",
                table: "Item");

            migrationBuilder.RenameColumn(
                name: "fabric",
                schema: "public",
                table: "Item",
                newName: "sub_color");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Outfit",
                type: "timestamp without time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_at",
                schema: "public",
                table: "Item",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "style",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.Sql("ALTER TABLE public.\"Item\" ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "Item",
                type: "integer",
                nullable: true,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "pattern",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "item_name",
                schema: "public",
                table: "Item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Item",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "category",
                schema: "public",
                table: "Item",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "fit",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                schema: "public",
                table: "Item",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                schema: "public",
                table: "Item",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "item_type",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "length",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "material",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "neckline",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sleeve_length",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_category",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutfitItem",
                schema: "public",
                columns: table => new
                {
                    outfit_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    slot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("OutfitItem_pkey", x => new { x.outfit_id, x.item_id });
                    table.ForeignKey(
                        name: "OutfitItem_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "OutfitItem_outfit_id_fkey",
                        column: x => x.outfit_id,
                        principalSchema: "public",
                        principalTable: "Outfit",
                        principalColumn: "outfit_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Item_item_embedding",
                schema: "public",
                table: "Item",
                column: "item_embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitItem_item_id",
                schema: "public",
                table: "OutfitItem",
                column: "item_id");

            migrationBuilder.AddForeignKey(
                name: "Item_wardrobe_id_fkey",
                schema: "public",
                table: "Item",
                column: "wardrobe_id",
                principalSchema: "public",
                principalTable: "Wardrobe",
                principalColumn: "wardrobe_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Item_wardrobe_id_fkey",
                schema: "public",
                table: "Item");

            migrationBuilder.DropTable(
                name: "OutfitItem",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Item_item_embedding",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "fit",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "gender",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "is_public",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "item_type",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "length",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "material",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "neckline",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "sleeve_length",
                schema: "public",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "sub_category",
                schema: "public",
                table: "Item");

            migrationBuilder.RenameColumn(
                name: "sub_color",
                schema: "public",
                table: "Item",
                newName: "fabric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Outfit",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_at",
                schema: "public",
                table: "Item",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "style",
                schema: "public",
                table: "Item",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "Item",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "pattern",
                schema: "public",
                table: "Item",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "item_name",
                schema: "public",
                table: "Item",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Item",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "public",
                table: "Item",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "placement",
                schema: "public",
                table: "Item",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "style_score",
                schema: "public",
                table: "Item",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "texture",
                schema: "public",
                table: "Item",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "public",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Category_pkey", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "Item_Category",
                schema: "public",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Item_Category_pkey", x => new { x.item_id, x.category_id });
                    table.ForeignKey(
                        name: "Item_Category_category_id_fkey",
                        column: x => x.category_id,
                        principalSchema: "public",
                        principalTable: "Category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "Item_Category_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Item_Category_category_id",
                schema: "public",
                table: "Item_Category",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "Item_wardrobe_id_fkey",
                schema: "public",
                table: "Item",
                column: "wardrobe_id",
                principalSchema: "public",
                principalTable: "Wardrobe",
                principalColumn: "wardrobe_id");
        }
    }
}
