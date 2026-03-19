using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitNewDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "public",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    verification_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    code_expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    free_try_on = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    count_post = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    count_follower = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    count_following = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    isOnline = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    normalized_username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Account_pkey", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "public",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Category_pkey", x => x.category_id);
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
                name: "Group",
                schema: "public",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying", nullable: true),
                    isGroup = table.Column<bool>(type: "boolean", nullable: true),
                    create_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_activity = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Group_pkey", x => x.group_id);
                });

            migrationBuilder.CreateTable(
                name: "Report_Type",
                schema: "public",
                columns: table => new
                {
                    report_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Report_Type_pkey", x => x.report_type_id);
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
                name: "AccountModels",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acc_id = table.Column<int>(type: "integer", nullable: false),
                    img_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountModels", x => x.id);
                    table.ForeignKey(
                        name: "FK_Account_Models",
                        column: x => x.acc_id,
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
                name: "Events",
                schema: "public",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    creator_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    expert_weight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    user_weight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    point_per_like = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    point_per_share = table.Column<double>(type: "double precision", nullable: false, defaultValue: 2.0),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Events_pkey", x => x.event_id);
                    table.ForeignKey(
                        name: "Events_creator_id_fkey",
                        column: x => x.creator_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "Expert_Profile",
                schema: "public",
                columns: table => new
                {
                    expert_profile_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    expertise_field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    style_aesthetic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    years_of_experience = table.Column<int>(type: "integer", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    rating_avg = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Expert_Profile_pkey", x => x.expert_profile_id);
                    table.ForeignKey(
                        name: "Expert_Profile_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "Follow",
                schema: "public",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    follower_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Follow_pkey", x => new { x.user_id, x.follower_id });
                    table.ForeignKey(
                        name: "Follow_follower_id_fkey",
                        column: x => x.follower_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Follow_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "public",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_id = table.Column<int>(type: "integer", nullable: false),
                    target_user_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Notification_pkey", x => x.notification_id);
                    table.ForeignKey(
                        name: "Notification_sender_id_fkey",
                        column: x => x.sender_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
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
                name: "Outfit",
                schema: "public",
                columns: table => new
                {
                    outfit_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    outfit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Outfit_pkey", x => x.outfit_id);
                    table.ForeignKey(
                        name: "Outfit_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "Package",
                schema: "public",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Package_pkey", x => x.package_id);
                    table.ForeignKey(
                        name: "FK_Package_Account",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "public",
                columns: table => new
                {
                    refresh_token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    isAvailable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RefreshToken_pkey", x => x.refresh_token_id);
                    table.ForeignKey(
                        name: "RefreshToken_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TryOnHistory",
                schema: "public",
                columns: table => new
                {
                    tryon_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acc_id = table.Column<int>(type: "integer", nullable: false),
                    img_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    create_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TryOnHistory", x => x.tryon_id);
                    table.ForeignKey(
                        name: "FK_TryOnHistory_Accounts_acc_id",
                        column: x => x.acc_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_Profile_Vector",
                schema: "public",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_Profile_Vector_pkey", x => x.account_id);
                    table.ForeignKey(
                        name: "User_Profile_Vector_account_id_fkey",
                        column: x => x.account_id,
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
                name: "Wardrobe",
                schema: "public",
                columns: table => new
                {
                    wardrobe_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wardrobe_pkey", x => x.wardrobe_id);
                    table.ForeignKey(
                        name: "Wardrobe_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "GroupUser",
                schema: "public",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GroupUser_pkey", x => new { x.group_id, x.account_id });
                    table.ForeignKey(
                        name: "GroupUser_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "GroupUser_group_id_fkey",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "Group",
                        principalColumn: "group_id");
                });

            migrationBuilder.CreateTable(
                name: "Message",
                schema: "public",
                columns: table => new
                {
                    message_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    replyToMessage_id = table.Column<int>(type: "integer", nullable: true),
                    isRecalled = table.Column<bool>(type: "boolean", nullable: true),
                    sentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Message_pkey", x => x.message_id);
                    table.ForeignKey(
                        name: "Message_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Message_group_id_fkey",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "Group",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "Message_replyToMessage_id_fkey",
                        column: x => x.replyToMessage_id,
                        principalSchema: "public",
                        principalTable: "Message",
                        principalColumn: "message_id");
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
                name: "Post",
                schema: "public",
                columns: table => new
                {
                    post_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    event_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_expert_post = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, defaultValue: "Draft"),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Visible"),
                    score = table.Column<double>(type: "double precision", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    share_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("post_pkey", x => x.post_id);
                    table.ForeignKey(
                        name: "post_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "post_event_id_fkey",
                        column: x => x.event_id,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "event_id");
                });

            migrationBuilder.CreateTable(
                name: "Expert_File",
                schema: "public",
                columns: table => new
                {
                    expert_file_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expert_profile_id = table.Column<int>(type: "integer", nullable: false),
                    expertise_field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    style_aesthetic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    years_of_experience = table.Column<int>(type: "integer", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    cv_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    certificate_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    identity_proof_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Expert_File_pkey", x => x.expert_file_id);
                    table.ForeignKey(
                        name: "Expert_File_expert_profile_id_fkey",
                        column: x => x.expert_profile_id,
                        principalSchema: "public",
                        principalTable: "Expert_Profile",
                        principalColumn: "expert_profile_id",
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
                name: "Payment",
                schema: "public",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    package_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    order_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Payment_pkey", x => x.payment_id);
                    table.ForeignKey(
                        name: "Payment_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Payment_package_id_fkey",
                        column: x => x.package_id,
                        principalSchema: "public",
                        principalTable: "Package",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                schema: "public",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wardrobe_id = table.Column<int>(type: "integer", nullable: false),
                    item_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    main_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pattern = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    style = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    texture = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fabric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    placement = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    style_score = table.Column<double>(type: "double precision", nullable: true),
                    item_embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Item_pkey", x => x.item_id);
                    table.ForeignKey(
                        name: "Item_wardrobe_id_fkey",
                        column: x => x.wardrobe_id,
                        principalSchema: "public",
                        principalTable: "Wardrobe",
                        principalColumn: "wardrobe_id");
                });

            migrationBuilder.CreateTable(
                name: "MessReaction",
                schema: "public",
                columns: table => new
                {
                    react_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying", nullable: true),
                    account_react_id = table.Column<int>(type: "integer", nullable: true),
                    message_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("MessReaction_pkey", x => x.react_id);
                    table.ForeignKey(
                        name: "MessReaction_account_react_id_fkey",
                        column: x => x.account_react_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "MessReaction_message_id_fkey",
                        column: x => x.message_id,
                        principalSchema: "public",
                        principalTable: "Message",
                        principalColumn: "message_id");
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                schema: "public",
                columns: table => new
                {
                    photo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    photo_url = table.Column<string>(type: "character varying", nullable: true),
                    message_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Photos_pkey", x => x.photo_id);
                    table.ForeignKey(
                        name: "Photos_message_id_fkey",
                        column: x => x.message_id,
                        principalSchema: "public",
                        principalTable: "Message",
                        principalColumn: "message_id");
                });

            migrationBuilder.CreateTable(
                name: "PinnedMessage",
                schema: "public",
                columns: table => new
                {
                    pinnedMsg_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    accountPinned_id = table.Column<int>(type: "integer", nullable: true),
                    message_id = table.Column<int>(type: "integer", nullable: true),
                    pinned_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PinnedMessage_pkey", x => x.pinnedMsg_id);
                    table.ForeignKey(
                        name: "PinnedMessage_accountPinned_id_fkey",
                        column: x => x.accountPinned_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "PinnedMessage_group_id_fkey",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "Group",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "PinnedMessage_message_id_fkey",
                        column: x => x.message_id,
                        principalSchema: "public",
                        principalTable: "Message",
                        principalColumn: "message_id");
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                schema: "public",
                columns: table => new
                {
                    comment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    parent_comment_id = table.Column<int>(type: "integer", nullable: true),
                    content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("comment_pkey", x => x.comment_id);
                    table.ForeignKey(
                        name: "comment_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "comment_parent_comment_fkey",
                        column: x => x.parent_comment_id,
                        principalSchema: "public",
                        principalTable: "Comment",
                        principalColumn: "comment_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "comment_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "Post_Vector",
                schema: "public",
                columns: table => new
                {
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    vector_data = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Post_Vector_pkey", x => x.post_id);
                    table.ForeignKey(
                        name: "Post_Vector_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id");
                });

            migrationBuilder.CreateTable(
                name: "PostSaves",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSaves", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostSaves_Accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostSaves_Post_post_id",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reaction",
                schema: "public",
                columns: table => new
                {
                    reaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reaction_pkey", x => x.reaction_id);
                    table.ForeignKey(
                        name: "reaction_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "reaction_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scoreboard",
                schema: "public",
                columns: table => new
                {
                    scoreboard_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    final_like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    final_share_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    expert_score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    expert_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    community_score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    final_score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Scoreboard_pkey", x => x.scoreboard_id);
                    table.ForeignKey(
                        name: "Scoreboard_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_Report",
                schema: "public",
                columns: table => new
                {
                    user_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    report_type_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    reviewed_by = table.Column<int>(type: "integer", nullable: true),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_Report_pkey", x => x.user_report_id);
                    table.ForeignKey(
                        name: "User_Report_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "User_Report_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "User_Report_report_type_id_fkey",
                        column: x => x.report_type_id,
                        principalSchema: "public",
                        principalTable: "Report_Type",
                        principalColumn: "report_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrizeEvent",
                schema: "public",
                columns: table => new
                {
                    prize_event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    ranked = table.Column<int>(type: "integer", nullable: false),
                    reward_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    escrow_session_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PrizeEvent_pkey", x => x.prize_event_id);
                    table.ForeignKey(
                        name: "PrizeEvent_escrow_session_id_fkey",
                        column: x => x.escrow_session_id,
                        principalSchema: "public",
                        principalTable: "EscrowSession",
                        principalColumn: "escrow_session_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "PrizeEvent_event_id_fkey",
                        column: x => x.event_id,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                schema: "public",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_id = table.Column<int>(type: "integer", nullable: false),
                    payment_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_before = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AccountId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Transaction_pkey", x => x.transaction_id);
                    table.ForeignKey(
                        name: "FK_Transaction_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Transaction_payment_id_fkey",
                        column: x => x.payment_id,
                        principalSchema: "public",
                        principalTable: "Payment",
                        principalColumn: "payment_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "Transaction_wallet_id_fkey",
                        column: x => x.wallet_id,
                        principalSchema: "public",
                        principalTable: "Wallet",
                        principalColumn: "wallet_id");
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "public",
                columns: table => new
                {
                    image_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    post_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    account_avatar_id = table.Column<int>(type: "integer", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    event_id = table.Column<int>(type: "integer", nullable: true),
                    owner_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Images_pkey", x => x.image_id);
                    table.ForeignKey(
                        name: "FK_Images_Accounts_account_avatar_id",
                        column: x => x.account_avatar_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Events_event_id",
                        column: x => x.event_id,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Group_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "public",
                        principalTable: "Group",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Item_item_id",
                        column: x => x.item_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Post_post_id",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Item_Category",
                schema: "public",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Item_Category_pkey", x => new { x.item_id, x.category_id });
                    table.ForeignKey(
                        name: "Item_Category_category_id_fkey",
                        column: x => x.category_id,
                        principalSchema: "public",
                        principalTable: "Category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "Item_Category_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id");
                });

            migrationBuilder.CreateTable(
                name: "CommentReaction",
                schema: "public",
                columns: table => new
                {
                    comment_reaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comment_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("comment_reaction_pkey", x => x.comment_reaction_id);
                    table.ForeignKey(
                        name: "comment_reaction_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "comment_reaction_comment_id_fkey",
                        column: x => x.comment_id,
                        principalSchema: "public",
                        principalTable: "Comment",
                        principalColumn: "comment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventWinner",
                schema: "public",
                columns: table => new
                {
                    event_winner_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    prize_event_id = table.Column<int>(type: "integer", nullable: false),
                    winning_score = table.Column<double>(type: "double precision", nullable: false),
                    final_rank = table.Column<int>(type: "integer", nullable: false),
                    expert_feedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("EventWinner_pkey", x => x.event_winner_id);
                    table.ForeignKey(
                        name: "EventWinner_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "EventWinner_prize_event_id_fkey",
                        column: x => x.prize_event_id,
                        principalSchema: "public",
                        principalTable: "PrizeEvent",
                        principalColumn: "prize_event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountClaims_account_id",
                table: "AccountClaims",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogins_account_id",
                table: "AccountLogins",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountModels_acc_id",
                schema: "public",
                table: "AccountModels",
                column: "acc_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_role_id",
                table: "AccountRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "public",
                table: "Accounts",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "public",
                table: "Accounts",
                column: "normalized_username",
                unique: true);

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
                name: "ix_comment_account_id",
                schema: "public",
                table: "Comment",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_parent_comment_id",
                schema: "public",
                table: "Comment",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_post_id",
                schema: "public",
                table: "Comment",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_account_id",
                schema: "public",
                table: "CommentReaction",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_comment_id",
                schema: "public",
                table: "CommentReaction",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ux_comment_reaction_account_comment",
                schema: "public",
                table: "CommentReaction",
                columns: new[] { "account_id", "comment_id" },
                unique: true);

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
                name: "IX_Events_creator_id",
                schema: "public",
                table: "Events",
                column: "creator_id");

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
                name: "Expert_File_expert_profile_id_idx",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id");

            migrationBuilder.CreateIndex(
                name: "Expert_Profile_account_id_key",
                schema: "public",
                table: "Expert_Profile",
                column: "account_id",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "UQ_Feature_FeatureCode",
                schema: "public",
                table: "Feature",
                column: "feature_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Follow_follower_id",
                schema: "public",
                table: "Follow",
                column: "follower_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupUser_account_id",
                schema: "public",
                table: "GroupUser",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_account_avatar_id",
                schema: "public",
                table: "Images",
                column: "account_avatar_id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_event_id",
                schema: "public",
                table: "Images",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_GroupId",
                schema: "public",
                table: "Images",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_item_id",
                schema: "public",
                table: "Images",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Images_post_id",
                schema: "public",
                table: "Images",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_Item_wardrobe_id",
                schema: "public",
                table: "Item",
                column: "wardrobe_id");

            migrationBuilder.CreateIndex(
                name: "IX_Item_Category_category_id",
                schema: "public",
                table: "Item_Category",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Message_account_id",
                schema: "public",
                table: "Message",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Message_group_id",
                schema: "public",
                table: "Message",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_Message_replyToMessage_id",
                schema: "public",
                table: "Message",
                column: "replyToMessage_id");

            migrationBuilder.CreateIndex(
                name: "IX_MessReaction_account_react_id",
                schema: "public",
                table: "MessReaction",
                column: "account_react_id");

            migrationBuilder.CreateIndex(
                name: "IX_MessReaction_message_id",
                schema: "public",
                table: "MessReaction",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_sender_id",
                schema: "public",
                table: "Notification",
                column: "sender_id");

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
                name: "IX_Outfit_account_id",
                schema: "public",
                table: "Outfit",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Package_account_id",
                schema: "public",
                table: "Package",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_PackageFeature_feature_id",
                schema: "public",
                table: "PackageFeature",
                column: "feature_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_account_id",
                schema: "public",
                table: "Payment",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_package_id",
                schema: "public",
                table: "Payment",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_message_id",
                schema: "public",
                table: "Photos",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "IX_PinnedMessage_accountPinned_id",
                schema: "public",
                table: "PinnedMessage",
                column: "accountPinned_id");

            migrationBuilder.CreateIndex(
                name: "IX_PinnedMessage_group_id",
                schema: "public",
                table: "PinnedMessage",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_PinnedMessage_message_id",
                schema: "public",
                table: "PinnedMessage",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_account_id",
                schema: "public",
                table: "Post",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_event_id",
                schema: "public",
                table: "Post",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_postsaves_account_post",
                schema: "public",
                table: "PostSaves",
                columns: new[] { "account_id", "post_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostSaves_post_id",
                schema: "public",
                table: "PostSaves",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeEvent_escrow_session_id",
                schema: "public",
                table: "PrizeEvent",
                column: "escrow_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeEvent_event_id",
                schema: "public",
                table: "PrizeEvent",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_reaction_account_id",
                schema: "public",
                table: "Reaction",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_reaction_post_id",
                schema: "public",
                table: "Reaction",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ux_reaction_account_post",
                schema: "public",
                table: "Reaction",
                columns: new[] { "account_id", "post_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RefreshToken_account_id_idx",
                schema: "public",
                table: "RefreshToken",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "RefreshToken_token_key",
                schema: "public",
                table: "RefreshToken",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Report_Type_type_name_key",
                schema: "public",
                table: "Report_Type",
                column: "type_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_role_id",
                table: "RoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scoreboard_post_id",
                schema: "public",
                table: "Scoreboard",
                column: "post_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_AccountId",
                schema: "public",
                table: "Transaction",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_wallet_id",
                schema: "public",
                table: "Transaction",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "Transaction_payment_id_key",
                schema: "public",
                table: "Transaction",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistory_acc_id",
                schema: "public",
                table: "TryOnHistory",
                column: "acc_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_account_id",
                schema: "public",
                table: "User_Report",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_report_type_id",
                schema: "public",
                table: "User_Report",
                column: "report_type_id");

            migrationBuilder.CreateIndex(
                name: "User_Report_post_id_account_id_key",
                schema: "public",
                table: "User_Report",
                columns: new[] { "post_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Wallet_account_id_key",
                schema: "public",
                table: "Wallet",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Wardrobe_account_id_key",
                schema: "public",
                table: "Wardrobe",
                column: "account_id",
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
                name: "AccountModels",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AccountRoles");

            migrationBuilder.DropTable(
                name: "AccountSubscription",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AccountTokens");

            migrationBuilder.DropTable(
                name: "CommentReaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventExpert",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventWinner",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Expert_File",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExpertRating",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Follow",
                schema: "public");

            migrationBuilder.DropTable(
                name: "GroupUser",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Item_Category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MessReaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OrderDetail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PackageFeature",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Photos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PinnedMessage",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Post_Vector",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PostSaves",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Reaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "Scoreboard",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Transaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TryOnHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "User_Profile_Vector",
                schema: "public");

            migrationBuilder.DropTable(
                name: "User_Report",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Comment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PrizeEvent",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Expert_Profile",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Item",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Outfit",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Feature",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wallet",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Report_Type",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Post",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscrowSession",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wardrobe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Group",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Package",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Order",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "public");
        }
    }
}
