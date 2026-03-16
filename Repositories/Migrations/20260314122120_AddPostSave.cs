using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostSaves",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSaves", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostSaves_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostSaves_Post_post_id",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_postsaves_account_post",
                schema: "public",
                table: "PostSaves",
                columns: new[] { "account_id", "post_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostSaves_post_id",
                schema: "public",
                table: "PostSaves",
                column: "post_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostSaves",
                schema: "public");
        }
    }
}
