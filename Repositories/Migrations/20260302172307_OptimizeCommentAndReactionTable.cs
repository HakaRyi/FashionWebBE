using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeCommentAndReactionTable : Migration
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

            migrationBuilder.DropForeignKey(
                name: "Reaction_account_id_fkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropForeignKey(
                name: "Reaction_post_id_fkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropPrimaryKey(
                name: "Reaction_pkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropPrimaryKey(
                name: "Comment_pkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "reaction_type",
                schema: "public",
                table: "Reaction");

            migrationBuilder.RenameIndex(
                name: "IX_Reaction_post_id",
                schema: "public",
                table: "Reaction",
                newName: "ix_reaction_post_id");

            migrationBuilder.RenameIndex(
                name: "IX_Reaction_account_id",
                schema: "public",
                table: "Reaction",
                newName: "ix_reaction_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_post_id",
                schema: "public",
                table: "Comment",
                newName: "ix_comment_post_id");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_account_id",
                schema: "public",
                table: "Comment",
                newName: "ix_comment_account_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Reaction",
                type: "timestamp without time zone",
                nullable: true,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "public",
                table: "Comment",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "reaction_pkey",
                schema: "public",
                table: "Reaction",
                column: "reaction_id");

            migrationBuilder.AddPrimaryKey(
                name: "comment_pkey",
                schema: "public",
                table: "Comment",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ux_reaction_account_post",
                schema: "public",
                table: "Reaction",
                columns: new[] { "account_id", "post_id" },
                unique: true);

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
                name: "comment_post_id_fkey",
                schema: "public",
                table: "Comment",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "reaction_account_id_fkey",
                schema: "public",
                table: "Reaction",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "reaction_post_id_fkey",
                schema: "public",
                table: "Reaction",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id",
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
                name: "comment_post_id_fkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "reaction_account_id_fkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropForeignKey(
                name: "reaction_post_id_fkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropPrimaryKey(
                name: "reaction_pkey",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropIndex(
                name: "ux_reaction_account_post",
                schema: "public",
                table: "Reaction");

            migrationBuilder.DropPrimaryKey(
                name: "comment_pkey",
                schema: "public",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "public",
                table: "Comment");

            migrationBuilder.RenameIndex(
                name: "ix_reaction_post_id",
                schema: "public",
                table: "Reaction",
                newName: "IX_Reaction_post_id");

            migrationBuilder.RenameIndex(
                name: "ix_reaction_account_id",
                schema: "public",
                table: "Reaction",
                newName: "IX_Reaction_account_id");

            migrationBuilder.RenameIndex(
                name: "ix_comment_post_id",
                schema: "public",
                table: "Comment",
                newName: "IX_Comment_post_id");

            migrationBuilder.RenameIndex(
                name: "ix_comment_account_id",
                schema: "public",
                table: "Comment",
                newName: "IX_Comment_account_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Reaction",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW()");

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
                name: "Reaction_pkey",
                schema: "public",
                table: "Reaction",
                column: "reaction_id");

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

            migrationBuilder.AddForeignKey(
                name: "Reaction_account_id_fkey",
                schema: "public",
                table: "Reaction",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Reaction_post_id_fkey",
                schema: "public",
                table: "Reaction",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id");
        }
    }
}
