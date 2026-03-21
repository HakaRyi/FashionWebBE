using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "applied_fee",
                schema: "public",
                table: "Events",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.0m);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "public",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    setting_value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("SystemSettings_pkey", x => x.setting_key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "applied_fee",
                schema: "public",
                table: "Events");
        }
    }
}
