using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repositories.Entities;

namespace Repositories.Data;

public partial class FashionDbContext : DbContext
{
    public FashionDbContext()
    {
    }

    public FashionDbContext(DbContextOptions<FashionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<ExpertFile> ExpertFiles { get; set; }

    public virtual DbSet<ExpertProfile> ExpertProfiles { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupUser> GroupUsers { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<MessReaction> MessReactions { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Outfit> Outfits { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Photo> Photos { get; set; }

    public virtual DbSet<PinnedMessage> PinnedMessages { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<PostVector> PostVectors { get; set; }

    public virtual DbSet<Reaction> Reactions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReportType> ReportTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<UserProfileVector> UserProfileVectors { get; set; }

    public virtual DbSet<UserReport> UserReports { get; set; }

    public virtual DbSet<Wardrobe> Wardrobes { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseNpgsql("Host=localhost;Database=fashionDB;Username=postgres;Password=12345");

    public static string GetConnectionString(string connectionStringName)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        return config.GetConnectionString(connectionStringName);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseNpgsql(GetConnectionString("DefaultConnection"), o => o.UseVector())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("Account_pkey");

            entity.ToTable("Account", "fashion_db");

            entity.HasIndex(e => e.Email, "Account_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "Account_username_key").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Account_role_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("Category_pkey");

            entity.ToTable("Category", "fashion_db");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("Comment_pkey");

            entity.ToTable("Comment", "fashion_db");

            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.PostId).HasColumnName("post_id");

            entity.HasOne(d => d.Account).WithMany(p => p.Comments)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Comment_account_id_fkey");

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Comment_post_id_fkey");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("Events_pkey");

            entity.ToTable("Events", "fashion_db");

            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Creator).WithMany(p => p.Events)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Events_creator_id_fkey");
        });

        modelBuilder.Entity<ExpertFile>(entity =>
        {
            entity.HasKey(e => e.ExpertFileId).HasName("Expert_File_pkey");

            entity.ToTable("Expert_File", "fashion_db");

            entity.HasIndex(e => e.ExpertProfileId, "Expert_File_expert_profile_id_key").IsUnique();

            entity.Property(e => e.ExpertFileId).HasColumnName("expert_file_id");
            entity.Property(e => e.Bio)
                .HasMaxLength(500)
                .HasColumnName("bio");
            entity.Property(e => e.CertificateUrl)
                .HasMaxLength(500)
                .HasColumnName("certificate_url");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExperienceYears).HasColumnName("experience_years");
            entity.Property(e => e.ExpertProfileId).HasColumnName("expert_profile_id");
            entity.Property(e => e.LicenseUrl)
                .HasMaxLength(500)
                .HasColumnName("license_url");
            entity.Property(e => e.RatingAvg).HasColumnName("rating_avg");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Verified).HasColumnName("verified");

            entity.HasOne(d => d.ExpertProfile).WithOne(p => p.ExpertFile)
                .HasForeignKey<ExpertFile>(d => d.ExpertProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Expert_File_expert_profile_id_fkey");
        });

        modelBuilder.Entity<ExpertProfile>(entity =>
        {
            entity.HasKey(e => e.ExpertProfileId).HasName("Expert_Profile_pkey");

            entity.ToTable("Expert_Profile", "fashion_db");

            entity.HasIndex(e => e.AccountId, "Expert_Profile_account_id_key").IsUnique();

            entity.Property(e => e.ExpertProfileId).HasColumnName("expert_profile_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpertiseField)
                .HasMaxLength(100)
                .HasColumnName("expertise_field");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Verified)
                .HasDefaultValue(false)
                .HasColumnName("verified");
            entity.Property(e => e.YearsOfExperience).HasColumnName("years_of_experience");

            entity.HasOne(d => d.Account).WithOne(p => p.ExpertProfile)
                .HasForeignKey<ExpertProfile>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Expert_Profile_account_id_fkey");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("Group_pkey");

            entity.ToTable("Group", "fashion_db");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsGroup).HasColumnName("isGroup");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<GroupUser>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.AccountId }).HasName("GroupUser_pkey");

            entity.ToTable("GroupUser", "fashion_db");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.JoinedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("joined_at");

            entity.HasOne(d => d.Account).WithMany(p => p.GroupUsers)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GroupUser_account_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupUsers)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GroupUser_group_id_fkey");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("Images_pkey");

            entity.ToTable("Images", "fashion_db");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.OwnerType)
                .HasMaxLength(50)
                .HasColumnName("owner_type");

