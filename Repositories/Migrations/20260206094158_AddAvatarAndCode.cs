using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarAndCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar",
                schema: "fashion_db",
                table: "Account",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "code_expires_at",
                schema: "fashion_db",
                table: "Account",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                schema: "fashion_db",
                table: "Account",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar",
                schema: "fashion_db",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "code_expires_at",
                schema: "fashion_db",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "verification_code",
                schema: "fashion_db",
                table: "Account");
        }
    }
}
