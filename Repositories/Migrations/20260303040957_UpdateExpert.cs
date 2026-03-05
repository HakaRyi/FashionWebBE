using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExpert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "experience_years",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "verified",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.RenameColumn(
                name: "bio",
                schema: "public",
                table: "Expert_File",
                newName: "identity_proof_url");

            migrationBuilder.AddColumn<string>(
                name: "style_aesthetic",
                schema: "public",
                table: "Expert_Profile",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Expert_File",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_url",
                schema: "public",
                table: "Expert_File",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "style_aesthetic",
                schema: "public",
                table: "Expert_Profile");

            migrationBuilder.DropColumn(
                name: "cv_url",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.RenameColumn(
                name: "identity_proof_url",
                schema: "public",
                table: "Expert_File",
                newName: "bio");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Expert_File",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "experience_years",
                schema: "public",
                table: "Expert_File",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "verified",
                schema: "public",
                table: "Expert_File",
                type: "boolean",
                nullable: true);
        }
    }
}
