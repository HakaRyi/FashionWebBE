using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePost01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "User_Report_account_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropForeignKey(
                name: "User_Report_post_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropForeignKey(
                name: "User_Report_report_type_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropIndex(
                name: "IX_User_Report_post_id",
                schema: "public",
                table: "User_Report");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "User_Report",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "admin_note",
                schema: "public",
                table: "User_Report",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                schema: "public",
                table: "User_Report",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by",
                schema: "public",
                table: "User_Report",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "public",
                table: "User_Report",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "Post",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                schema: "public",
                table: "Post",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Visible");

            migrationBuilder.CreateIndex(
                name: "User_Report_post_id_account_id_key",
                schema: "public",
                table: "User_Report",
                columns: new[] { "post_id", "account_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "User_Report_account_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "User_Report_post_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "User_Report_report_type_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "report_type_id",
                principalSchema: "public",
                principalTable: "Report_Type",
                principalColumn: "report_type_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "User_Report_account_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropForeignKey(
                name: "User_Report_post_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropForeignKey(
                name: "User_Report_report_type_id_fkey",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropIndex(
                name: "User_Report_post_id_account_id_key",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropColumn(
                name: "admin_note",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "public",
                table: "User_Report");

            migrationBuilder.DropColumn(
                name: "visibility",
                schema: "public",
                table: "Post");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "User_Report",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "Post",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldDefaultValue: "Draft");

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_post_id",
                schema: "public",
                table: "User_Report",
                column: "post_id");

            migrationBuilder.AddForeignKey(
                name: "User_Report_account_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "User_Report_post_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "post_id",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id");

            migrationBuilder.AddForeignKey(
                name: "User_Report_report_type_id_fkey",
                schema: "public",
                table: "User_Report",
                column: "report_type_id",
                principalSchema: "public",
                principalTable: "Report_Type",
                principalColumn: "report_type_id");
        }
    }
}