            entity.HasOne(d => d.Owner).WithMany(p => p.Images)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Images_owner_id_fkey");

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.Images)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Images_owner_id_fkey1");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("Item_pkey");

            entity.ToTable("Item", "fashion_db");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Brand)
                .HasMaxLength(50)
                .HasColumnName("brand");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Fabric)
                .HasMaxLength(50)
                .HasColumnName("fabric");
            entity.Property(e => e.ItemName)
                .HasMaxLength(100)
                .HasColumnName("item_name");
            entity.Property(e => e.MainColor)
                .HasMaxLength(50)
                .HasColumnName("main_color");
            entity.Property(e => e.Pattern)
                .HasMaxLength(255)
                .HasColumnName("pattern");
            entity.Property(e => e.Placement)
                .HasMaxLength(255)
                .HasColumnName("placement");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Style)
                .HasMaxLength(255)
                .HasColumnName("style");
            entity.Property(e => e.StyleScore).HasColumnName("style_score");
            entity.Property(e => e.Texture)
                .HasMaxLength(255)
                .HasColumnName("texture");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("update_at");
            entity.Property(e => e.WardrobeId).HasColumnName("wardrobe_id");

            entity.HasOne(d => d.Wardrobe).WithMany(p => p.Items)
                .HasForeignKey(d => d.WardrobeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Item_wardrobe_id_fkey");

            entity.HasMany(d => d.Categories).WithMany(p => p.Items)
                .UsingEntity<Dictionary<string, object>>(
                    "ItemCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("Item_Category_category_id_fkey"),
                    l => l.HasOne<Item>().WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("Item_Category_item_id_fkey"),
                    j =>
                    {
                        j.HasKey("ItemId", "CategoryId").HasName("Item_Category_pkey");
                        j.ToTable("Item_Category", "fashion_db");
                        j.IndexerProperty<int>("ItemId").HasColumnName("item_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<MessReaction>(entity =>
        {
            entity.HasKey(e => e.ReactId).HasName("MessReaction_pkey");

            entity.ToTable("MessReaction", "fashion_db");

            entity.Property(e => e.ReactId).HasColumnName("react_id");
            entity.Property(e => e.AccountReactId).HasColumnName("account_react_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.Type)
                .HasColumnType("character varying")
                .HasColumnName("type");

            entity.HasOne(d => d.AccountReact).WithMany(p => p.MessReactions)
                .HasForeignKey(d => d.AccountReactId)
                .HasConstraintName("MessReaction_account_react_id_fkey");

            entity.HasOne(d => d.Message).WithMany(p => p.MessReactions)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("MessReaction_message_id_fkey");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("Message_pkey");

            entity.ToTable("Message", "fashion_db");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.IsRecalled).HasColumnName("isRecalled");
            entity.Property(e => e.ReplyToMessageId).HasColumnName("replyToMessage_id");
            entity.Property(e => e.SentAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sentAt");

            entity.HasOne(d => d.Account).WithMany(p => p.Messages)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("Message_account_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.Messages)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("Message_group_id_fkey");

            entity.HasOne(d => d.ReplyToMessage).WithMany(p => p.InverseReplyToMessage)
                .HasForeignKey(d => d.ReplyToMessageId)
                .HasConstraintName("Message_replyToMessage_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Notification_pkey");

            entity.ToTable("Notification", "fashion_db");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.HasOne(d => d.Sender).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notification_sender_id_fkey");
        });

        modelBuilder.Entity<Outfit>(entity =>
        {
            entity.HasKey(e => e.OutfitId).HasName("Outfit_pkey");

            entity.ToTable("Outfit", "fashion_db");

            entity.Property(e => e.OutfitId).HasColumnName("outfit_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.OutfitName)
                .HasMaxLength(100)
                .HasColumnName("outfit_name");

            entity.HasOne(d => d.Account).WithMany(p => p.Outfits)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Outfit_account_id_fkey");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("Package_pkey");

            entity.ToTable("Package", "fashion_db");

            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CoinAmount).HasColumnName("coin_amount");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PriceVnd).HasColumnName("price_vnd");

            entity.HasOne(d => d.Account).WithMany(p => p.Packages)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Package_account_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("Payment_pkey");

            entity.ToTable("Payment", "fashion_db");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(100)
                .HasColumnName("order_code");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.PaidAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("paid_at");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Account).WithMany(p => p.Payments)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Payment_account_id_fkey");

            entity.HasOne(d => d.Package).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("Payment_package_id_fkey");
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("Photos_pkey");

            entity.ToTable("Photos", "fashion_db");

            entity.Property(e => e.PhotoId).HasColumnName("photo_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.PhotoUrl)
                .HasColumnType("character varying")
                .HasColumnName("photo_url");

            entity.HasOne(d => d.Message).WithMany(p => p.Photos)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("Photos_message_id_fkey");
        });

        modelBuilder.Entity<PinnedMessage>(entity =>
        {
            entity.HasKey(e => e.PinnedMsgId).HasName("PinnedMessage_pkey");

            entity.ToTable("PinnedMessage", "fashion_db");

            entity.Property(e => e.PinnedMsgId).HasColumnName("pinnedMsg_id");
            entity.Property(e => e.AccountPinnedId).HasColumnName("accountPinned_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.PinnedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("pinned_at");

            entity.HasOne(d => d.AccountPinned).WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.AccountPinnedId)
                .HasConstraintName("PinnedMessage_accountPinned_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("PinnedMessage_group_id_fkey");

            entity.HasOne(d => d.Message).WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("PinnedMessage_message_id_fkey");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_pkey");

            entity.ToTable("Post", "fashion_db");

            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.IsExpertPost).HasColumnName("is_expert_post");
            entity.Property(e => e.LikeCount)
                .HasDefaultValue(0)
                .HasColumnName("like_count");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.ShareCount)
                .HasDefaultValue(0)
                .HasColumnName("share_count");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Tittle).HasColumnName("tittle");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Post_account_id_fkey");

            entity.HasOne(d => d.Event).WithMany(p => p.Posts)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("Post_event_id_fkey");
        });

        modelBuilder.Entity<PostVector>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_Vector_pkey");

            entity.ToTable("Post_Vector", "fashion_db");

            entity.Property(e => e.PostId)
                .ValueGeneratedNever()
                .HasColumnName("post_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.VectorData).HasColumnName("vector_data");

            entity.HasOne(d => d.Post).WithOne(p => p.PostVector)
                .HasForeignKey<PostVector>(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Post_Vector_post_id_fkey");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.ReactionId).HasName("Reaction_pkey");

            entity.ToTable("Reaction", "fashion_db");

            entity.Property(e => e.ReactionId).HasColumnName("reaction_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.ReactionType)
                .HasMaxLength(30)
                .HasColumnName("reaction_type");

            entity.HasOne(d => d.Account).WithMany(p => p.Reactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Reaction_account_id_fkey");

            entity.HasOne(d => d.Post).WithMany(p => p.Reactions)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Reaction_post_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("RefreshToken_pkey");

            entity.ToTable("RefreshToken", "fashion_db");

            entity.HasIndex(e => e.AccountId, "RefreshToken_account_id_key").IsUnique();

            entity.HasIndex(e => e.DeviceInfo, "RefreshToken_device_info_key").IsUnique();

            entity.HasIndex(e => e.IpAddress, "RefreshToken_ip_address_key").IsUnique();

            entity.HasIndex(e => e.Token, "RefreshToken_token_key").IsUnique();

            entity.Property(e => e.RefreshTokenId).HasColumnName("refresh_token_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiryDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiry_date");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(500)
                .HasColumnName("ip_address");
            entity.Property(e => e.IsAvailable).HasColumnName("isAvailable");
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .HasColumnName("token");

            entity.HasOne(d => d.Account).WithOne(p => p.RefreshToken)
                .HasForeignKey<RefreshToken>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("RefreshToken_account_id_fkey");
        });

        modelBuilder.Entity<ReportType>(entity =>
        {
            entity.HasKey(e => e.ReportTypeId).HasName("Report_Type_pkey");

            entity.ToTable("Report_Type", "fashion_db");

            entity.HasIndex(e => e.TypeName, "Report_Type_type_name_key").IsUnique();

            entity.Property(e => e.ReportTypeId).HasColumnName("report_type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Role_pkey");

            entity.ToTable("Role", "fashion_db");

            entity.HasIndex(e => e.RoleName, "Role_role_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("Transaction_pkey");

            entity.ToTable("Transaction", "fashion_db");

            entity.HasIndex(e => e.PaymentId, "Transaction_payment_id_key").IsUnique();

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AmountCoin).HasColumnName("amount_coin");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(30)
                .HasColumnName("reference_type");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");

            entity.HasOne(d => d.Account).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Transaction_account_id_fkey");

            entity.HasOne(d => d.Payment).WithOne(p => p.Transaction)
                .HasForeignKey<Transaction>(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Transaction_payment_id_fkey");
        });

        modelBuilder.Entity<UserProfileVector>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("User_Profile_Vector_pkey");

            entity.ToTable("User_Profile_Vector", "fashion_db");

            entity.Property(e => e.AccountId)
                .ValueGeneratedOnAdd()
                .HasColumnName("account_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account).WithOne(p => p.UserProfileVector)
                .HasForeignKey<UserProfileVector>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Profile_Vector_account_id_fkey");
        });

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.HasKey(e => e.UserReportId).HasName("User_Report_pkey");

            entity.ToTable("User_Report", "fashion_db");

            entity.Property(e => e.UserReportId).HasColumnName("user_report_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReportTypeId).HasColumnName("report_type_id");

            entity.HasOne(d => d.Account).WithMany(p => p.UserReports)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Report_account_id_fkey");

            entity.HasOne(d => d.Post).WithMany(p => p.UserReports)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Report_post_id_fkey");

            entity.HasOne(d => d.ReportType).WithMany(p => p.UserReports)
                .HasForeignKey(d => d.ReportTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Report_report_type_id_fkey");
        });

        modelBuilder.Entity<Wardrobe>(entity =>
        {
            entity.HasKey(e => e.WardrobeId).HasName("Wardrobe_pkey");

            entity.ToTable("Wardrobe", "fashion_db");

            entity.HasIndex(e => e.AccountId, "Wardrobe_account_id_key").IsUnique();

            entity.Property(e => e.WardrobeId).HasColumnName("wardrobe_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.Account).WithOne(p => p.Wardrobe)
                .HasForeignKey<Wardrobe>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Wardrobe_account_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
