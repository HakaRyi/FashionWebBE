using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repositories.Constants;
using Repositories.Entities;

namespace Repositories.Data;

public partial class FashionDbContext : IdentityDbContext<Account, IdentityRole<int>, int>
{
    public FashionDbContext()
    {
    }

    public FashionDbContext(DbContextOptions<FashionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountSubscription> AccountSubscriptions { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentReaction> CommentReactions { get; set; }

    public virtual DbSet<EscrowSession> EscrowSessions { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventExpert> EventExperts { get; set; }

    public virtual DbSet<EventWinner> EventWinners { get; set; }

    public virtual DbSet<ExpertProfile> ExpertProfiles { get; set; }

    public virtual DbSet<ExpertRating> ExpertRatings { get; set; }

    public virtual DbSet<ExpertRequest> ExpertRequests { get; set; }

    public virtual DbSet<Feature> Features { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupUser> GroupUsers { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessReaction> MessReactions { get; set; }

    public virtual DbSet<Model> Models { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Outfit> Outfits { get; set; }

    public virtual DbSet<OutfitItem> OutfitItems { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<PackageFeature> PackageFeatures { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Photo> Photos { get; set; }

    public virtual DbSet<PinnedMessage> PinnedMessages { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<PostSave> PostSaves { get; set; }

    public virtual DbSet<PostVector> PostVectors { get; set; }

    public virtual DbSet<PrizeEvent> PrizeEvents { get; set; }

    public virtual DbSet<Reaction> Reactions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReportType> ReportTypes { get; set; }

    public virtual DbSet<ReputationHistory> ReputationHistorys { get; set; }

    public virtual DbSet<SavedItem> SavedItems { get; set; }

    public virtual DbSet<Scoreboard> Scoreboards { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TryOnHistory> TryOnHistories { get; set; }

    public virtual DbSet<UserProfileVector> UserProfileVectors { get; set; }

    public virtual DbSet<UserReport> UserReports { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<Wardrobe> Wardrobes { get; set; }

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

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts", "public");

            entity.HasKey(e => e.Id).HasName("Account_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("account_id");

            entity.Property(e => e.UserName)
                .HasColumnName("username")
                .HasMaxLength(100);

            entity.Property(e => e.NormalizedUserName)
                .HasColumnName("normalized_username")
                .HasMaxLength(256);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            entity.Property(e => e.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(256);

            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.IsOnline)
                .HasMaxLength(30)
                .HasColumnName("isOnline");

            entity.Property(e => e.VerificationCode)
                .HasMaxLength(100)
                .HasColumnName("verification_code");

            entity.Property(e => e.CodeExpiredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("code_expires_at");

            entity.Property(e => e.FreeTryOn)
                .HasDefaultValue(3)
                .HasColumnName("free_try_on");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.CountPost)
                .HasDefaultValue(0)
                .HasColumnName("count_post");

            entity.Property(e => e.CountFollower)
                .HasDefaultValue(0)
                .HasColumnName("count_follower");

            entity.Property(e => e.CountFollowing)
                .HasDefaultValue(0)
                .HasColumnName("count_following");

            entity.HasIndex(e => e.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique();

            entity.HasIndex(e => e.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();

            entity.HasOne(a => a.Wallet)
                .WithOne(w => w.Account)
                .HasForeignKey<Wallet>(w => w.AccountId);

            entity.HasMany(a => a.SentEscrows)
                .WithOne(e => e.Sender)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(a => a.ReceivedEscrows)
                .WithOne(e => e.Receiver)
                .HasForeignKey(e => e.ReceiverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(a => a.SavedPosts)
                .WithOne(ps => ps.Account)
                .HasForeignKey(ps => ps.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AccountSubscription", "public");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.PackageId).HasColumnName("package_id");

            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_date");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.AccountSubscriptions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Package)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdentityRole<int>>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(e => e.Id).HasColumnName("role_id");
            entity.Property(e => e.Name).HasColumnName("role_name");
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        });
        modelBuilder.Entity<Model>(entity =>
        {
            entity.ToTable("AccountModels", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("acc_id");
            entity.Property(e => e.ImageUrl).HasColumnName("img_url").HasMaxLength(500);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasColumnName("create_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.AccountModels)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_Models");
        });

        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.ToTable("AccountRoles");
            entity.Property(e => e.UserId).HasColumnName("account_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.ToTable("AccountClaims");
            entity.Property(e => e.Id).HasColumnName("claim_id");
            entity.Property(e => e.UserId).HasColumnName("account_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(entity =>
        {
            entity.ToTable("RoleClaims");
            entity.Property(e => e.Id).HasColumnName("role_claim_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.ToTable("AccountLogins");
            entity.Property(e => e.UserId).HasColumnName("account_id");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.ToTable("AccountTokens");
            entity.Property(e => e.UserId).HasColumnName("account_id");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.Name).HasColumnName("token_name");
            entity.Property(e => e.Value).HasColumnName("token_value");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId)
                  .HasName("comment_pkey");

            entity.ToTable("Comment", "public");

            entity.Property(e => e.CommentId)
                  .HasColumnName("comment_id");

            entity.Property(e => e.PostId)
                  .IsRequired()
                  .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                  .IsRequired()
                  .HasColumnName("account_id");

            entity.Property(e => e.ParentCommentId)
                  .HasColumnName("parent_comment_id");

            entity.Property(e => e.Content)
                  .IsRequired()
                  .HasMaxLength(1000)
                  .HasColumnName("content");

            entity.Property(e => e.LikeCount)
                  .HasDefaultValue(0)
                  .HasColumnName("like_count");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("NOW()")
                  .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("updated_at");

            entity.HasIndex(e => e.PostId)
                  .HasDatabaseName("ix_comment_post_id");

            entity.HasIndex(e => e.AccountId)
                  .HasDatabaseName("ix_comment_account_id");

            entity.HasIndex(e => e.ParentCommentId)
                  .HasDatabaseName("ix_comment_parent_comment_id");

            entity.HasOne(d => d.Post)
                  .WithMany(p => p.Comments)
                  .HasForeignKey(d => d.PostId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_post_id_fkey");

            entity.HasOne(d => d.Account)
                  .WithMany(p => p.Comments)
                  .HasForeignKey(d => d.AccountId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_account_id_fkey");

            entity.HasOne(d => d.ParentComment)
                  .WithMany(p => p.Replies)
                  .HasForeignKey(d => d.ParentCommentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("comment_parent_comment_fkey");
        });


        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.HasKey(e => e.CommentReactionId)
                  .HasName("comment_reaction_pkey");

            entity.ToTable("CommentReaction", "public");

            entity.Property(e => e.CommentReactionId)
                  .HasColumnName("comment_reaction_id");

            entity.Property(e => e.CommentId)
                  .IsRequired()
                  .HasColumnName("comment_id");

            entity.Property(e => e.AccountId)
                  .IsRequired()
                  .HasColumnName("account_id");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("NOW()")
                  .HasColumnName("created_at");

            entity.HasIndex(e => new { e.AccountId, e.CommentId })
                  .IsUnique()
                  .HasDatabaseName("ux_comment_reaction_account_comment");

            entity.HasIndex(e => e.CommentId)
                  .HasDatabaseName("ix_comment_reaction_comment_id");

            entity.HasIndex(e => e.AccountId)
                  .HasDatabaseName("ix_comment_reaction_account_id");

            entity.HasOne(d => d.Comment)
                  .WithMany(p => p.Reactions)
                  .HasForeignKey(d => d.CommentId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_reaction_comment_id_fkey");

            entity.HasOne(d => d.Account)
                  .WithMany()
                  .HasForeignKey(d => d.AccountId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_reaction_account_id_fkey");
        });

        modelBuilder.Entity<EscrowSession>(entity =>
        {
            entity.HasKey(e => e.EscrowSessionId)
                  .HasName("EscrowSession_pkey");

            entity.ToTable("EscrowSession", "public");

            entity.Property(e => e.EscrowSessionId)
                  .HasColumnName("escrow_session_id");

            entity.Property(e => e.Amount)
                  .HasColumnType("decimal(18,2)")
                  .HasColumnName("amount");

            entity.Property(e => e.ServiceFee)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0m)
                  .HasColumnName("service_fee");

            entity.Property(e => e.Status)
                  .HasMaxLength(30)
                  .IsRequired()
                  .HasColumnName("status");

            entity.Property(e => e.Description)
                  .HasMaxLength(500)
                  .HasColumnName("description");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .HasColumnName("created_at");

            entity.Property(e => e.ResolvedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("resolved_at");

            entity.Property(e => e.SenderId)
                  .HasColumnName("sender_id");

            entity.Property(e => e.ReceiverId)
                  .HasColumnName("receiver_id");

            entity.Property(e => e.OrderId)
                  .HasColumnName("order_id");

            entity.Property(e => e.EventId)
                  .HasColumnName("event_id");

            entity.HasOne(d => d.Sender)
                  .WithMany(p => p.SentEscrows)
                  .HasForeignKey(d => d.SenderId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("EscrowSession_sender_id_fkey");

            entity.HasOne(d => d.Receiver)
                  .WithMany(p => p.ReceivedEscrows)
                  .HasForeignKey(d => d.ReceiverId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("EscrowSession_receiver_id_fkey");

            entity.HasOne(d => d.Order)
                  .WithOne(p => p.EscrowSession)
                  .HasForeignKey<EscrowSession>(d => d.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Event)
                  .WithMany()
                  .HasForeignKey(d => d.EventId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("Events_pkey");

            entity.ToTable("Events", "public");

            entity.Property(e => e.EventId).HasColumnName("event_id");

            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MinExpertsToStart)
                .HasColumnName("min_experts_to_start");

            entity.Property(e => e.ExpertWeight).HasColumnName("expert_weight").HasDefaultValue(0.0);
            entity.Property(e => e.UserWeight).HasColumnName("user_weight").HasDefaultValue(0.0);
            entity.Property(e => e.AppliedFee)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("applied_fee")
                .HasDefaultValue(0.0m);
            entity.Property(e => e.PointPerLike).HasColumnName("point_per_like").HasDefaultValue(1.0);
            entity.Property(e => e.PointPerShare).HasColumnName("point_per_share").HasDefaultValue(2.0);

            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.SubmissionDeadline)
               .HasColumnType("timestamp with time zone")
               .HasColumnName("submission_deadline");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Creator)
                .WithMany(p => p.Events)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Events_creator_id_fkey");
        });

        modelBuilder.Entity<EventExpert>(entity =>
        {
            entity.ToTable("EventExpert", "public");
            entity.HasKey(e => e.EventExpertId).HasName("EventExpert_pkey");

            entity.Property(e => e.EventExpertId).HasColumnName("event_expert_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.ExpertId).HasColumnName("expert_id");

            entity.HasIndex(e => new { e.EventId, e.ExpertId })
                .IsUnique()
                .HasDatabaseName("IX_EventExpert_Event_Expert");

            entity.Property(e => e.JoinedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("joined_at");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Event)
                .WithMany(p => p.EventExperts)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("EventExpert_event_id_fkey");

            entity.HasOne(d => d.Expert)
                .WithMany()
                .HasForeignKey(d => d.ExpertId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("EventExpert_expert_id_fkey");
        });

        modelBuilder.Entity<ExpertRating>(entity =>
        {
            entity.ToTable("ExpertRating", "public");
            entity.HasKey(e => e.ExpertRatingId).HasName("ExpertRating_pkey");

            entity.Property(e => e.ExpertRatingId).HasColumnName("expert_rating_id");
            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.ExpertId).HasColumnName("expert_id");

            entity.HasIndex(e => new { e.PostId, e.ExpertId })
                .IsUnique()
                .HasDatabaseName("IX_ExpertRating_Post_Expert");

            entity.Property(e => e.Score)
                .IsRequired()
                .HasColumnName("score")
                .HasDefaultValue(0.0);

            entity.Property(e => e.Reason)
                .HasMaxLength(1000)
                .HasColumnName("reason");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.ExpertRatings)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("ExpertRating_post_id_fkey");

            entity.HasOne(d => d.Expert)
                .WithMany()
                .HasForeignKey(d => d.ExpertId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("ExpertRating_expert_id_fkey");
        });

        modelBuilder.Entity<EventWinner>(entity =>
        {
            entity.HasKey(e => e.EventWinnerId).HasName("EventWinner_pkey");

            entity.ToTable("EventWinner", "public");

            entity.Property(e => e.EventWinnerId).HasColumnName("event_winner_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.PrizeEventId).HasColumnName("prize_event_id");

            entity.Property(e => e.WinningScore).HasColumnName("winning_score");
            entity.Property(e => e.FinalRank).HasColumnName("final_rank");

            entity.Property(e => e.ExpertFeedback)
                .HasMaxLength(1000)
                .HasColumnName("expert_feedback");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsRequired()
                .HasColumnName("status");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.EventWinners)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EventWinner_account_id_fkey");

            entity.HasOne(d => d.PrizeEvent)
                .WithMany(p => p.EventWinners)
                .HasForeignKey(d => d.PrizeEventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("EventWinner_prize_event_id_fkey");
        });

        modelBuilder.Entity<ExpertRequest>(entity =>
        {
            entity.HasKey(e => e.ExpertFileId).HasName("Expert_File_pkey");
            entity.ToTable("Expert_File", "public");

            entity.HasIndex(e => e.ExpertProfileId, "Expert_File_expert_profile_id_idx");

            entity.Property(e => e.ExpertFileId).HasColumnName("expert_file_id");
            entity.Property(e => e.ExpertProfileId).HasColumnName("expert_profile_id");

            entity.Property(e => e.ExpertiseField)
                .HasMaxLength(200)
                .HasColumnName("expertise_field");

            entity.Property(e => e.StyleAesthetic)
                .HasMaxLength(200)
                .HasColumnName("style_aesthetic");

            entity.Property(e => e.YearsOfExperience)
                .HasColumnName("years_of_experience");

            entity.Property(e => e.Bio)
                .HasColumnName("bio");

            entity.Property(e => e.CvUrl)
                .HasMaxLength(500)
                .HasColumnName("cv_url");

            entity.Property(e => e.CertificateUrl)
                .HasMaxLength(500)
                .HasColumnName("certificate_url");

            entity.Property(e => e.LicenseUrl)
                .HasMaxLength(500)
                .HasColumnName("license_url");

            entity.Property(e => e.IdentityProofUrl)
                .HasMaxLength(500)
                .HasColumnName("identity_proof_url");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.Property(e => e.Reason)
                .HasMaxLength(1000)
                .HasColumnName("reason");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            entity.Property(e => e.ProcessedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("processed_at");

            entity.HasOne(d => d.ExpertProfile)
                .WithMany(p => p.ExpertRequests)
                .HasForeignKey(d => d.ExpertProfileId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Expert_File_expert_profile_id_fkey");
        });

        modelBuilder.Entity<ExpertProfile>(entity =>
        {
            entity.HasKey(e => e.ExpertProfileId).HasName("Expert_Profile_pkey");

            entity.ToTable("Expert_Profile", "public");

            entity.HasIndex(e => e.AccountId, "Expert_Profile_account_id_key").IsUnique();

            entity.Property(e => e.ExpertProfileId).HasColumnName("expert_profile_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Bio)
                    .HasColumnType("text")
                    .HasColumnName("bio");
            entity.Property(e => e.ExpertiseField)
                .HasMaxLength(100)
                .HasColumnName("expertise_field");
            entity.Property(e => e.StyleAesthetic)
                .HasMaxLength(100)
                .HasColumnName("style_aesthetic");
            entity.Property(e => e.YearsOfExperience).HasColumnName("years_of_experience");
            entity.Property(e => e.Verified)
                .HasDefaultValue(false)
                .HasColumnName("verified");
            entity.Property(e => e.RatingAvg).HasColumnName("rating_avg");
            entity.Property(e => e.ReputationScore)
                .HasColumnName("reputation_score");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account).WithOne(p => p.ExpertProfile)
                .HasForeignKey<ExpertProfile>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Expert_Profile_account_id_fkey");
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.FeatureId).HasName("Feature_pkey");

            entity.ToTable("Feature", "public");

            entity.Property(e => e.FeatureId)
                .HasColumnName("feature_id");

            entity.Property(e => e.FeatureCode)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("feature_code");

            entity.HasIndex(e => e.FeatureCode)
                .IsUnique()
                .HasDatabaseName("UQ_Feature_FeatureCode");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.HasMany(d => d.PackageFeatures)
                .WithOne(p => p.Feature)
                .HasForeignKey(p => p.FeatureId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PackageFeature_Feature");
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.FollowerId }).HasName("Follow_pkey");

            entity.ToTable("Follow", "public");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FollowerId).HasColumnName("follower_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.FollowUserNavigations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Follow_user_id_fkey");

            entity.HasOne(d => d.Follower).WithMany(p => p.FollowFollowerNavigations)
                .HasForeignKey(d => d.FollowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Follow_follower_id_fkey");

        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("Group_pkey");

            entity.ToTable("Group", "public");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsGroup).HasColumnName("isGroup");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.LastActivity)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("last_activity");
        });

        modelBuilder.Entity<GroupUser>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.AccountId }).HasName("GroupUser_pkey");

            entity.ToTable("GroupUser", "public");

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
            entity.ToTable("Images", "public");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.AccountAvatarId).HasColumnName("account_avatar_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");

            entity.Property(e => e.ImageUrl).HasMaxLength(500).HasColumnName("image_url");
            entity.Property(e => e.OwnerType).HasMaxLength(50).HasColumnName("owner_type");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");

            entity.HasOne(d => d.Post).WithMany(p => p.Images)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Item).WithMany(p => p.Images)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Account).WithMany(p => p.Avatars)
                .HasForeignKey(d => d.AccountAvatarId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Event).WithMany(p => p.Images)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Group).WithMany(p => p.Images)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("Item_pkey");
            entity.ToTable("Item", "public");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.WardrobeId).HasColumnName("wardrobe_id");

            entity.Property(e => e.ItemName)
                .HasMaxLength(200)
                .HasColumnName("item_name");

            entity.Property(e => e.Category)
                .HasColumnName("category")
                .HasMaxLength(70)
                .HasDefaultValue("unknown");

            entity.Property(e => e.ItemType)
                .HasMaxLength(50)
                .HasColumnName("item_type");

            entity.Property(e => e.SubCategory)
                .HasMaxLength(100)
                .HasColumnName("sub_category");

            entity.Property(e => e.Gender).HasMaxLength(20).HasColumnName("gender");
            entity.Property(e => e.MainColor).HasMaxLength(50).HasColumnName("main_color");
            entity.Property(e => e.SubColor).HasMaxLength(50).HasColumnName("sub_color");
            entity.Property(e => e.Material).HasMaxLength(100).HasColumnName("material");
            entity.Property(e => e.Pattern).HasMaxLength(100).HasColumnName("pattern");
            entity.Property(e => e.Style).HasMaxLength(100).HasColumnName("style");
            entity.Property(e => e.Fit).HasMaxLength(50).HasColumnName("fit");
            entity.Property(e => e.Neckline).HasMaxLength(50).HasColumnName("neckline");
            entity.Property(e => e.SleeveLength).HasMaxLength(50).HasColumnName("sleeve_length");
            entity.Property(e => e.Length).HasMaxLength(50).HasColumnName("length");
            entity.Property(e => e.Brand).HasMaxLength(100).HasColumnName("brand");
            entity.Property(e => e.Description).HasColumnName("description");

            entity.Property(e => e.ItemEmbedding)
                .HasColumnName("item_embedding")
                .HasColumnType("vector(768)");

            entity.HasIndex(e => e.ItemEmbedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasDefaultValue(ItemStatus.Active);

            entity.Property(e => e.IsPublic)
                .HasColumnName("is_public")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdateAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("update_at");

            entity.HasIndex(e => e.WardrobeId)
                .HasDatabaseName("IX_Item_WardrobeId");

            entity.HasIndex(e => e.IsPublic)
                .HasDatabaseName("IX_Item_IsPublic");

            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_Item_Category");

            entity.HasIndex(e => e.Gender)
                .HasDatabaseName("IX_Item_Gender");

            entity.HasIndex(e => new { e.IsPublic, e.Category })
                .HasDatabaseName("IX_Item_IsPublic_Category");

            entity.HasOne(d => d.Wardrobe)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.WardrobeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Item_wardrobe_id_fkey");
        });

        modelBuilder.Entity<SavedItem>(entity =>
        {
            entity.HasKey(e => new { e.AccountId, e.ItemId }).HasName("SavedItem_pkey");

            entity.ToTable("SavedItem", "public");

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");

            entity.Property(e => e.SavedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("saved_at");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.SavedItems)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("SavedItem_account_id_fkey");

            entity.HasOne(d => d.Item)
                .WithMany(p => p.SavedByUsers)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("SavedItem_item_id_fkey");
        });

        modelBuilder.Entity<MessReaction>(entity =>
        {
            entity.HasKey(e => e.ReactId).HasName("MessReaction_pkey");

            entity.ToTable("MessReaction", "public");

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

            entity.ToTable("Message", "public");

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

            entity.ToTable("Notification", "public");

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

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_pkey");

            entity.ToTable("Order", "public");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");

            entity.Property(e => e.SubTotal)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("sub_total");

            entity.Property(e => e.ServiceFee)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .HasColumnName("service_fee");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("total_amount");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsRequired()
                .HasColumnName("status");

            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");

            entity.Property(e => e.ShippingAddress).HasMaxLength(255).HasColumnName("shipping_address");
            entity.Property(e => e.ReceiverName).HasMaxLength(100).HasColumnName("receiver_name");
            entity.Property(e => e.ReceiverPhone).HasMaxLength(20).HasColumnName("receiver_phone");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Buyer)
                .WithMany()
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Order_buyer_id_fkey");

            entity.HasOne(d => d.Seller)
                .WithMany()
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Order_seller_id_fkey");

            entity.HasMany(d => d.OrderDetails)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("OrderDetail_pkey");

            entity.ToTable("OrderDetail", "public");

            entity.Property(e => e.OrderDetailId).HasColumnName("order_detail_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OutfitId).HasColumnName("outfit_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.Property(e => e.Quantity)
                .IsRequired()
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("OrderDetail_order_id_fkey");

            entity.HasOne(d => d.Outfit)
                .WithMany()
                .HasForeignKey(d => d.OutfitId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("OrderDetail_outfit_id_fkey");
        });

        modelBuilder.Entity<Outfit>(entity =>
        {
            entity.HasKey(e => e.OutfitId).HasName("Outfit_pkey");

            entity.ToTable("Outfit", "public");

            entity.Property(e => e.OutfitId).HasColumnName("outfit_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
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

        modelBuilder.Entity<OutfitItem>(entity =>
        {
            entity.HasKey(ei => new { ei.OutfitId, ei.ItemId }).HasName("OutfitItem_pkey");

            entity.ToTable("OutfitItem", "public");

            entity.Property(e => e.OutfitId).HasColumnName("outfit_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Slot).HasMaxLength(50).HasColumnName("slot");

            entity.HasOne(d => d.Outfit)
                .WithMany(p => p.OutfitItems)
                .HasForeignKey(d => d.OutfitId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("OutfitItem_outfit_id_fkey");

            entity.HasOne(d => d.Item)
                .WithMany(p => p.OutfitItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("OutfitItem_item_id_fkey");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("Package_pkey");

            entity.ToTable("Package", "public");

            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("price");

            entity.Property(e => e.DurationDays)
                .HasColumnName("duration_days");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.Packages)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Package_Account");

            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Package)
                .HasForeignKey(p => p.PackageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Subscriptions)
                .WithOne(s => s.Package)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PackageFeatures)
                .WithOne(pf => pf.Package)
                .HasForeignKey(pf => pf.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackageFeature>(entity =>
        {
            entity.HasKey(pf => new { pf.PackageId, pf.FeatureId })
                  .HasName("PackageFeature_pkey");

            entity.ToTable("PackageFeature", "public");

            entity.Property(pf => pf.PackageId)
                .HasColumnName("package_id");

            entity.Property(pf => pf.FeatureId)
                .HasColumnName("feature_id");

            entity.Property(pf => pf.Value)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("value");

            entity.HasOne(pf => pf.Package)
                .WithMany(p => p.PackageFeatures)
                .HasForeignKey(pf => pf.PackageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PackageFeature_Package");

            entity.HasOne(pf => pf.Feature)
                .WithMany(f => f.PackageFeatures)
                .HasForeignKey(pf => pf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PackageFeature_Feature");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("Payment_pkey");

            entity.ToTable("Payment", "public");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Amount)
                .HasColumnType("numeric(18,2)")
                .HasColumnName("amount");
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

            entity.ToTable("Photos", "public");

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

            entity.ToTable("PinnedMessage", "public");

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
            entity.HasKey(e => e.PostId)
                  .HasName("post_pkey");

            entity.ToTable("Post", "public");

            entity.Property(e => e.PostId)
                  .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                  .HasColumnName("account_id");

            entity.Property(e => e.EventId)
                  .HasColumnName("event_id");

            entity.Property(e => e.Title)
                  .HasColumnName("title");

            entity.Property(e => e.Content)
                  .HasColumnName("content");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("updated_at");

            entity.Property(e => e.IsExpertPost)
                  .HasColumnName("is_expert_post");

            entity.Property(e => e.Status)
                  .HasMaxLength(30)
                  .HasDefaultValue(PostStatus.Draft)
                  .HasColumnName("status");

            entity.Property(e => e.Visibility)
                  .HasMaxLength(20)
                  .HasDefaultValue(PostVisibility.Visible)
                  .HasColumnName("visibility");

            entity.Property(e => e.Score)
                  .HasColumnName("score");

            entity.Property(e => e.LikeCount)
                  .HasDefaultValue(0)
                  .HasColumnName("like_count");

            entity.Property(e => e.CommentCount)
                  .HasDefaultValue(0)
                  .HasColumnName("comment_count");

            entity.Property(e => e.ShareCount)
                  .HasDefaultValue(0)
                  .HasColumnName("share_count");

            entity.HasIndex(e => e.AccountId)
                  .HasDatabaseName("ix_post_account_id");

            entity.HasIndex(e => e.EventId)
                  .HasDatabaseName("ix_post_event_id");

            entity.HasOne(d => d.Account)
                  .WithMany(p => p.Posts)
                  .HasForeignKey(d => d.AccountId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("post_account_id_fkey");

            entity.HasOne(d => d.Event)
                  .WithMany(p => p.Posts)
                  .HasForeignKey(d => d.EventId)
                  .HasConstraintName("post_event_id_fkey");
        });

        modelBuilder.Entity<PostSave>(entity =>
        {
            entity.ToTable("PostSaves", "public");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PostId)
                .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                .HasColumnName("account_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasIndex(e => new { e.AccountId, e.PostId })
                .IsUnique()
                .HasDatabaseName("ix_postsaves_account_post");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.Saves)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Account)
                .WithMany(a => a.SavedPosts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostSave>(entity =>
        {
            entity.ToTable("PostSaves", "public");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PostId)
                .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                .HasColumnName("account_id");


            entity.HasIndex(e => new { e.AccountId, e.PostId })
                .IsUnique()
                .HasDatabaseName("ix_postsaves_account_post");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.Saves)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Account)
                .WithMany(a => a.SavedPosts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostVector>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_Vector_pkey");

            entity.ToTable("Post_Vector", "public");

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

        modelBuilder.Entity<PrizeEvent>(entity =>
        {
            entity.HasKey(e => e.PrizeEventId).HasName("PrizeEvent_pkey");

            entity.ToTable("PrizeEvent", "public");

            entity.Property(e => e.PrizeEventId).HasColumnName("prize_event_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EscrowSessionId).HasColumnName("escrow_session_id");

            entity.Property(e => e.Ranked)
                .IsRequired()
                .HasColumnName("ranked");

            entity.Property(e => e.RewardAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("reward_amount");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Event)
                .WithMany(p => p.PrizeEvents)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("PrizeEvent_event_id_fkey");

            entity.HasOne<EscrowSession>()
                .WithMany()
                .HasForeignKey(d => d.EscrowSessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("PrizeEvent_escrow_session_id_fkey");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.ReactionId)
                  .HasName("reaction_pkey");

            entity.ToTable("Reaction", "public");

            entity.Property(e => e.ReactionId)
                  .HasColumnName("reaction_id");

            entity.Property(e => e.PostId)
                  .IsRequired()
                  .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                  .IsRequired()
                  .HasColumnName("account_id");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("NOW()")
                  .HasColumnName("created_at");

            entity.HasIndex(e => new { e.AccountId, e.PostId })
                  .IsUnique()
                  .HasDatabaseName("ux_reaction_account_post");

            entity.HasIndex(e => e.PostId)
                  .HasDatabaseName("ix_reaction_post_id");

            entity.HasIndex(e => e.AccountId)
                  .HasDatabaseName("ix_reaction_account_id");

            entity.HasOne(d => d.Post)
                  .WithMany(p => p.Reactions)
                  .HasForeignKey(d => d.PostId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("reaction_post_id_fkey");

            entity.HasOne(d => d.Account)
                  .WithMany(p => p.Reactions)
                  .HasForeignKey(d => d.AccountId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("reaction_account_id_fkey");
        });

        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.HasKey(e => e.CommentReactionId)
                  .HasName("comment_reaction_pkey");

            entity.ToTable("CommentReaction", "public");

            entity.Property(e => e.CommentReactionId)
                  .HasColumnName("comment_reaction_id");

            entity.Property(e => e.CommentId)
                  .IsRequired()
                  .HasColumnName("comment_id");

            entity.Property(e => e.AccountId)
                  .IsRequired()
                  .HasColumnName("account_id");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("NOW()")
                  .HasColumnName("created_at");

            entity.HasIndex(e => new { e.AccountId, e.CommentId })
                  .IsUnique()
                  .HasDatabaseName("ux_comment_reaction_account_comment");

            entity.HasIndex(e => e.CommentId)
                  .HasDatabaseName("ix_comment_reaction_comment_id");

            entity.HasIndex(e => e.AccountId)
                  .HasDatabaseName("ix_comment_reaction_account_id");

            entity.HasOne(d => d.Comment)
                  .WithMany(p => p.Reactions)
                  .HasForeignKey(d => d.CommentId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_reaction_comment_id_fkey");

            entity.HasOne(d => d.Account)
                  .WithMany()
                  .HasForeignKey(d => d.AccountId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("comment_reaction_account_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("RefreshToken_pkey");
            entity.ToTable("RefreshToken", "public");

            entity.HasIndex(e => e.AccountId, "RefreshToken_account_id_idx");

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

            entity.HasOne(d => d.Account)
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("RefreshToken_account_id_fkey");
        });

        modelBuilder.Entity<ReportType>(entity =>
        {
            entity.HasKey(e => e.ReportTypeId).HasName("Report_Type_pkey");

            entity.ToTable("Report_Type", "public");

            entity.HasIndex(e => e.TypeName, "Report_Type_type_name_key")
                .IsUnique();

            entity.Property(e => e.ReportTypeId)
                .HasColumnName("report_type_id");

            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .HasColumnName("type_name");

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
        });

        modelBuilder.Entity<ReputationHistory>(entity =>
        {
            entity.HasKey(e => e.ReputationHistoryId).HasName("Reputation_History_pkey");

            entity.ToTable("Reputation_History", "public");

            entity.Property(e => e.ReputationHistoryId).HasColumnName("reputation_history_id");

            entity.Property(e => e.ExpertProfileId).HasColumnName("expert_profile_id");

            entity.Property(e => e.PointChange).HasColumnName("point_change");

            entity.Property(e => e.CurrentPoint).HasColumnName("current_point");

            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.ExpertProfile)
                .WithMany(p => p.ReputationHistories)
                .HasForeignKey(d => d.ExpertProfileId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Reputation_History_expert_profile_id_fkey");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("SystemSettings_pkey");

            entity.ToTable("SystemSettings", "public");

            entity.Property(e => e.SettingKey)
                .HasMaxLength(50)
                .HasColumnName("setting_key");

            entity.Property(e => e.SettingValue)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("setting_value");

            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .HasColumnName("data_type");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("Transaction_pkey");
            entity.ToTable("Transaction", "public");

            entity.HasIndex(e => e.PaymentId, "Transaction_payment_id_key").IsUnique();

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("amount");

            entity.Property(e => e.BalanceBefore)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("balance_before");

            entity.Property(e => e.BalanceAfter)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("balance_after");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");

            entity.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .HasColumnName("reference_type");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");

            entity.HasOne(d => d.Wallet)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Transaction_wallet_id_fkey");

            entity.HasOne(d => d.Payment)
                .WithOne(p => p.Transaction)
                .HasForeignKey<Transaction>(d => d.PaymentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("Transaction_payment_id_fkey");
        });

        modelBuilder.Entity<UserProfileVector>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("User_Profile_Vector_pkey");

            entity.ToTable("User_Profile_Vector", "public");

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

            entity.ToTable("User_Report", "public");

            entity.HasIndex(e => new { e.PostId, e.AccountId }, "User_Report_post_id_account_id_key")
                .IsUnique();

            entity.Property(e => e.UserReportId).HasColumnName("user_report_id");

            entity.Property(e => e.PostId)
                .HasColumnName("post_id");

            entity.Property(e => e.AccountId)
                .HasColumnName("account_id");

            entity.Property(e => e.ReportTypeId)
                .HasColumnName("report_type_id");

            entity.Property(e => e.Reason)
                .HasColumnName("reason");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue(ReportStatus.Pending)
                .HasColumnName("status");

            entity.Property(e => e.ReviewedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("reviewed_at");

            entity.Property(e => e.ReviewedBy)
                .HasColumnName("reviewed_by");

            entity.Property(e => e.AdminNote)
                .HasMaxLength(1000)
                .HasColumnName("admin_note");

            entity.HasOne(d => d.Account)
                .WithMany(p => p.UserReports)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("User_Report_account_id_fkey");

            entity.HasOne(d => d.Post)
                .WithMany(p => p.UserReports)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("User_Report_post_id_fkey");

            entity.HasOne(d => d.ReportType)
                .WithMany(p => p.UserReports)
                .HasForeignKey(d => d.ReportTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("User_Report_report_type_id_fkey");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("Wallet_pkey");

            entity.ToTable("Wallet", "public");

            entity.HasIndex(e => e.AccountId, "Wallet_account_id_key").IsUnique();

            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");

            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .HasColumnName("balance");

            entity.Property(e => e.LockedBalance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .HasColumnName("locked_balance");

            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND")
                .HasColumnName("currency");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account)
                .WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Wallet_account_id_fkey");
        });

        modelBuilder.Entity<Wardrobe>(entity =>
        {
            entity.HasKey(e => e.WardrobeId).HasName("Wardrobe_pkey");

            entity.ToTable("Wardrobe", "public");

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

        modelBuilder.Entity<TryOnHistory>(entity => {
            entity.ToTable("TryOnHistory", "public");
            entity.HasKey(e => e.TryOnId);
            entity.Property(e => e.TryOnId).HasColumnName("tryon_id");
            entity.Property(e => e.AccountId).HasColumnName("acc_id");
            entity.Property(e => e.ImageUrl).HasColumnName("img_url").HasMaxLength(500);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnName("create_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithMany(p => p.TryOnHistories).HasForeignKey(d => d.AccountId);
        });

        modelBuilder.Entity<Scoreboard>(entity =>
        {
            entity.ToTable("Scoreboard", "public");
            entity.HasKey(e => e.ScoreboardId).HasName("Scoreboard_pkey");

            entity.HasIndex(e => e.PostId).IsUnique();

            entity.Property(e => e.ScoreboardId).HasColumnName("scoreboard_id");
            entity.Property(e => e.PostId).HasColumnName("post_id");

            entity.Property(e => e.FinalLikeCount).HasDefaultValue(0).HasColumnName("final_like_count");
            entity.Property(e => e.FinalShareCount).HasDefaultValue(0).HasColumnName("final_share_count");

            entity.Property(e => e.ExpertScore).HasDefaultValue(0.0).HasColumnName("expert_score");
            entity.Property(e => e.ExpertReason)
                .HasMaxLength(1000)
                .HasColumnName("expert_reason");
            entity.Property(e => e.CommunityScore).HasDefaultValue(0.0).HasColumnName("community_score");
            entity.Property(e => e.FinalScore).HasDefaultValue(0.0).HasColumnName("final_score");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.HasOne(d => d.Post)
                .WithOne(p => p.Scoreboard)
                .HasForeignKey<Scoreboard>(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Scoreboard_post_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
