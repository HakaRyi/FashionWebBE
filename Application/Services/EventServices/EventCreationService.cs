using Application.Interfaces;
using Application.Jobs;
using Application.Request.EventReq;
using Application.Request.NotificationReq;
using Application.Request.PrizeReq;
using Application.Services.NotificationImp;
using Application.Utils;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Services.EventServices
{
    public class EventCreationService : IEventCreationService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IPrizeEventRepository _prizeRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ISystemSettingRepository _settingRepo;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly INotificationService _notificationService;


        public EventCreationService(
            IEventRepository eventRepo,
            IWalletRepository walletRepo,
            IPrizeEventRepository prizeRepo,
            ITransactionRepository transactionRepo,
            IEscrowSessionRepository escrowRepo,
            IEventExpertRepository eventExpertRepo,
            IUnitOfWork unitOfWork,
            ISystemSettingRepository settingRepo,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            ICloudStorageService cloudStorageService)
        {
            _eventRepo = eventRepo;
            _walletRepo = walletRepo;
            _prizeRepo = prizeRepo;
            _transactionRepo = transactionRepo;
            _escrowRepo = escrowRepo;
            _eventExpertRepo = eventExpertRepo;
            _settingRepo = settingRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _schedulerFactory = schedulerFactory;
            _cloudStorageService = cloudStorageService;
            _notificationService = notificationService;
        }

        public async Task<Event> CreateEventAsync(CreateEventRequest dto)
        {
            int creatorId = _currentUserService.GetRequiredUserId();

            int minExpertsSystemConfig = await _settingRepo.GetIntValueAsync("MIN_EXPERTS_PER_EVENT", 2);

            if (dto.MinExpertsRequired < minExpertsSystemConfig)
            {
                dto.MinExpertsRequired = minExpertsSystemConfig;
            }

            ValidateEventRequest(dto, minExpertsSystemConfig);

            decimal fixedSystemFee = await _settingRepo.GetDecimalValueAsync("EVENT_MIN_FEE", 10000m);

            var totalPrize = dto.Prizes.Sum(p => p.RewardAmount);

            decimal totalToLock = totalPrize + fixedSystemFee;

            var wallet = await _walletRepo.GetByAccountIdAsync(creatorId);
            if (wallet == null || wallet.Balance < totalToLock)
                throw new Exception($"Insufficient wallet balance. You need {totalToLock:N0} VNĐ.");

            await CheckSpendingLimitAsync(
                wallet,
                totalToLock,
                "event creation cost");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                string? imageUrl = dto.ImageFile != null ? await _cloudStorageService.UploadImageAsync(dto.ImageFile) : null;

                var eventData = dto.Adapt<Event>();

                eventData.StartTime = dto.StartTime.ToUniversalTime();
                eventData.SubmissionDeadline = dto.SubmissionDeadline.ToUniversalTime();
                eventData.EndTime = dto.EndTime.ToUniversalTime();

                eventData.MinExpertsToStart = dto.MinExpertsRequired;
                eventData.CreatorId = creatorId;
                eventData.AppliedFee = fixedSystemFee;
                eventData.EntryFee = dto.EntryFee;
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

                foreach (var criteriaDto in dto.Criteria)
                {
                    eventData.EventCriteria.Add(new EventCriterion
                    {
                        Name = criteriaDto.Name,
                        Description = criteriaDto.Description,
                        WeightPercentage = criteriaDto.WeightPercentage
                    });
                }

                await _eventRepo.AddAsync(eventData);
                await _unitOfWork.SaveChangesAsync();

                await CreatePrizesAsync(eventData.EventId, dto.Prizes);
                await SetupExpertPanelAsync(eventData.EventId, creatorId, dto.InvitedExpertIds, isDraft: true);

                wallet = await _walletRepo.GetByAccountIdAsync(creatorId);
                if (wallet == null || wallet.Balance < totalToLock)
                    throw new Exception($"Insufficient wallet balance. You need {totalToLock:N0} VNĐ (including creation fee).");

                await CheckSpendingLimitAsync(
                    wallet,
                    totalToLock,
                    "event creation cost");

                decimal balanceBefore = wallet.Balance;


                wallet.Balance -= totalToLock;
                wallet.LockedBalance += totalToLock;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                await _transactionRepo.AddAsync(new Transaction
                {
                    TransactionCode = $"HOLD_{eventData.EventId}_{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    WalletId = wallet.WalletId,
                    Amount = -totalToLock,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Event_Funds_Locked",
                    ReferenceId = eventData.EventId,
                    ReferenceType = "Event",
                    Status = "Success",
                    Description = $"Freeze money (Prize + Fee) in wallet for event setup: {eventData.Title}",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                return eventData;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Event creation error: {ex.Message}");
            }
        }

        public async Task ActivateEventWithEscrowAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Inviting") return;

            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            await _unitOfWork.BeginTransactionAsync();
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
                    await ProcessEscrowFromLockedAsync(ev, ev.CreatorId, totalPrizeAmount, wallet);

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

        private async Task CollectSystemFeeAsync(Wallet creatorWallet, Event ev)
        {
            if (ev.AppliedFee < 0) return;

            int adminAccountId = await _settingRepo.GetIntValueAsync("SystemAdminAccountId", 1);
            var adminWallet = await _walletRepo.GetByAccountIdAsync(adminAccountId);
            if (adminWallet == null) throw new Exception("System wallet not found.");

            // --- 1. XỬ LÝ VÍ CREATOR (NGƯỜI TẠO SỰ KIỆN) ---
            // Tính toán số dư tổng thực tế của Creator trước khi trừ phí hệ thống
            decimal creatorTotalBefore = creatorWallet.Balance + creatorWallet.LockedBalance;

            // Thu phí hệ thống từ khoản tiền đang bị khóa phục vụ cho Event này
            creatorWallet.LockedBalance -= ev.AppliedFee;
            creatorWallet.UpdatedAt = DateTime.UtcNow;

            decimal creatorTotalAfter = creatorWallet.Balance + creatorWallet.LockedBalance;

            // Log giao dịch chi trả phí cho Creator (Lưu chuẩn số dư tổng hệ thống)
            await _transactionRepo.AddAsync(new Transaction
            {
                TransactionCode = $"PAY_FEE_{ev.EventId}_{DateTime.UtcNow.Ticks}",
                WalletId = creatorWallet.WalletId,
                Amount = -ev.AppliedFee,
                BalanceBefore = creatorTotalBefore,
                BalanceAfter = creatorTotalAfter,
                Type = "System_Fee_Payment",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Pay the system creation fee for the event: {ev.Title}",
                CreatedAt = DateTime.UtcNow
            });

            // --- 2. XỬ LÝ VÍ ADMIN (HỆ THỐNG) ---
            decimal adminBefore = adminWallet.Balance;
            adminWallet.Balance += ev.AppliedFee;
            adminWallet.UpdatedAt = DateTime.UtcNow;

            // Log giao dịch doanh thu cho Admin
            await _transactionRepo.AddAsync(new Transaction
            {
                TransactionCode = $"REV_FEE_{ev.EventId}_{DateTime.UtcNow.Ticks}",
                WalletId = adminWallet.WalletId,
                Amount = ev.AppliedFee,
                BalanceBefore = adminBefore,
                BalanceAfter = adminWallet.Balance,
                Type = "System_Fee_Revenue",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Collect system fees from event ID: {ev.EventId}",
                CreatedAt = DateTime.UtcNow
            });

            // Đồng bộ ví Admin ngay, còn ví Creator sẽ update chung ở hàm tiếp theo
            _walletRepo.Update(adminWallet);
        }

        private async Task ProcessEscrowFromLockedAsync(Event ev, int creatorId, decimal amount, Wallet wallet)
        {
            if (amount <= 0) return;

            // Tính toán số dư tổng thực tế của Creator trước khi trích quỹ giải thưởng vào Escrow
            decimal mainBalanceBefore = wallet.Balance + wallet.LockedBalance;

            // 1. Trừ từ tiền đã khóa của Creator
            wallet.LockedBalance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Tính toán số dư tổng thực tế sau khi trích tiền giải thưởng đi
            decimal mainBalanceAfter = wallet.Balance + wallet.LockedBalance;

            // 2. Tạo phiên ký quỹ giải thưởng (Dùng chung bảng EscrowSession)
            await _escrowRepo.AddAsync(new EscrowSession
            {
                EventId = ev.EventId,
                SenderId = creatorId,
                ReceiverId = null,
                Amount = amount,
                ServiceFee = 0,
                Status = EscrowStatus.Held,
                Description = $"PRIZE_POOL: Total prize money for event '{ev.Title}'",
                CreatedAt = DateTime.UtcNow
            });

            // 3. Log giao dịch chuyển tiền giải thưởng vào hệ thống ký quỹ
            await _transactionRepo.AddAsync(new Transaction
            {
                TransactionCode = $"ESCROW_PRIZE_HOLD_{ev.EventId}_{DateTime.UtcNow.Ticks}",
                WalletId = wallet.WalletId,
                Amount = -amount,
                BalanceBefore = mainBalanceBefore,
                BalanceAfter = mainBalanceAfter,
                Type = "Escrow_Prize_Hold",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Transferred locked balance to Escrow Prize Pool for event: {ev.Title}",
                CreatedAt = DateTime.UtcNow
            });

            _walletRepo.Update(wallet);
        }

        public async Task ManualStartEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("The event does not exist.");

            // 1. Kiểm tra trạng thái
            if (ev.Status != "Inviting")
                throw new Exception("The event has not been approved or has already started/ended.");

            // 2. CHECK GIỚI HẠN THỜI GIAN
            double maxEarlyHours = await _settingRepo.GetDoubleValueAsync("MaxEarlyStartHours", 24.0);
            DateTime now = DateTime.UtcNow;

            DateTime dbTime = ev.StartTime.Value;

            DateTime eventStartUtc = ev.StartTime.Value.ToUniversalTime();

            double hoursUntilStart = (eventStartUtc - now).TotalHours;

            if (hoursUntilStart > maxEarlyHours)
            {
                throw new Exception($"You can only start a maximum of {maxEarlyHours} hours earlier than scheduled.");
            }

            if (ev.IsAutoStart)
            {
                // Sự kiện Tự động: Đã đến giờ StartTime -> Cấm bấm tay, bắt chờ Quartz xử lý
                if (now >= eventStartUtc)
                {
                    throw new Exception("This event is set to Automatic. It's time, please wait a moment for the system to activate.");
                }
            }
            else
            {
                // Sự kiện Thủ công: Nếu ngâm quá 12 tiếng kể từ giờ StartTime dự kiến -> cấm Start
                if ((now - ev.StartTime.Value).TotalHours > 12)
                {
                    throw new Exception("More than 12 hours have passed since the scheduled start time. You can no longer trigger this event.");
                    // TIP: Chỗ này sau này bạn có thể viết thêm logic tự động Cancel Event và hoàn tiền (Refund) nếu muốn.
                }
            }

            // 3. Kiểm tra số lượng Expert hiện tại
            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            if (acceptedCount < ev.MinExpertsToStart)
                throw new Exception($"The minimum number of experts is not yet available: ({ev.MinExpertsToStart}).");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                decimal totalRequiredFromLocked = ev.AppliedFee + totalPrizeAmount;

                if (wallet == null || wallet.LockedBalance < totalRequiredFromLocked)
                {
                    throw new Exception($"The organizer's locked balance is insufficient to start the event. " +
                                        $"Required: {totalRequiredFromLocked:N0} VNĐ (Fee: {ev.AppliedFee:N0} VNĐ, Prizes: {totalPrizeAmount:N0} VNĐ), " +
                                        $"Available Locked: {wallet?.LockedBalance ?? 0:N0} VNĐ.");
                }

                // 1. Thu phí hệ thống & Chuyển tiền vào Escrow (Ký quỹ)
                await CollectSystemFeeAsync(wallet, ev);
                await ProcessEscrowFromLockedAsync(ev, ev.CreatorId, totalPrizeAmount, wallet);

                // 2. Cập nhật thông tin Event: Chuyển sang Active và cập nhật StartTime thực tế
                ev.Status = "Active";
                ev.StartTime = DateTime.UtcNow;
                _eventRepo.Update(ev);

                // 3. Xử lý các Expert chưa phản hồi (Pending) -> Chuyển thành Closed
                foreach (var exp in experts)
                {
                    if (exp.Status == "Pending")
                    {
                        exp.Status = "Closed_InvitationExpired";
                        _eventExpertRepo.Update(exp);
                    }
                    else if (exp.Status == "Accepted")
                    {
                        // THÔNG BÁO SỰ KIỆN BẮT ĐẦU
                        await _notificationService.SendNotificationAsync(new SendNotificationRequest
                        {
                            SenderId = ev.CreatorId,
                            TargetUserId = exp.ExpertId,
                            Title = "The event has begun!",
                            Content = $"The '{ev.Title}' has officially begun.",
                            Type = "Event_Started",
                            RelatedId = eventId.ToString()
                        });
                    }
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

        public async Task CancelEventAsync(int eventId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null) throw new Exception("Sự kiện không tồn tại.");

            if (ev.CreatorId != currentUserId)
                throw new Exception("Bạn không có quyền hủy sự kiện này.");

            var allowedStatuses = new[] { "Pending_Review", "Inviting" };
            if (!allowedStatuses.Contains(ev.Status))
                throw new Exception($"Không thể hủy sự kiện ở trạng thái {ev.Status}.");

            string oldStatus = ev.Status;

            // Lấy danh sách giải thưởng để tính toán chính xác số tiền cần hoàn trả lại
            var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
            decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

            // Tổng số tiền đang bị khóa trong ví lúc tạo = Tiền giải thưởng + Phí hệ thống cố định áp dụng
            decimal totalToRefund = totalPrizeAmount + ev.AppliedFee;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                if (wallet == null) throw new Exception("Không tìm thấy ví của nhà tổ chức.");

                // KIỂM TRA ĐỒNG BỘ: Phòng trường hợp hy hữu số dư khóa bị hụt
                if (wallet.LockedBalance < totalToRefund)
                    throw new Exception("Số dư đóng băng của ví không đủ để thực hiện hoàn tác.");

                decimal beforeBalance = wallet.Balance;

                // 1. Thực hiện trả tiền từ LockedBalance về Balance khả dụng
                wallet.Balance += totalToRefund;
                wallet.LockedBalance -= totalToRefund; // QUAN TRỌNG: Phải trừ đi khoản đóng băng
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                // 2. Kiểm tra xem có bản ghi Escrow nào lỡ tạo sớm hay không (Bọc an toàn)
                var allEscrows = await _escrowRepo.GetTotalEscrowsByEventIdAsync(eventId);
                var prizeEscrow = allEscrows?.FirstOrDefault(e => e.Status == EscrowStatus.Held
                                                          && e.Description != null
                                                          && e.Description.StartsWith("PRIZE_POOL"));
                if (prizeEscrow != null)
                {
                    prizeEscrow.Status = EscrowStatus.Refunded;
                    prizeEscrow.ResolvedAt = DateTime.UtcNow;
                    prizeEscrow.Description = $"Refunded to creator due to event cancellation.";
                    _escrowRepo.Update(prizeEscrow);
                }

                // 3. Ghi Log giao dịch hoàn tiền ví cho Creator
                await _transactionRepo.AddAsync(new Transaction
                {
                    TransactionCode = $"REFUND_{eventId}_{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    WalletId = wallet.WalletId,
                    Amount = totalToRefund,
                    BalanceBefore = beforeBalance,
                    BalanceAfter = wallet.Balance,
                    Type = "Event_Cancel_Refund",
                    ReferenceId = eventId,
                    ReferenceType = "Event",
                    Status = "Success",
                    Description = $"Event cancellation refund for prize pool ({totalPrizeAmount:N0} VNĐ) and fee ({ev.AppliedFee:N0} VNĐ): {ev.Title}",
                    CreatedAt = DateTime.UtcNow
                });

                // 4. Cập nhật trạng thái sự kiện
                ev.Status = "Cancelled_By_Creator";
                _eventRepo.Update(ev);

                // 5. Cập nhật trạng thái các Chuyên gia (Experts) đã mời
                var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
                foreach (var exp in experts)
                {
                    if (exp.Status == "Pending" || exp.Status == "Accepted" || exp.Status == "Awaiting_Review")
                    {
                        exp.Status = "Event_Cancelled";
                        _eventExpertRepo.Update(exp);

                        if (oldStatus == "Inviting" && exp.ExpertId != currentUserId)
                        {
                            await _notificationService.SendNotificationAsync(new SendNotificationRequest
                            {
                                SenderId = currentUserId,
                                TargetUserId = exp.ExpertId,
                                Title = "The event has been cancelled.",
                                Content = $"The '{ev.Title}' you were invited to has been cancelled by the organizers.",
                                Type = "Event_Cancelled",
                                RelatedId = eventId.ToString()
                            });
                        }
                    }
                }

                // 6. Xóa Scheduler Job kích hoạt tự động nếu có
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey($"Job_Activate_{ev.EventId}", "EventGroup");
                if (await scheduler.CheckExists(jobKey))
                {
                    await scheduler.DeleteJob(jobKey);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi hủy sự kiện: {ex.Message}");
            }
        }

        private async Task ScheduleEventFinalization(Event ev)
        {
            if (!ev.EndTime.HasValue) return;

            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<FinalizeEventJob>()
                .WithIdentity($"Job_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Automatically award prizes for events: {ev.Title} (ID: {ev.EventId})")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            DateTime endTimeUtc = ev.EndTime.Value.ToUniversalTime();
            DateTimeOffset endTimeOffset = new DateTimeOffset(endTimeUtc);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Award Ceremony Schedule '{ev.Title}' on {endTimeOffset:dd/MM/yyyy HH:mm}")
                .StartAt(endTimeOffset)
                // Misfire Instruction: Nếu Server tắt ngay lúc EndTime, khi bật lại sẽ bắn bù ngay!
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private void ValidateEventRequest(CreateEventRequest dto, int minExpertsRequiredBySystem)
        {
            if (dto.StartTime >= dto.SubmissionDeadline)
            {
                throw new Exception("The start date must be before the submission deadline.");
            }

            if (dto.SubmissionDeadline >= dto.EndTime)
            {
                throw new Exception("The submission deadline must be before the event end date (to allow experts time to review).");
            }

            if (Math.Abs(dto.ExpertWeight + dto.UserWeight - 1.0) > 0.001)
                throw new Exception("The total weight of Expert and User must equal 1.0.");

            if (dto.MinExpertsRequired < minExpertsRequiredBySystem)
            {
                throw new Exception($"The minimum number of Experts required for each event, as stipulated by the system, is{minExpertsRequiredBySystem} people.");
            }
            if (dto.Criteria == null || !dto.Criteria.Any())
            {
                throw new Exception("The event needs to have at least one scoring criterion.");
            }
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

        private async Task CheckSpendingLimitAsync(
            Wallet wallet,
            decimal debitAmount,
            string actionName)
        {
            if (wallet == null)
                throw new Exception("Wallet not found.");

            if (debitAmount <= 0)
                throw new Exception("Invalid spending amount.");

            if (!wallet.MonthlySpendingLimit.HasValue ||
                wallet.MonthlySpendingLimit.Value <= 0)
            {
                return;
            }

            if (!wallet.IsHardSpendingLimit)
                return;

            var now = DateTime.UtcNow;

            decimal spentThisMonth = await _transactionRepo.GetMonthlyDebitTotalAsync(
                wallet.WalletId,
                now.Month,
                now.Year);

            decimal projectedSpent = spentThisMonth + debitAmount;
            decimal limitAmount = wallet.MonthlySpendingLimit.Value;

            if (projectedSpent > limitAmount)
            {
                throw new Exception(
                    $"You have exceeded your monthly spending limit. " +
                    $"Spent this month: {spentThisMonth:N0} VND, " +
                    $"{actionName}: {debitAmount:N0} VND, " +
                    $"limit: {limitAmount:N0} VND.");
            }
        }
    }
}