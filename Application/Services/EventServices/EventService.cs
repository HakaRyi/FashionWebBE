using Application.Interfaces;
using Application.Jobs;
using Application.Request.EventReq;
using Application.Response.DashboardResp;
using Application.Response.EventResp;
using Application.Response.PostResp;
using Application.Services.NotificationImp;
using Application.Utils;
using Application.Utils.File;
using Domain.Constants;
using Domain.Dto.Social.Post;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Quartz;

namespace Application.Services.EventServices
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IPostRepository _postRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly INotificationService _notificationService;
        private readonly IImageRepository _imageRepo;
        private readonly UserManager<Account> _userManager;
        private readonly ICloudStorageService _storage;

        private const int MAX_IMAGES = 5;

        public EventService(
            IEventRepository eventRepo,
            IWalletRepository walletRepo,
            IEventExpertRepository eventExpertRepo,
            IPostRepository postRepo,
            ITransactionRepository transactionRepo,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUserService,
            UserManager<Account> userManager,
            IImageRepository imageRepo,
            ICloudStorageService storage)
        {
            _eventRepo = eventRepo;
            _walletRepo = walletRepo;
            _eventExpertRepo = eventExpertRepo;
            _postRepo = postRepo;
            _transactionRepo = transactionRepo;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _schedulerFactory = schedulerFactory;
            _userManager = userManager;
            _imageRepo = imageRepo;
            _storage = storage;
        }

        #region User Join Event
        public async Task<PostResponse> JoinEventByPostAsync(int accountId, CreatePostDto dto)
        {
            if (!dto.EventId.HasValue) throw new Exception("Missing EventId to participate in the event.");

            var ev = await _eventRepo.GetByIdAsync(dto.EventId.Value);

            if (ev == null) throw new Exception("The event does not exist.");

            var isJoined = await _postRepo.AnyAsync(p => p.AccountId == accountId && p.EventId == dto.EventId);
            if (isJoined) throw new Exception("You have already participated in this event.");

            if (ev.Status != "Active" ||
               (ev.SubmissionDeadline.HasValue && ev.SubmissionDeadline.Value.ToUniversalTime() < DateTime.UtcNow))
            {
                throw new Exception("The event is no longer open for participation.");
            }

            var imageUrls = dto.Images != null ? await UploadImages(dto.Images.ToList()) : new List<string>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (ev.EntryFee > 0)
                {
                    var userWallet = await _walletRepo.GetByAccountIdAsync(accountId);
                    if (userWallet == null || userWallet.Balance < ev.EntryFee)
                        throw new Exception($"Insufficient wallet balance. Participation fee applies: {ev.EntryFee:N0} VNĐ.");

                    var creatorWallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                    if (creatorWallet == null) throw new Exception("Organizer's wallet not found.");

                    decimal userBalanceBefore = userWallet.Balance;
                    userWallet.Balance -= ev.EntryFee;
                    _walletRepo.Update(userWallet);

                    decimal creatorLockedBefore = creatorWallet.LockedBalance;
                    creatorWallet.LockedBalance += ev.EntryFee;
                    _walletRepo.Update(creatorWallet);

                    // 3. Ghi log giao dịch (Loại: Tham gia sự kiện - Chờ xử lý)
                    await _transactionRepo.AddAsync(new Transaction
                    {
                        TransactionCode = $"JOIN_PAY_{ev.EventId}_{accountId}_{DateTime.UtcNow.Ticks}",
                        WalletId = userWallet.WalletId,
                        Amount = -ev.EntryFee,
                        BalanceBefore = userBalanceBefore,
                        BalanceAfter = userWallet.Balance,
                        Type = "Event_Entry_Fee_Paid",
                        Status = "Success",
                        Description = $"Paid entry fee for event: {ev.Title}",
                        ReferenceId = ev.EventId,
                        ReferenceType = "Event",
                        CreatedAt = DateTime.UtcNow
                    });

                    await _transactionRepo.AddAsync(new Transaction
                    {
                        TransactionCode = $"JOIN_REVENUE_LOCKED_{ev.EventId}_{accountId}_{DateTime.UtcNow.Ticks}",
                        WalletId = creatorWallet.WalletId,
                        Amount = ev.EntryFee,
                        BalanceBefore = creatorLockedBefore,
                        BalanceAfter = creatorWallet.LockedBalance,
                        Type = "Event_Revenue_Locked",
                        Status = "Success",
                        Description = $"Entry fee for event '{ev.Title}' from User #{accountId} is being held by the system.",
                        ReferenceId = ev.EventId,
                        ReferenceType = "Event",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // --- LOGIC TẠO POST ---
                var now = DateTime.UtcNow;

                var post = new Post
                {
                    AccountId = accountId,
                    Title = dto.Title?.Trim(),
                    Content = dto.Content?.Trim(),
                    EventId = dto.EventId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = PostStatus.Published,
                    Visibility = PostVisibility.Visible,
                    LikeCount = 0,
                    CommentCount = 0,
                    ShareCount = 0
                };

                await _postRepo.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                var images = imageUrls.Select(url => new Image
                {
                    PostId = post.PostId,
                    ImageUrl = url,
                    OwnerType = "Post",
                    CreatedAt = now
                }).ToList();

                await _imageRepo.AddRangeAsync(images);

                var account = await _userManager.FindByIdAsync(accountId.ToString());
                if (account != null)
                {
                    account.CountPost += 1;
                    await _userManager.UpdateAsync(account);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                post.Images = images;
                return MapToResponse(post);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Event participation error: {ex.Message}");
            }
        }

        private PostResponse MapToResponse(Post post)
        {
            var currentUserId = _currentUserService.GetUserId();

            return new PostResponse
            {
                PostId = post.PostId,
                AccountId = post.AccountId,

                UserName = post.Account?.UserName,
                AvatarUrl = post.Account?.Avatars?
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault()?.ImageUrl,

                EventId = post.EventId,
                EventName = post.Event?.Title,

                Title = post.Title,
                Content = post.Content,

                ImageUrls = post.Images?
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList() ?? new List<string>(),

                IsExpertPost = post.IsExpertPost ?? false,

                IsLikedByExpert = post.Reactions.Any(r =>
                    r.Account != null &&
                    r.Account.ExpertProfile != null &&
                    r.Account.ExpertProfile.Verified == true
                ),

                Status = post.Status,

                IsLiked = currentUserId.HasValue &&
                          post.Reactions.Any(r => r.AccountId == currentUserId.Value),

                IsSaved = currentUserId.HasValue &&
                          post.Saves.Any(s => s.AccountId == currentUserId.Value),

                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                ShareCount = post.ShareCount,

                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        private async Task<List<string>> UploadImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new Exception("Post must contain at least one image.");

            if (files.Count > MAX_IMAGES)
                throw new Exception("Maximum 5 images allowed.");

            var tasks = files.Select(f => _storage.UploadImageAsync(f));
            return (await Task.WhenAll(tasks)).ToList();
        }
        #endregion

        #region Admin Workflow
        public async Task ApproveEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Pending_Review")
                throw new Exception("Sự kiện không tồn tại hoặc đã được xử lý.");

            int adminId = _currentUserService.GetRequiredUserId();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                ev.Status = "Inviting";
                _eventRepo.Update(ev);

                var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
                var expertsToNotify = experts.Where(e => e.Status == "Awaiting_Review").ToList();

                foreach (var exp in experts.Where(e => e.Status == "Awaiting_Review"))
                {
                    exp.Status = "Pending";
                    _eventExpertRepo.Update(exp);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                foreach (var exp in expertsToNotify)
                {
                    await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                    {
                        SenderId = adminId,
                        TargetUserId = exp.ExpertId,
                        Title = "Invitation to a new event",
                        Content = $"You have been invited to be an expert for the event: {ev.Title}. Please check and respond.",
                        Type = "Event_Invitation",
                        RelatedId = eventId.ToString()
                    });
                }

                await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                {
                    SenderId = adminId,
                    TargetUserId = ev.CreatorId,
                    Title = "The event has been approved.",
                    Content = $"Event '{ev.Title}' has been approved by the admin and is now sending invitations to experts.",
                    Type = "Event_Approved",
                    RelatedId = eventId.ToString()
                });

                if (ev.IsAutoStart)
                {
                    await ScheduleEventActivation(ev);
                }
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin từ chối sự kiện: Hoàn tiền từ LockedBalance về Balance và cập nhật trạng thái
        /// </summary>
        public async Task RejectEventAsync(int eventId, string reason)
        {
            // 1. Lấy thông tin sự kiện và kiểm tra điều kiện
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new KeyNotFoundException("Không tìm thấy sự kiện.");

            if (ev.Status != "Pending_Review")
                throw new InvalidOperationException("Chỉ có thể từ chối sự kiện đang ở trạng thái 'Chờ duyệt'.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Vui lòng cung cấp lý do từ chối.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3. Tính toán số tiền cần hoàn lại
                // Refund = Phí áp dụng + Tổng các giải thưởng đã nạp
                decimal totalPrizePool = ev.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0;
                decimal totalToRefund = totalPrizePool + ev.AppliedFee;

                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                if (wallet == null)
                    throw new Exception("Không tìm thấy ví của người tạo sự kiện.");

                if (wallet.LockedBalance < totalToRefund)
                    throw new Exception("Số dư bị khóa không đủ để thực hiện hoàn tiền (Lỗi logic dữ liệu).");

                decimal balanceBefore = wallet.Balance;

                // Chuyển tiền từ 'Bị khóa' về lại 'Số dư khả dụng'
                wallet.LockedBalance -= totalToRefund;
                wallet.Balance += totalToRefund;

                ev.Status = "Rejected";
                ev.Note = reason;

                var refundTransaction = new Domain.Entities.Transaction
                {
                    WalletId = wallet.WalletId,
                    TransactionCode = $"REFUND_EV_{ev.EventId}_{DateTime.UtcNow.Ticks}",
                    Amount = totalToRefund,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Refund",
                    ReferenceType = "Event_Reject",
                    ReferenceId = ev.EventId,
                    Description = $"Refund for rejected event: {ev.Title}. Reason: {reason}",
                    Status = "Success",
                    CreatedAt = DateTime.Now
                };

                _eventRepo.Update(ev);
                _walletRepo.Update(wallet);
                await _transactionRepo.AddAsync(refundTransaction);

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                {
                    SenderId = _currentUserService.GetRequiredUserId(),
                    TargetUserId = ev.CreatorId,
                    Title = "The event was rejected.",
                    Content = $"Event '{ev.Title}' was not approved. Reason: {reason}. The money has been refunded to your wallet.",
                    Type = "Event_Rejected",
                    RelatedId = eventId.ToString()
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi từ chối sự kiện: {ex.Message}");
            }
        }

        private async Task ScheduleEventActivation(Event ev)
        {
            // 1. Lấy bộ lập lịch từ Factory (đã inject ở Constructor)
            var scheduler = await _schedulerFactory.GetScheduler();

            // 2. Định nghĩa Job và truyền dữ liệu (EventId) vào JobDataMap
            var job = JobBuilder.Create<ActivateEventJob>()
                .WithIdentity($"Job_Activate_{ev.EventId}", "EventGroup")
                .WithDescription($"Kích hoạt sự kiện: {ev.Title} (ID: {ev.EventId})")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            // 3. Tạo Trigger để xác định thời điểm chạy
            // Sử dụng StartTime của Event, nếu không có thì mặc định chạy sau 30 giây
            var startTimeRaw = ev.StartTime ?? DateTime.Now.AddSeconds(30);
            DateTime processTime = startTimeRaw.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(startTimeRaw, DateTimeKind.Local)
                : startTimeRaw;

            DateTimeOffset startTimeOffset = new DateTimeOffset(processTime);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Activate_{ev.EventId}", "EventGroup")
                .WithDescription($"Lịch kích hoạt cho sự kiện '{ev.Title}'")
                .StartAt(startTimeOffset) // Truyền Offset vào đây
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        public async Task<bool> UpdateEventByAdmin(int eventId, UpdateEventRequestAdmin dto)
        {
            var existingEvent = await _eventRepo.GetByIdAsync(eventId);
            if (existingEvent == null)
            {
                return false;
            }

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartTime = dto.StartTime;
            existingEvent.SubmissionDeadline = dto.SubmissionDeadline;
            existingEvent.EndTime = dto.EndTime;

            _eventRepo.Update(existingEvent);

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }
        #endregion

        #region Get Methods
        public async Task<IEnumerable<PostReviewDto>> GetPostsForReviewAsync(int eventId)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            // Đảm bảo PostRepo.GetPostsByEventIdAsync đã .Include(p => p.ExpertRatings)
            var posts = await _postRepo.GetPostsByEventIdAsync(eventId);

            return posts.Select(p =>
            {
                // Tìm bản ghi chấm điểm của Expert hiện tại
                var myRating = p.ExpertRatings?.FirstOrDefault(r => r.ExpertId == currentExpertId);

                return new PostReviewDto
                {
                    PostId = p.PostId,
                    Title = p.Title ?? "Untitled",
                    Content = p.Content,
                    ImageUrl = p.Images.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
                    AuthorName = p.Account?.UserName,

                    // Thông tin chấm điểm
                    CurrentScore = myRating?.Score,
                    MyReason = myRating?.Reason,
                    IsGraded = myRating != null,

                    // Thông số khác
                    LikeCount = p.LikeCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,
                    SubmittedAt = p.CreatedAt ?? DateTime.Now
                };
            }).ToList();
        }

        public async Task<EventAnalyticsRawResponse> GetAnalyticsAsync(string period)
        {
            int expertId = _currentUserService.GetRequiredUserId();

            int days;
            string numericPart = new string(period.Where(char.IsDigit).ToArray());

            if (!int.TryParse(numericPart, out days))
            {
                days = 30;
            }

            DateTime startDate = DateTime.UtcNow.AddDays(-days);

            var events = await _eventRepo.GetAnalyticsDataAsync(expertId, startDate);

            var response = new EventAnalyticsRawResponse
            {
                ExpertId = expertId,
                Events = events.Select(e => new EventRawDto
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    AppliedFee = e.AppliedFee,
                    EntryFee = e.EntryFee,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt ?? DateTime.MinValue,
                    Posts = e.Posts.Select(p => new PostRawDto
                    {
                        PostId = p.PostId,
                        LikeCount = p.LikeCount ?? 0,
                        ShareCount = p.ShareCount ?? 0,
                        CommentCount = p.CommentCount ?? 0,
                        CreatedAt = p.CreatedAt ?? DateTime.MinValue,
                        IsRatedByExpert = p.ExpertRatings.Any(er => er.ExpertId == expertId)
                    }).ToList()
                }).ToList()
            };

            return response;
        }

        /// <summary>
        /// Expert xem danh sách các sự kiện do chính họ tạo ra (Tất cả trạng thái)
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetMyCreatedEventsAsync()
        {
            int expertId = _currentUserService.GetRequiredUserId();

            var events = await _eventRepo.GetAllByCreatorIdAsync(expertId);

            return events.Adapt<IEnumerable<EventListDto>>();
        }

        /// <summary>
        /// Lấy chi tiết một Event kèm theo các Prize và Expert liên quan
        /// </summary>
        public async Task<EventDetailDto?> GetEventDetailsAsync(int eventId)
        {
            int currentUserId = _currentUserService.GetUserId() ?? 0;

            var e = await _eventRepo.GetByIdAsync(eventId);
            if (e == null) return null;

            var dto = e.Adapt<EventDetailDto>();
            bool isCreator = currentUserId == e.CreatorId;

            dto.IsJoined = currentUserId != 0 && e.Posts.Any(p => p.AccountId == currentUserId);
            dto.AcceptedExpertsCount = e.EventExperts?.Count(ex => ex.Status == "Accepted") ?? 0;

            if (isCreator)
            {
                bool hasEnoughExperts = dto.AcceptedExpertsCount >= e.MinExpertsToStart;
                dto.CanManualStart = e.Status == "Inviting" && hasEnoughExperts;

                if (e.Status == "Inviting")
                {
                    if (!hasEnoughExperts)
                    {
                        dto.ReasonManualStart = $"Need at least {e.MinExpertsToStart} accepted experts to start (Current: {dto.AcceptedExpertsCount}).";
                        dto.CanManualStart = false;
                    }
                    else if (dto.StartTime.HasValue)
                    {
                        var nowUtc = DateTime.UtcNow;
                        var startTimeUtc = dto.StartTime.Value.ToUniversalTime();
                        var timeUntilStart = startTimeUtc - nowUtc;
                        double maxEarlyHours = 24.0;

                        // 2. Kiểm tra nếu là AutoStart
                        if (e.IsAutoStart)
                        {
                            if (nowUtc >= startTimeUtc)
                            {
                                dto.ReasonManualStart = "This is an Auto-Start event. System is processing, please wait...";
                                dto.CanManualStart = false;
                            }
                            else
                            {
                                dto.ReasonManualStart = "Event is scheduled for Auto-Start.";
                            }
                        }

                        // 3. Kiểm tra giới hạn bắt đầu sớm (24h)
                        if (timeUntilStart.TotalHours > maxEarlyHours)
                        {
                            dto.ReasonManualStart = $"Too early to start. Max early start is {maxEarlyHours} hours before scheduled time.";
                            dto.CanManualStart = false;
                        }
                        // 4. Kiểm tra giới hạn "ngâm" quá lâu (12h) - Chỉ áp dụng cho Manual Start
                        else if (!e.IsAutoStart && nowUtc > startTimeUtc.AddHours(12))
                        {
                            dto.ReasonManualStart = "Scheduled start time passed by 12+ hours. Activation is no longer allowed.";
                            dto.CanManualStart = false;
                        }
                        else
                        {
                            dto.ReasonManualStart = "The event is ready to start now.";
                        }
                    }
                }

                dto.CanFinalize = (e.Status == "Active" || e.Status == "Judging") &&
                                   e.EndTime.HasValue &&
                                   DateTime.UtcNow >= e.EndTime.Value.ToUniversalTime();

                dto.IsCreator = isCreator;
                dto.ReasonRejectEvent = e.Note;
            }
            else
            {
                dto.AppliedFee = 0;
                dto.Experts = dto.Experts.Where(ex => ex.Status == "Accepted").ToList();
                dto.CanManualStart = false;
                dto.CanFinalize = false;
                dto.IsCreator = false;
            }

            return dto;
        }

        /// <summary>
        /// DÀNH CHO USER: Chỉ thấy sự kiện đang mời, đang chạy hoặc đã xong
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync()
        {
            // Lọc ngay tại SQL thông qua statuses parameter
            var publicStatuses = new[] { "Active", "Completed" };
            //var publicStatuses = new[] { "Inviting", "Active", "Completed" };
            var events = await _eventRepo.GetAllAsync(publicStatuses);

            return events.Select(MapToEventListDto);
        }

        /// <summary>
        /// DÀNH CHO EXPERT: Thấy tổng hợp (Tạo HOẶC Mời)
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetAllEventsForExpertAsync()
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();
            var events = await _eventRepo.GetExpertRelatedEventsAsync(currentExpertId);

            return events.Select(MapToEventListDto);
        }

        /// <summary>
        /// DÀNH CHO ADMIN: Thấy TẤT CẢ trạng thái
        /// </summary>
        public async Task<IEnumerable<EventAdminListDto>> GetAllEventsForAdminAsync()
        {
            // statuses = null để lấy tất cả
            var events = await _eventRepo.GetAllAsync();

            return events.Select(e => new EventAdminListDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Status = e.Status,
                Note = e.Note,
                CreatorId = e.CreatorId,
                CreatorName = e.Creator?.UserName,
                CreatorEmail = e.Creator?.Email,
                AppliedFee = e.AppliedFee,
                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                MinExperts = e.MinExpertsToStart,
                CurrentAcceptedExperts = e.EventExperts?.Count(ee => ee.Status == "Accepted") ?? 0,
                StartTime = e.StartTime,
                SubmissionDeadline = e.SubmissionDeadline,
                EndTime = e.EndTime,
                CreatedAt = e.CreatedAt,
                ParticipantCount = e.Posts?.Count ?? 0,
                ThumbnailUrl = e.Images?.FirstOrDefault()?.ImageUrl
            });
        }

        private EventListDto MapToEventListDto(Event e)
        {
            // Lấy UserId của người đang đăng nhập (nếu có)
            int? currentUserId = _currentUserService.GetUserId();

            return new EventListDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Status = e.Status,
                StartTime = e.StartTime,
                SubmissionDeadline = e.SubmissionDeadline,
                EndTime = e.EndTime,
                CreatedAt = e.CreatedAt,
                CreatorId = e.CreatorId,
                EntryFee = e.EntryFee,
                CreatorName = e.Creator?.UserName,
                CreatorAvatarUrl = e.Creator?.Avatars.OrderByDescending(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault() ?? null,


                ParticipantCount = e.Posts?.Count ?? 0,

                ThumbnailUrl = e.Images?.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,

                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                Prizes = e.PrizeEvents?
                    .OrderBy(p => p.Ranked)
                    .Select(p => new PrizeBriefDto
                    {
                        Ranked = p.Ranked,
                        RewardAmount = p.RewardAmount
                    }).ToList() ?? new List<PrizeBriefDto>(),

                IsJoined = currentUserId.HasValue && e.Posts.Any(p => p.AccountId == currentUserId.Value),

                MyExpertStatus = currentUserId.HasValue
                    ? e.EventExperts?.FirstOrDefault(ee => ee.ExpertId == currentUserId.Value)?.Status
                    : null
            };
        }

        public async Task<List<EventLeaderboardDto>> GetEventLeaderboardAsync(int eventId)
        {
            var eventDetail = await _eventRepo.GetByIdAsync(eventId);
            var scores = await _eventRepo.GetLeaderboardAsync(eventId);
            return scores.Select((s, index) =>
            {
                int rank = index + 1;
                var prize = eventDetail?.PrizeEvents?.FirstOrDefault(p => p.Ranked == rank);

                return new EventLeaderboardDto
                {
                    Rank = rank,
                    AccountId = s.Post.AccountId,
                    UserName = s.Post.Account.UserName ?? "Anonymous",
                    AvatarUrl = s.Post.Account.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    FinalScore = s.FinalScore,
                    PostId = s.PostId,
                    RewardAmount = prize?.RewardAmount
                };
            }).ToList();
        }

        public async Task<MyEventResultDetailDto?> GetMyResultDetailAsync(int eventId)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            var myScore = await _eventRepo.GetUserScoreAsync(eventId, userId);
            if (myScore == null) return null;

            var leaderboard = await GetEventLeaderboardAsync(eventId);
            int myRank = leaderboard.FirstOrDefault(x => x.AccountId == userId)?.Rank ?? 0;

            var ratings = await _eventRepo.GetExpertRatingsForPostAsync(myScore.PostId);
            var reactions = await _eventRepo.GetPostVotersAsync(myScore.PostId);

            // Lấy ảnh bài post của chính mình
            var post = await _postRepo.GetByIdAsync(myScore.PostId);

            return new MyEventResultDetailDto
            {
                Rank = myRank,
                MyScore = myScore.FinalScore,
                MyPostImageUrl = post?.Images.FirstOrDefault()?.ImageUrl,
                ExpertReviews = ratings.Select(r => new ExpertReviewDto
                {
                    ExpertName = r.Expert.UserName ?? "Expert",
                    ExpertAvatar = r.Expert.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    Score = r.Score,
                    Reason = r.Reason
                }).ToList(),
                Voters = reactions.Select(re => new VoterDto
                {
                    UserName = re.Account.UserName ?? "Voter",
                    AvatarUrl = re.Account.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    VotedAt = re.CreatedAt ?? DateTime.Now
                }).ToList()
            };
        }
        #endregion
    }
}