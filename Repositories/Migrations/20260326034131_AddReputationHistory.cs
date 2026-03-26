using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddReputationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reputation_score",
                schema: "public",
                table: "Expert_Profile",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reputation_History",
                schema: "public",
                columns: table => new
                {
                    reputation_history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expert_profile_id = table.Column<int>(type: "integer", nullable: false),
                    point_change = table.Column<int>(type: "integer", nullable: false),
                    current_point = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Reputation_History_pkey", x => x.reputation_history_id);
                    table.ForeignKey(
                        name: "Reputation_History_expert_profile_id_fkey",
                        column: x => x.expert_profile_id,
                        principalSchema: "public",
                        principalTable: "Expert_Profile",
                        principalColumn: "expert_profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reputation_History_expert_profile_id",
                schema: "public",
                table: "Reputation_History",
                column: "expert_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reputation_History",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "reputation_score",
                schema: "public",
                table: "Expert_Profile");
        }
    }
}
