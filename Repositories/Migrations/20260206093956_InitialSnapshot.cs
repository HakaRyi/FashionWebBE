using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comment",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Expert_File",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Follow",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "GroupUser",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Item_Category",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "MessReaction",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Outfit",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Photos",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "PinnedMessage",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Post_Vector",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Reaction",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Transaction",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "User_Profile_Vector",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "User_Report",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Expert_Profile",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Item",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Post",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Report_Type",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Wardrobe",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Group",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Package",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Account",
                schema: "fashion_db");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "fashion_db");
        }
    }
}
