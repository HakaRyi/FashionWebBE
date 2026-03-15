using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class EditAllRelatedTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Package_account_id_fkey",
                schema: "public",
                table: "Package");

            migrationBuilder.DropForeignKey(
                name: "Payment_package_id_fkey",
                schema: "public",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_PrizeEvent_Events_event_id",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropForeignKey(
                name: "RefreshToken_account_id_fkey",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropForeignKey(
                name: "FK_TheEventWinner_Accounts_AccountId",
                schema: "public",
                table: "TheEventWinner");

            migrationBuilder.DropForeignKey(
                name: "FK_TheEventWinner_PrizeEvent_PrizeEventId",
                schema: "public",
                table: "TheEventWinner");

            migrationBuilder.DropForeignKey(
                name: "Transaction_account_id_fkey",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "Transaction_payment_id_fkey",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "RefreshToken_account_id_key",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "RefreshToken_device_info_key",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "RefreshToken_ip_address_key",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PrizeEvent",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TheEventWinner",
                schema: "public",
                table: "TheEventWinner");

            migrationBuilder.DropIndex(
                name: "IX_TheEventWinner_AccountId",
                schema: "public",
                table: "TheEventWinner");

            migrationBuilder.DropIndex(
                name: "IX_TheEventWinner_PrizeEventId",
                schema: "public",
                table: "TheEventWinner");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "create_at",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "reward_coin",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "coin_amount",
                schema: "public",
                table: "Package");

            migrationBuilder.RenameTable(
                name: "TheEventWinner",
                schema: "public",
                newName: "EventWinner",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "account_id",
                schema: "public",
                table: "Transaction",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "amount_coin",
                schema: "public",
                table: "Transaction",
                newName: "wallet_id");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_account_id",
                schema: "public",
                table: "Transaction",
                newName: "IX_Transaction_AccountId");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "public",
                table: "PrizeEvent",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "price_vnd",
                schema: "public",
                table: "Package",
                newName: "duration_days");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "public",
                table: "EventWinner",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "public",
                table: "EventWinner",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PrizeEventId",
                schema: "public",
                table: "EventWinner",
                newName: "prize_event_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "public",
                table: "EventWinner",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                schema: "public",
                table: "EventWinner",
                newName: "account_id");

            migrationBuilder.RenameColumn(
                name: "EventWinnerId",
                schema: "public",
                table: "EventWinner",
                newName: "event_winner_id");

            migrationBuilder.AlterColumn<string>(
                name: "reference_type",
                schema: "public",
                table: "Transaction",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "payment_id",
                schema: "public",
                table: "Transaction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Transaction",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "balance_after",
                schema: "public",
                table: "Transaction",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                schema: "public",
                table: "Transaction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "public",
                table: "Transaction",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_before",
                schema: "public",
                table: "Transaction",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "PrizeEvent",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "escrow_session_id",
                schema: "public",
                table: "PrizeEvent",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "reward_amount",
                schema: "public",
                table: "PrizeEvent",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Package",
                type: "timestamp without time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "public",
                table: "Package",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                schema: "public",
                table: "Package",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "public",
                table: "EventWinner",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "EventWinner",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PrizeEvent_pkey",
                schema: "public",
                table: "PrizeEvent",
                column: "prize_event_id");

            migrationBuilder.AddPrimaryKey(
                name: "EventWinner_pkey",
                schema: "public",
                table: "EventWinner",
                column: "event_winner_id");

            migrationBuilder.CreateTable(
                name: "AccountSubscription",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    package_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSubscription", x => x.id);
                    table.ForeignKey(
                        name: "FK_AccountSubscription_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountSubscription_Package_package_id",
                        column: x => x.package_id,
                        principalSchema: "public",
                        principalTable: "Package",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feature",
                schema: "public",
                columns: table => new
                {
                    feature_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    feature_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Feature_pkey", x => x.feature_id);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                schema: "public",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    buyer_id = table.Column<int>(type: "integer", nullable: false),
                    seller_id = table.Column<int>(type: "integer", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    service_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    receiver_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiver_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Order_pkey", x => x.order_id);
                    table.ForeignKey(
                        name: "Order_buyer_id_fkey",
                        column: x => x.buyer_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Order_seller_id_fkey",
                        column: x => x.seller_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "Wallet",
                schema: "public",
                columns: table => new
                {
                    wallet_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    locked_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "VND"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wallet_pkey", x => x.wallet_id);
                    table.ForeignKey(
                        name: "Wallet_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageFeature",
                schema: "public",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "integer", nullable: false),
                    feature_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PackageFeature_pkey", x => new { x.package_id, x.feature_id });
                    table.ForeignKey(
                        name: "FK_PackageFeature_Feature",
                        column: x => x.feature_id,
                        principalSchema: "public",
                        principalTable: "Feature",
                        principalColumn: "feature_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageFeature_Package",
                        column: x => x.package_id,
                        principalSchema: "public",
                        principalTable: "Package",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscrowSession",
                schema: "public",
                columns: table => new
                {
                    escrow_session_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    event_id = table.Column<int>(type: "integer", nullable: true),
                    sender_id = table.Column<int>(type: "integer", nullable: false),
                    receiver_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    service_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    resolved_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("EscrowSession_pkey", x => x.escrow_session_id);
                    table.ForeignKey(
                        name: "EscrowSession_receiver_id_fkey",
                        column: x => x.receiver_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "EscrowSession_sender_id_fkey",
                        column: x => x.sender_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "FK_EscrowSession_Events_event_id",
                        column: x => x.event_id,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EscrowSession_Order_order_id",
                        column: x => x.order_id,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetail",
                schema: "public",
                columns: table => new
                {
                    order_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    outfit_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("OrderDetail_pkey", x => x.order_detail_id);
                    table.ForeignKey(
                        name: "OrderDetail_order_id_fkey",
                        column: x => x.order_id,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "OrderDetail_outfit_id_fkey",
                        column: x => x.outfit_id,
                        principalSchema: "public",
                        principalTable: "Outfit",
                        principalColumn: "outfit_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_wallet_id",
                schema: "public",
                table: "Transaction",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "RefreshToken_account_id_idx",
                schema: "public",
                table: "RefreshToken",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeEvent_escrow_session_id",
                schema: "public",
                table: "PrizeEvent",
                column: "escrow_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_EventWinner_account_id",
                schema: "public",
                table: "EventWinner",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_EventWinner_prize_event_id",
                schema: "public",
                table: "EventWinner",
                column: "prize_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubscription_account_id",
                schema: "public",
                table: "AccountSubscription",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubscription_package_id",
                schema: "public",
                table: "AccountSubscription",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowSession_event_id",
                schema: "public",
                table: "EscrowSession",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowSession_order_id",
                schema: "public",
                table: "EscrowSession",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscrowSession_receiver_id",
                schema: "public",
                table: "EscrowSession",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowSession_sender_id",
                schema: "public",
                table: "EscrowSession",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Feature_FeatureCode",
                schema: "public",
                table: "Feature",
                column: "feature_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_buyer_id",
                schema: "public",
                table: "Order",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_seller_id",
                schema: "public",
                table: "Order",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_order_id",
                schema: "public",
                table: "OrderDetail",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_outfit_id",
                schema: "public",
                table: "OrderDetail",
                column: "outfit_id");

            migrationBuilder.CreateIndex(
                name: "IX_PackageFeature_feature_id",
                schema: "public",
                table: "PackageFeature",
                column: "feature_id");

            migrationBuilder.CreateIndex(
                name: "Wallet_account_id_key",
                schema: "public",
                table: "Wallet",
                column: "account_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "EventWinner_account_id_fkey",
                schema: "public",
                table: "EventWinner",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "EventWinner_prize_event_id_fkey",
                schema: "public",
                table: "EventWinner",
                column: "prize_event_id",
                principalSchema: "public",
                principalTable: "PrizeEvent",
                principalColumn: "prize_event_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Package_Account",
                schema: "public",
                table: "Package",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Payment_package_id_fkey",
                schema: "public",
                table: "Payment",
                column: "package_id",
                principalSchema: "public",
                principalTable: "Package",
                principalColumn: "package_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "PrizeEvent_escrow_session_id_fkey",
                schema: "public",
                table: "PrizeEvent",
                column: "escrow_session_id",
                principalSchema: "public",
                principalTable: "EscrowSession",
                principalColumn: "escrow_session_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "PrizeEvent_event_id_fkey",
                schema: "public",
                table: "PrizeEvent",
                column: "event_id",
                principalSchema: "public",
                principalTable: "Events",
                principalColumn: "event_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "RefreshToken_account_id_fkey",
                schema: "public",
                table: "RefreshToken",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Accounts_AccountId",
                schema: "public",
                table: "Transaction",
                column: "AccountId",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Transaction_payment_id_fkey",
                schema: "public",
                table: "Transaction",
                column: "payment_id",
                principalSchema: "public",
                principalTable: "Payment",
                principalColumn: "payment_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "Transaction_wallet_id_fkey",
                schema: "public",
                table: "Transaction",
                column: "wallet_id",
                principalSchema: "public",
                principalTable: "Wallet",
                principalColumn: "wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "EventWinner_account_id_fkey",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropForeignKey(
                name: "EventWinner_prize_event_id_fkey",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropForeignKey(
                name: "FK_Package_Account",
                schema: "public",
                table: "Package");

            migrationBuilder.DropForeignKey(
                name: "Payment_package_id_fkey",
                schema: "public",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "PrizeEvent_escrow_session_id_fkey",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropForeignKey(
                name: "PrizeEvent_event_id_fkey",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropForeignKey(
                name: "RefreshToken_account_id_fkey",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Accounts_AccountId",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "Transaction_payment_id_fkey",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "Transaction_wallet_id_fkey",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropTable(
                name: "AccountSubscription",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscrowSession",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OrderDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PackageFeature",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wallet",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Order",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Feature",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_wallet_id",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "RefreshToken_account_id_idx",
                schema: "public",
                table: "RefreshToken");

            migrationBuilder.DropPrimaryKey(
                name: "PrizeEvent_pkey",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropIndex(
                name: "IX_PrizeEvent_escrow_session_id",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropPrimaryKey(
                name: "EventWinner_pkey",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropIndex(
                name: "IX_EventWinner_account_id",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropIndex(
                name: "IX_EventWinner_prize_event_id",
                schema: "public",
                table: "EventWinner");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "balance_before",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "escrow_session_id",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "reward_amount",
                schema: "public",
                table: "PrizeEvent");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "public",
                table: "Package");

            migrationBuilder.DropColumn(
                name: "price",
                schema: "public",
                table: "Package");

            migrationBuilder.RenameTable(
                name: "EventWinner",
                schema: "public",
                newName: "TheEventWinner",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                schema: "public",
                table: "Transaction",
                newName: "account_id");

            migrationBuilder.RenameColumn(
                name: "wallet_id",
                schema: "public",
                table: "Transaction",
                newName: "amount_coin");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_AccountId",
                schema: "public",
                table: "Transaction",
                newName: "IX_Transaction_account_id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "public",
                table: "PrizeEvent",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "duration_days",
                schema: "public",
                table: "Package",
                newName: "price_vnd");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "public",
                table: "TheEventWinner",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "public",
                table: "TheEventWinner",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "prize_event_id",
                schema: "public",
                table: "TheEventWinner",
                newName: "PrizeEventId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "public",
                table: "TheEventWinner",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "account_id",
                schema: "public",
                table: "TheEventWinner",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "event_winner_id",
                schema: "public",
                table: "TheEventWinner",
                newName: "EventWinnerId");

            migrationBuilder.AlterColumn<string>(
                name: "reference_type",
                schema: "public",
                table: "Transaction",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "payment_id",
                schema: "public",
                table: "Transaction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Transaction",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<int>(
                name: "balance_after",
                schema: "public",
                table: "Transaction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "account_id",
                schema: "public",
                table: "Transaction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "PrizeEvent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "public",
                table: "PrizeEvent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "public",
                table: "PrizeEvent",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "create_at",
                schema: "public",
                table: "PrizeEvent",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "reward_coin",
                schema: "public",
                table: "PrizeEvent",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "public",
                table: "Package",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "coin_amount",
                schema: "public",
                table: "Package",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "TheEventWinner",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "TheEventWinner",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PrizeEvent",
                schema: "public",
                table: "PrizeEvent",
                column: "prize_event_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TheEventWinner",
                schema: "public",
                table: "TheEventWinner",
                column: "EventWinnerId");

            migrationBuilder.CreateIndex(
                name: "RefreshToken_account_id_key",
                schema: "public",
                table: "RefreshToken",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RefreshToken_device_info_key",
                schema: "public",
                table: "RefreshToken",
                column: "device_info");

            migrationBuilder.CreateIndex(
                name: "RefreshToken_ip_address_key",
                schema: "public",
                table: "RefreshToken",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "IX_TheEventWinner_AccountId",
                schema: "public",
                table: "TheEventWinner",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheEventWinner_PrizeEventId",
                schema: "public",
                table: "TheEventWinner",
                column: "PrizeEventId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "Package_account_id_fkey",
                schema: "public",
                table: "Package",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Payment_package_id_fkey",
                schema: "public",
                table: "Payment",
                column: "package_id",
                principalSchema: "public",
                principalTable: "Package",
                principalColumn: "package_id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrizeEvent_Events_event_id",
                schema: "public",
                table: "PrizeEvent",
                column: "event_id",
                principalSchema: "public",
                principalTable: "Events",
                principalColumn: "event_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "RefreshToken_account_id_fkey",
                schema: "public",
                table: "RefreshToken",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_TheEventWinner_Accounts_AccountId",
                schema: "public",
                table: "TheEventWinner",
                column: "AccountId",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TheEventWinner_PrizeEvent_PrizeEventId",
                schema: "public",
                table: "TheEventWinner",
                column: "PrizeEventId",
                principalSchema: "public",
                principalTable: "PrizeEvent",
                principalColumn: "prize_event_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Transaction_account_id_fkey",
                schema: "public",
                table: "Transaction",
                column: "account_id",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "Transaction_payment_id_fkey",
                schema: "public",
                table: "Transaction",
                column: "payment_id",
                principalSchema: "public",
                principalTable: "Payment",
                principalColumn: "payment_id");
        }
    }
}
