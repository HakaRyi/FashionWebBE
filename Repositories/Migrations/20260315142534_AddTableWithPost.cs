using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTableWithPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Comment_account_id_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "Comment_post_id_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropPrimaryKey(
                name: "Comment_pkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "reaction_type",
                schema: "public",
                table: "Reaction");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_account_id",
                schema: "public",
                table: "Comment",
                newName: "ix_comment_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_PostId",
                schema: "public",
                table: "Comment",
                newName: "ix_comment_post_id");

            migrationBuilder.AddColumn<int>(
                name: "comment_count",
                schema: "public",
                table: "Post",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "event_id",
                schema: "public",
                table: "Images",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Comment",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "public",
                table: "Comment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "like_count",
                schema: "public",
                table: "Comment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "parent_comment_id",
                schema: "public",
                table: "Comment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "comment_pkey",
                schema: "public",
                table: "Comment",
                column: "comment_id");

            migrationBuilder.CreateTable(
                name: "CommentReaction",
                schema: "public",
                columns: table => new
                {
                    comment_reaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comment_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("comment_reaction_pkey", x => x.comment_reaction_id);
                    table.ForeignKey(
                        name: "comment_reaction_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "comment_reaction_comment_id_fkey",
                        column: x => x.comment_id,
                        principalSchema: "public",
                        principalTable: "Comment",
                        principalColumn: "comment_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Images_event_id",
                schema: "public",
                table: "Images",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_parent_comment_id",
                schema: "public",
                table: "Comment",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_account_id",
                schema: "public",
                table: "CommentReaction",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_comment_id",
                schema: "public",
                table: "CommentReaction",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ux_comment_reaction_account_comment",
                schema: "public",
                table: "CommentReaction",
                columns: new[] { "account_id", "comment_id" },
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "comment_account_id_fkey",
                schema: "public",
                table: "Comment",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "comment_parent_comment_fkey",
                schema: "public",
                table: "Comment",
                column: "parent_comment_id",
                principalSchema: "public",
                principalTable: "Comment",
                principalColumn: "comment_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "comment_post_id_fkey",
                schema: "public",
                table: "Comment",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Events_event_id",
                schema: "public",
                table: "Images",
                column: "event_id",
                principalSchema: "public",
                principalTable: "Events",
                principalColumn: "event_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "comment_account_id_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "comment_parent_comment_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "comment_post_id_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Images_Events_event_id",
                schema: "public",
                table: "Images");

            migrationBuilder.DropTable(
                name: "CommentReaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PostSaves",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Images_event_id",
                schema: "public",
                table: "Images");

            migrationBuilder.DropPrimaryKey(
                name: "comment_pkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "ix_comment_parent_comment_id",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "comment_count",
                schema: "public",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "event_id",
                schema: "public",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "like_count",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "parent_comment_id",
                schema: "public",
                table: "Comment");

            migrationBuilder.RenameIndex(
                name: "ix_comment_account_id",
                schema: "public",
                table: "Comment",
                newName: "IX_Comment_account_id");

            migrationBuilder.RenameIndex(
                name: "ix_comment_post_id",
                schema: "public",
                table: "Comment",
                newName: "IX_Comment_PostId");

            migrationBuilder.AddColumn<string>(
                name: "reaction_type",
                schema: "public",
                table: "Reaction",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Comment",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "public",
                table: "Comment",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddPrimaryKey(
                name: "Comment_pkey",
                schema: "public",
                table: "Comment",
                column: "comment_id");

            migrationBuilder.AddForeignKey(
                name: "Comment_account_id_fkey",
                schema: "public",
                table: "Comment",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Comment_post_id_fkey",
                schema: "public",
                table: "Comment",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id");
        }
    }
}
