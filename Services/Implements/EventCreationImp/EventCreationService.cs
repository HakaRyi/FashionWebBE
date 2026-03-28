using Mapster;
using Quartz;
using Repositories.Entities;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.EventWinnerRepos;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.Repos.SystemSettingRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Jobs;
using Services.Request.EventReq;
using Services.Request.PrizeReq;
using Services.Utils.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.EventCreationImp
{
    public class EventCreationService: IEventCreationService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IPrizeEventRepository _prizeRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IExpertRatingRepository _ratingRepo;
        private readonly IScoreboardRepository _scoreboardRepo;
        private readonly IPostRepository _postRepo;
        private readonly IEventWinnerRepository _winnerRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IImageRepository _imageRepo;
        private readonly IFileService _fileService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ISystemSettingRepository _settingRepo;

        public EventCreationService(
            IEventRepository eventRepo,
            IWalletRepository walletRepo,
            IPrizeEventRepository prizeRepo,
            ITransactionRepository transactionRepo,
            IEscrowSessionRepository escrowRepo,
            IEventExpertRepository eventExpertRepo,
            IExpertRatingRepository ratingRepo,
            IScoreboardRepository scoreboardRepo,
            IPostRepository postRepo,
            IEventWinnerRepository winnerRepo,
            IUnitOfWork unitOfWork,
            IImageRepository imageRepo,
            ISystemSettingRepository settingRepo,
            IFileService fileService,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUserService)
        {
            _eventRepo = eventRepo;
            _walletRepo = walletRepo;
            _prizeRepo = prizeRepo;
            _transactionRepo = transactionRepo;
            _escrowRepo = escrowRepo;
            _eventExpertRepo = eventExpertRepo;
            _ratingRepo = ratingRepo;
            _scoreboardRepo = scoreboardRepo;
            _postRepo = postRepo;
            _winnerRepo = winnerRepo;
            _settingRepo = settingRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _imageRepo = imageRepo;
            _fileService = fileService;
            _schedulerFactory = schedulerFactory;
        }

        public async Task<Event> CreateEventAsync(CreateEventRequest dto)
        {
            int creatorId = _currentUserService.GetRequiredUserId();
            ValidateEventRequest(dto);

            decimal currentFee = await _settingRepo.GetDecimalValueAsync("EventCreationFee", 10000);
            var totalPrize = dto.Prizes.Sum(p => p.RewardAmount);

            var wallet = await _walletRepo.GetByAccountIdAsync(creatorId);
            if (wallet == null || wallet.Balance < (totalPrize + currentFee))
                throw new Exception($"Số dư ví không đủ. Cần {(totalPrize + currentFee):N0} VNĐ (bao gồm phí tạo).");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                string? imageUrl = dto.ImageFile != null ? await _fileService.UploadAsync(dto.ImageFile) : null;

                var eventData = dto.Adapt<Event>();

                eventData.MinExpertsToStart = dto.MinExpertsRequired;
                eventData.CreatorId = creatorId;
                eventData.AppliedFee = currentFee;
                eventData.Status = "Pending_Review";
                eventData.CreatedAt = DateTime.UtcNow;

                if (imageUrl != null)
                {
                    eventData.Images.Add(new Image
                    {
                        ImageUrl = imageUrl,
                        OwnerType = "Event_Thumbnail",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _eventRepo.AddAsync(eventData);
                await _unitOfWork.SaveChangesAsync();

                await CreatePrizesAsync(eventData.EventId, dto.Prizes);
                await SetupExpertPanelAsync(eventData.EventId, creatorId, dto.InvitedExpertIds, isDraft: true);

                decimal totalToLock = totalPrize + currentFee;
                wallet.Balance -= totalToLock;
                wallet.LockedBalance += totalToLock;
                _walletRepo.Update(wallet);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                return eventData;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi tạo Event: {ex.Message}");
            }
        }

        public async Task ActivateEventWithEscrowAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Inviting") return;

            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // TRƯỜNG HỢP 1: Thất bại - Không đủ Expert tham gia
                if (acceptedCount < ev.MinExpertsToStart)
                {
                    decimal totalToRefund = totalPrizeAmount + ev.AppliedFee;
                    wallet.LockedBalance -= totalToRefund;
                    wallet.Balance += totalToRefund;

                    ev.Status = "Cancelled_NotEnoughExperts";
                    _eventRepo.Update(ev);
                    _walletRepo.Update(wallet);
                }
                // TRƯỜNG HỢP 2: Thành công - Kích hoạt và Ký quỹ
                else
                {
                    await CollectSystemFeeAsync(wallet, ev);
                    await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

                    ev.Status = "Active";
                    _eventRepo.Update(ev);

                    await ScheduleEventFinalization(ev);

                }

                await _unitOfWork.SaveChangesAsync();


                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        private async Task CollectSystemFeeAsync(Wallet expertWallet, Event ev)
        {
            int adminAccountId = await _settingRepo.GetIntValueAsync("SystemAdminAccountId", 1);
            var adminWallet = await _walletRepo.GetByAccountIdAsync(adminAccountId);

            if (adminWallet == null) throw new Exception("Không tìm thấy ví hệ thống.");

            // Trừ từ tiền đã khóa của Expert
            expertWallet.LockedBalance -= ev.AppliedFee;

            decimal adminBefore = adminWallet.Balance;
            adminWallet.Balance += ev.AppliedFee;

            _walletRepo.Update(expertWallet);
            _walletRepo.Update(adminWallet);

            // Log giao dịch doanh thu cho Admin
            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = adminWallet.WalletId,
                Amount = ev.AppliedFee,
                BalanceBefore = adminBefore,
                BalanceAfter = adminWallet.Balance,
                Type = "System_Fee_Revenue",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                CreatedAt = DateTime.Now
            });
        }

        private async Task ProcessEscrowFromLockedAsync(int eventId, int expertId, decimal amount, Wallet wallet)
        {
            // Trừ từ tiền đã khóa
            wallet.LockedBalance -= amount;
            _walletRepo.Update(wallet);

            await _escrowRepo.AddAsync(new EscrowSession
            {
                EventId = eventId,
                SenderId = expertId,
                Amount = amount,
                Status = "Held",
                CreatedAt = DateTime.Now
            });

            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = -amount,
                Type = "Escrow_Hold",
                ReferenceId = eventId,
                ReferenceType = "Event",
                Status = "Success",
                CreatedAt = DateTime.Now
            });
        }

        public async Task ManualStartEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("Sự kiện không tồn tại.");

            // 1. Kiểm tra trạng thái
            if (ev.Status != "Inviting")
                throw new Exception("Sự kiện chưa được duyệt hoặc đã bắt đầu/kết thúc.");

            // 2. CHECK GIỚI HẠN THỜI GIAN
            double maxEarlyHours = await _settingRepo.GetDoubleValueAsync("MaxEarlyStartHours", 24.0);
            DateTime now = DateTime.Now;

            // Chặn bấm Start quá sớm (Áp dụng chung cho cả Auto và Manual)
            if ((ev.StartTime.Value - now).TotalHours > maxEarlyHours)
            {
                throw new Exception($"Bạn chỉ có thể bắt đầu sớm tối đa {maxEarlyHours} tiếng so với lịch dự kiến.");
            }

            // --- TÁCH LOGIC DỰA VÀO IsAutoStart ---
            if (ev.IsAutoStart)
            {
                // Sự kiện Tự động: Đã đến giờ StartTime -> Cấm bấm tay, bắt chờ Quartz xử lý
                if (now >= ev.StartTime)
                {
                    throw new Exception("Sự kiện này được cài đặt Tự động. Đã đến giờ, vui lòng đợi hệ thống kích hoạt trong giây lát.");
                }
            }
            else
            {
                // Sự kiện Thủ công: Nếu ngâm quá 12 tiếng kể từ giờ StartTime dự kiến -> Khóa mõm, cấm Start
                // (Lưu ý: Nếu ý bạn là 12 tiếng từ lúc TẠO event, hãy đổi ev.StartTime.Value thành ev.CreatedAt.Value nhé)
                if ((now - ev.StartTime.Value).TotalHours > 12)
                {
                    throw new Exception("Đã quá 12 tiếng kể từ thời gian bắt đầu dự kiến. Bạn không thể kích hoạt sự kiện này được nữa.");
                    // TIP: Chỗ này sau này bạn có thể viết thêm logic tự động Cancel Event và hoàn tiền (Refund) nếu muốn.
                }
            }

            // 3. Kiểm tra số lượng Expert hiện tại
            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            if (acceptedCount < ev.MinExpertsToStart)
                throw new Exception($"Chưa đủ số lượng chuyên gia tối thiểu ({ev.MinExpertsToStart}).");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // 1. Thu phí hệ thống & Chuyển tiền vào Escrow (Ký quỹ)
                await CollectSystemFeeAsync(wallet, ev);
                await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

                // 2. Cập nhật thông tin Event: Chuyển sang Active và cập nhật StartTime thực tế
                ev.Status = "Active";
                ev.StartTime = DateTime.Now;
                _eventRepo.Update(ev);

                // 3. Xử lý các Expert chưa phản hồi (Pending) -> Chuyển thành Closed
                foreach (var exp in experts.Where(e => e.Status == "Pending"))
                {
                    exp.Status = "Closed_InvitationExpired";
                    _eventExpertRepo.Update(exp);
                }

                await _unitOfWork.SaveChangesAsync();

                // 4. HỦY BACKGROUND JOB ĐÃ LẬP LỊCH TRƯỚC ĐÓ (Phòng trường hợp Admin có can thiệp)
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey($"Job_Activate_{ev.EventId}", "EventGroup");
                if (await scheduler.CheckExists(jobKey))
                {
                    await scheduler.DeleteJob(jobKey);
                }

                // Lập lịch Job trao giải khi kết thúc sự kiện
                await ScheduleEventFinalization(ev);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi bắt đầu sự kiện thủ công: {ex.Message}");
            }
        }

        private async Task ScheduleEventFinalization(Event ev)
        {
            if (!ev.EndTime.HasValue) return;

            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<FinalizeEventJob>()
                .WithIdentity($"Job_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Tự động trao giải sự kiện: {ev.Title} (ID: {ev.EventId})")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            DateTime processTime = ev.EndTime.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.EndTime.Value, DateTimeKind.Local)
                : ev.EndTime.Value;

            DateTimeOffset endTimeOffset = new DateTimeOffset(processTime);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Lịch trao giải sự kiện '{ev.Title}' vào {endTimeOffset:dd/MM/yyyy HH:mm}")
                .StartAt(endTimeOffset)
                // Misfire Instruction: Nếu Server tắt ngay lúc EndTime, khi bật lại sẽ bắn bù ngay!
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private void ValidateEventRequest(CreateEventRequest dto)
        {
            if (dto.StartTime >= dto.SubmissionDeadline)
            {
                throw new Exception("Ngày bắt đầu phải trước hạn chót nộp bài.");
            }

            if (dto.SubmissionDeadline >= dto.EndTime)
            {
                throw new Exception("Hạn chót nộp bài phải trước ngày kết thúc sự kiện (để Expert có thời gian chấm).");
            }

            if (Math.Abs(dto.ExpertWeight + dto.UserWeight - 1.0) > 0.001)
                throw new Exception("Tổng trọng số Expert và User phải bằng 1.0.");

            if (dto.MinExpertsRequired < 2)
                throw new Exception("Số lượng Expert yêu cầu tối thiểu không được dưới 1.");
        }

        private async Task CreatePrizesAsync(int eventId, List<PrizeRequest> prizeRequests)
        {
            var prizes = prizeRequests.Select(p => new PrizeEvent
            {
                EventId = eventId,
                Ranked = p.Ranked,
                RewardAmount = p.RewardAmount,
                Status = "Active"
            }).ToList();
            await _prizeRepo.AddRangeAsync(prizes);
        }

        private async Task SetupExpertPanelAsync(int eventId, int creatorId, List<int>? invitedIds, bool isDraft)
        {
            var status = isDraft ? "Awaiting_Review" : "Pending";

            var expertPanel = new List<EventExpert> {
                new EventExpert {
                    EventId = eventId,
                    ExpertId = creatorId,
                    JoinedAt = DateTime.Now,
                    Status = "Accepted"
                }
            };

            if (invitedIds != null)
            {
                expertPanel.AddRange(invitedIds.Distinct().Where(id => id != creatorId).Select(id => new EventExpert
                {
                    EventId = eventId,
                    ExpertId = id,
                    JoinedAt = DateTime.Now,
                    Status = status
                }));
            }
            await _eventExpertRepo.AddRangeAsync(expertPanel);
        }
    }
}
