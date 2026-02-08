using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Follow",
                schema: "fashion_db",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    follower_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Follow_pkey", x => new { x.user_id, x.follower_id });

                    table.ForeignKey(
                        name: "Follow_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "fashion_db",
                        principalTable: "Account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "Follow_follower_id_fkey",
                        column: x => x.follower_id,
                        principalSchema: "fashion_db",
                        principalTable: "Account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Tạo Index cho nhanh
            migrationBuilder.CreateIndex(
                name: "IX_Follow_follower_id",
                schema: "fashion_db",
                table: "Follow",
                column: "follower_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Follow",
                schema: "fashion_db");
        }
    }
}
