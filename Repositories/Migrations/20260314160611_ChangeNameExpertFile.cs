using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameExpertFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Expert_File_expert_profile_id_fkey",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropIndex(
                name: "Expert_File_expert_profile_id_key",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "rating_avg",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.AddColumn<double>(
                name: "rating_avg",
                schema: "public",
                table: "Expert_Profile",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "Expert_File",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "expertise_field",
                schema: "public",
                table: "Expert_File",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processed_at",
                schema: "public",
                table: "Expert_File",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "public",
                table: "Expert_File",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "style_aesthetic",
                schema: "public",
                table: "Expert_File",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "years_of_experience",
                schema: "public",
                table: "Expert_File",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "Expert_File_expert_profile_id_idx",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id");

            migrationBuilder.AddForeignKey(
                name: "Expert_File_expert_profile_id_fkey",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id",
                principalSchema: "public",
                principalTable: "Expert_Profile",
                principalColumn: "expert_profile_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Expert_File_expert_profile_id_fkey",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropIndex(
                name: "Expert_File_expert_profile_id_idx",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "rating_avg",
                schema: "public",
                table: "Expert_Profile");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "expertise_field",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "processed_at",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "reason",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "style_aesthetic",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.DropColumn(
                name: "years_of_experience",
                schema: "public",
                table: "Expert_File");

            migrationBuilder.AddColumn<double>(
                name: "rating_avg",
                schema: "public",
                table: "Expert_File",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "Expert_File_expert_profile_id_key",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "Expert_File_expert_profile_id_fkey",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id",
                principalSchema: "public",
                principalTable: "Expert_Profile",
                principalColumn: "expert_profile_id");
        }
    }
}
