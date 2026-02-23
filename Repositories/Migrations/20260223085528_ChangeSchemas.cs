using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

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
                name: "Group",
                schema: "public",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying", nullable: true),
                    isGroup = table.Column<bool>(type: "boolean", nullable: true),
                    create_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                name: "Role",
                schema: "public",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Role_pkey", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                schema: "public",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    verification_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    code_expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Account_pkey", x => x.account_id);
                    table.ForeignKey(
                        name: "Account_role_id_fkey",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "Role",
                        principalColumn: "role_id");
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
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Events_pkey", x => x.event_id);
                    table.ForeignKey(
                        name: "Events_creator_id_fkey",
                        column: x => x.creator_id,
                        principalSchema: "public",
                        principalTable: "Account",
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
                    years_of_experience = table.Column<int>(type: "integer", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
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
                        principalTable: "Account",
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
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Follow_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Account",
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
                        principalTable: "Account",
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
                        principalTable: "Account",
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
                name: "Notification",
                schema: "public",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_id = table.Column<int>(type: "integer", nullable: false),
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
                        principalTable: "Account",
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
                        principalTable: "Account",
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
                    coin_amount = table.Column<int>(type: "integer", nullable: false),
                    price_vnd = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Package_pkey", x => x.package_id);
                    table.ForeignKey(
                        name: "Package_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
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
                        principalTable: "Account",
                        principalColumn: "account_id");
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
                        principalTable: "Account",
                        principalColumn: "account_id");
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
                        principalTable: "Account",
                        principalColumn: "account_id");
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
                    tittle = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_expert_post = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    score = table.Column<double>(type: "double precision", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    share_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Post_pkey", x => x.post_id);
                    table.ForeignKey(
                        name: "Post_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Post_event_id_fkey",
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
                    certificate_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rating_avg = table.Column<double>(type: "double precision", nullable: true),
                    experience_years = table.Column<int>(type: "integer", nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Expert_File_pkey", x => x.expert_file_id);
                    table.ForeignKey(
                        name: "Expert_File_expert_profile_id_fkey",
                        column: x => x.expert_profile_id,
                        principalSchema: "public",
                        principalTable: "Expert_Profile",
                        principalColumn: "expert_profile_id");
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
                        principalTable: "Account",
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
                        principalTable: "Account",
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
                name: "Payment",
                schema: "public",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    package_id = table.Column<int>(type: "integer", nullable: true),
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
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Payment_package_id_fkey",
                        column: x => x.package_id,
                        principalSchema: "public",
                        principalTable: "Package",
                        principalColumn: "package_id");
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
                name: "Comment",
                schema: "public",
                columns: table => new
                {
                    comment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Comment_pkey", x => x.comment_id);
                    table.ForeignKey(
                        name: "Comment_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Comment_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id");
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
                name: "Reaction",
                schema: "public",
                columns: table => new
                {
                    reaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    reaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Reaction_pkey", x => x.reaction_id);
                    table.ForeignKey(
                        name: "Reaction_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Reaction_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id");
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
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_Report_pkey", x => x.user_report_id);
                    table.ForeignKey(
                        name: "User_Report_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "User_Report_post_id_fkey",
                        column: x => x.post_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id");
                    table.ForeignKey(
                        name: "User_Report_report_type_id_fkey",
                        column: x => x.report_type_id,
                        principalSchema: "public",
                        principalTable: "Report_Type",
                        principalColumn: "report_type_id");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                schema: "public",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    payment_id = table.Column<int>(type: "integer", nullable: false),
                    amount_coin = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    balance_after = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Transaction_pkey", x => x.transaction_id);
                    table.ForeignKey(
                        name: "Transaction_account_id_fkey",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "Account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "Transaction_payment_id_fkey",
                        column: x => x.payment_id,
                        principalSchema: "public",
                        principalTable: "Payment",
                        principalColumn: "payment_id");
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "public",
                columns: table => new
                {
                    image_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    owner_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Images_pkey", x => x.image_id);
                    table.ForeignKey(
                        name: "Images_owner_id_fkey",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "Item",
                        principalColumn: "item_id");
                    table.ForeignKey(
                        name: "Images_owner_id_fkey1",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "post_id");
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

            migrationBuilder.CreateIndex(
                name: "Account_email_key",
                schema: "public",
                table: "Account",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Account_username_key",
                schema: "public",
                table: "Account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_role_id",
                schema: "public",
                table: "Account",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_account_id",
                schema: "public",
                table: "Comment",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_post_id",
                schema: "public",
                table: "Comment",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_Events_creator_id",
                schema: "public",
                table: "Events",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "Expert_File_expert_profile_id_key",
                schema: "public",
                table: "Expert_File",
                column: "expert_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Expert_Profile_account_id_key",
                schema: "public",
                table: "Expert_Profile",
                column: "account_id",
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
                name: "IX_Images_owner_id",
                schema: "public",
                table: "Images",
                column: "owner_id");

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
                name: "IX_Post_account_id",
                schema: "public",
                table: "Post",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Post_event_id",
                schema: "public",
                table: "Post",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reaction_account_id",
                schema: "public",
                table: "Reaction",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reaction_post_id",
                schema: "public",
                table: "Reaction",
                column: "post_id");

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
                column: "device_info",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RefreshToken_ip_address_key",
                schema: "public",
                table: "RefreshToken",
                column: "ip_address",
                unique: true);

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
                name: "Role_role_name_key",
                schema: "public",
                table: "Role",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_account_id",
                schema: "public",
                table: "Transaction",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "Transaction_payment_id_key",
                schema: "public",
                table: "Transaction",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_account_id",
                schema: "public",
                table: "User_Report",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_post_id",
                schema: "public",
                table: "User_Report",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Report_report_type_id",
                schema: "public",
                table: "User_Report",
                column: "report_type_id");

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
                name: "Comment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Expert_File",
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
                name: "Outfit",
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
                name: "Reaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Transaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "User_Profile_Vector",
                schema: "public");

            migrationBuilder.DropTable(
                name: "User_Report",
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
                name: "Message",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Post",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Report_Type",
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
                name: "Account",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "public");
        }
    }
}
