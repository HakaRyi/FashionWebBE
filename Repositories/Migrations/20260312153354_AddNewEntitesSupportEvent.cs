using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntitesSupportEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scoreboard_Post_PostId",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scoreboard",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "Like",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "Score",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "Share",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "public",
                table: "Scoreboard",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "PostId",
                schema: "public",
                table: "Scoreboard",
                newName: "post_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "public",
                table: "Scoreboard",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ScoreboardId",
                schema: "public",
                table: "Scoreboard",
                newName: "scoreboard_id");

            migrationBuilder.RenameIndex(
                name: "IX_Scoreboard_PostId",
                schema: "public",
                table: "Scoreboard",
                newName: "IX_Scoreboard_post_id");

            migrationBuilder.RenameColumn(
                name: "tittle",
                schema: "public",
                table: "Post",
                newName: "title");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "Scoreboard",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Scoreboard",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<double>(
                name: "community_score",
                schema: "public",
                table: "Scoreboard",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "expert_reason",
                schema: "public",
                table: "Scoreboard",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "expert_score",
                schema: "public",
                table: "Scoreboard",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "final_like_count",
                schema: "public",
                table: "Scoreboard",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "final_score",
                schema: "public",
                table: "Scoreboard",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "final_share_count",
                schema: "public",
                table: "Scoreboard",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "ALTER TABLE public.\"PrizeEvent\" ALTER COLUMN ranked TYPE integer USING ranked::integer;"
            );

            migrationBuilder.AlterColumn<int>(
                name: "ranked",
                schema: "public",
                table: "PrizeEvent",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "expert_feedback",
                schema: "public",
                table: "EventWinner",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_rank",
                schema: "public",
                table: "EventWinner",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "winning_score",
                schema: "public",
                table: "EventWinner",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Events",
                type: "timestamp without time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "expert_weight",
                schema: "public",
                table: "Events",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "point_per_like",
                schema: "public",
                table: "Events",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "point_per_share",
                schema: "public",
                table: "Events",
                type: "double precision",
                nullable: false,
                defaultValue: 2.0);

            migrationBuilder.AddColumn<double>(
                name: "user_weight",
                schema: "public",
                table: "Events",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddPrimaryKey(
                name: "Scoreboard_pkey",
                schema: "public",
                table: "Scoreboard",
                column: "scoreboard_id");

            migrationBuilder.CreateTable(
                name: "EventExpert",
                schema: "public",
                columns: table => new
                {
                    event_expert_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    expert_id = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("EventExpert_pkey", x => x.event_expert_id);
                    table.ForeignKey(
                        name: "EventExpert_event_id_fkey",
                        column: x => x.event_id,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "EventExpert_expert_id_fkey",
                        column: x => x.expert_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpertRating",
                schema: "public",
                columns: table => new
                {
                    expert_rating_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    expert_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ExpertRating_pkey", x => x.expert_rating_id);
                    table.ForeignKey(
                        name: "ExpertRating_expert_id_fkey",
                        column: x => x.expert_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ExpertRating_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventExpert_Event_Expert",
                schema: "public",
                table: "EventExpert",
                columns: new[] { "event_id", "expert_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventExpert_expert_id",
                schema: "public",
                table: "EventExpert",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertRating_expert_id",
                schema: "public",
                table: "ExpertRating",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertRating_Post_Expert",
                schema: "public",
                table: "ExpertRating",
                columns: new[] { "post_id", "expert_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "Scoreboard_post_id_fkey",
                schema: "public",
                table: "Scoreboard",
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
                name: "Scoreboard_post_id_fkey",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropTable(
                name: "EventExpert",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExpertRating",
                schema: "public");

            migrationBuilder.DropPrimaryKey(
                name: "Scoreboard_pkey",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "community_score",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "expert_reason",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "expert_score",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "final_like_count",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "final_score",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "final_share_count",
                schema: "public",
                table: "Scoreboard");

            migrationBuilder.DropColumn(
                name: "expert_feedback",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropColumn(
                name: "final_rank",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropColumn(
                name: "winning_score",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropColumn(
                name: "expert_weight",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "point_per_like",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "point_per_share",
                schema: "public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "user_weight",
                schema: "public",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "public",
                table: "Scoreboard",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "post_id",
                schema: "public",
                table: "Scoreboard",
                newName: "PostId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "public",
                table: "Scoreboard",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "scoreboard_id",
                schema: "public",
                table: "Scoreboard",
                newName: "ScoreboardId");

            migrationBuilder.RenameIndex(
                name: "IX_Scoreboard_post_id",
                schema: "public",
                table: "Scoreboard",
                newName: "IX_Scoreboard_PostId");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "public",
                table: "Post",
                newName: "tittle");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "Scoreboard",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "Scoreboard",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "Like",
                schema: "public",
                table: "Scoreboard",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Score",
                schema: "public",
                table: "Scoreboard",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Share",
                schema: "public",
                table: "Scoreboard",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ranked",
                schema: "public",
                table: "PrizeEvent",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Events",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scoreboard",
                schema: "public",
                table: "Scoreboard",
                column: "ScoreboardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scoreboard_Post_PostId",
                schema: "public",
                table: "Scoreboard",
                column: "PostId",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "post_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
