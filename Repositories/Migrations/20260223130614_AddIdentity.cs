using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Account_role_id_fkey",
                schema: "public",
                table: "Account");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Account_role_id",
                schema: "public",
                table: "Account");

            migrationBuilder.RenameTable(
                name: "Account",
                schema: "public",
                newName: "Accounts",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "role_id",
                schema: "public",
                table: "Accounts",
                newName: "access_failed_count");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                schema: "public",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "Accounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "Accounts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                schema: "public",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                schema: "public",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                schema: "public",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                schema: "public",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                schema: "public",
                table: "Accounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_username",
                schema: "public",
                table: "Accounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "public",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "phone_number_confirmed",
                schema: "public",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "public",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "two_factor_enabled",
                schema: "public",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AccountClaims",
                columns: table => new
                {
                    claim_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountClaims", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK_AccountClaims_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLogins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_AccountLogins_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountTokens",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    token_name = table.Column<string>(type: "text", nullable: false),
                    token_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTokens", x => new { x.account_id, x.login_provider, x.token_name });
                    table.ForeignKey(
                        name: "FK_AccountTokens_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "AccountRoles",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRoles", x => new { x.account_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_AccountRoles_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountRoles_Roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    role_claim_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.role_claim_id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "public",
                table: "Accounts",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "public",
                table: "Accounts",
                column: "normalized_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountClaims_account_id",
                table: "AccountClaims",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogins_account_id",
                table: "AccountLogins",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_role_id",
                table: "AccountRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_role_id",
                table: "RoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountClaims");

            migrationBuilder.DropTable(
                name: "AccountLogins");

            migrationBuilder.DropTable(
                name: "AccountRoles");

            migrationBuilder.DropTable(
                name: "AccountTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "normalized_username",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "phone_number_confirmed",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled",
                schema: "public",
                table: "Accounts");

            migrationBuilder.RenameTable(
                name: "Accounts",
                schema: "public",
                newName: "Account",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                schema: "public",
                table: "Account",
                newName: "role_id");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                schema: "public",
                table: "Account",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "Account",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "Account",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "public",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Role_pkey", x => x.role_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_role_id",
                schema: "public",
                table: "Account",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "Role_role_name_key",
                schema: "public",
                table: "Role",
                column: "role_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "Account_role_id_fkey",
                schema: "public",
                table: "Account",
                column: "role_id",
                principalSchema: "public",
                principalTable: "Role",
                principalColumn: "role_id");
        }
    }
}
