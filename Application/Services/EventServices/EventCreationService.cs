using Application.Interfaces;
using Application.Jobs;
using Application.Request.EventReq;
using Application.Request.NotificationReq;
using Application.Request.PrizeReq;
using Application.Services.NotificationImp;
using Application.Utils;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
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

            decimal feePercentage = await _settingRepo.GetDecimalValueAsync("EVENT_FEE_PERCENTAGE", 5.0m);

            decimal minFee = await _settingRepo.GetDecimalValueAsync("EVENT_MIN_FEE", 10000m);

            var totalPrize = dto.Prizes.Sum(p => p.RewardAmount);

            decimal calculatedFee = totalPrize * (feePercentage / 100m);

            decimal currentFee = Math.Max(calculatedFee, minFee);

            var wallet = await _walletRepo.GetByAccountIdAsync(creatorId);
            if (wallet == null || wallet.Balance < totalPrize + currentFee)
                throw new Exception($"Số dư ví không đủ. Cần {totalPrize + currentFee:N0} VNĐ (bao gồm phí tạo).");

            decimal totalToLock = totalPrize + currentFee;

            await CheckSpendingLimitAsync(wallet, totalToLock);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                string? imageUrl = dto.ImageFile != null ? await _cloudStorageService.UploadImageAsync(dto.ImageFile) : null;

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

                //decimal totalToLock = totalPrize + currentFee;
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
                    //await CollectSystemFeeAsync(wallet, ev);
                    //await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);
                    decimal totalEventSpending = ev.AppliedFee + totalPrizeAmount;
                    await CheckSpendingLimitAsync(wallet, totalEventSpending);
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
            if (ev.AppliedFee <= 0) return; // Không có phí thì không cần chạy tiếp

            // --- 1. XỬ LÝ VÍ EXPERT (NGƯỜI TẠO) ---
            decimal expertBefore = expertWallet.LockedBalance;
            expertWallet.LockedBalance -= ev.AppliedFee;

            // Log giao dịch chi trả phí cho Expert
            await _transactionRepo.AddAsync(new Transaction
            {
                TransactionCode = $"PAY_FEE_{ev.EventId}_{DateTime.Now.Ticks}",
                WalletId = expertWallet.WalletId,
                Amount = -ev.AppliedFee, // Số tiền âm (chi ra)
                BalanceBefore = expertBefore,
                BalanceAfter = expertWallet.LockedBalance,
                Type = "System_Fee_Payment",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Thanh toán phí hệ thống cho sự kiện: {ev.Title}",
                CreatedAt = DateTime.Now
            });

            // --- 2. XỬ LÝ VÍ ADMIN (HỆ THỐNG) ---
            decimal adminBefore = adminWallet.Balance;
            adminWallet.Balance += ev.AppliedFee;

            // Log giao dịch doanh thu cho Admin
            await _transactionRepo.AddAsync(new Transaction
            {
                TransactionCode = $"REV_FEE_{ev.EventId}_{DateTime.Now.Ticks}",
                WalletId = adminWallet.WalletId,
                Amount = ev.AppliedFee, // Số tiền dương (thu vào)
                BalanceBefore = adminBefore,
                BalanceAfter = adminWallet.Balance,
                Type = "System_Fee_Revenue",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Thu phí hệ thống từ sự kiện: {ev.EventId}",
                CreatedAt = DateTime.Now
            });

            // Cập nhật trạng thái ví vào DB
            _walletRepo.Update(expertWallet);
            _walletRepo.Update(adminWallet);
        }

        private async Task ProcessEscrowFromLockedAsync(int eventId, int expertId, decimal amount, Wallet wallet)
        {
            // 1. Lấy số dư trước khi thay đổi để log giao dịch
            decimal beforeLocked = wallet.LockedBalance;

            // 2. Trừ từ tiền đã khóa (Tiền thưởng sự kiện)
            wallet.LockedBalance -= amount;
            _walletRepo.Update(wallet);

            // 3. Tạo phiên ký quỹ (Escrow) để giữ tiền thưởng
            await _escrowRepo.AddAsync(new EscrowSession
            {
                EventId = eventId,
                SenderId = expertId,
                Amount = amount,
                Status = "Held",
                CreatedAt = DateTime.Now
            });

            // 4. Log giao dịch chuyển tiền vào hệ thống ký quỹ
            await _transactionRepo.AddAsync(new Transaction
            {
                // FIX LỖI: Thêm mã giao dịch duy nhất
                TransactionCode = $"ESCROW_HOLD_{eventId}_{DateTime.Now.Ticks}",

                WalletId = wallet.WalletId,
                Amount = -amount, // Số tiền âm vì đang chuyển ra khỏi ví (vào Escrow)

                // Bổ sung thông tin đối soát số dư
                BalanceBefore = beforeLocked,
                BalanceAfter = wallet.LockedBalance,

                Type = "Escrow_Hold",
                ReferenceId = eventId,
                ReferenceType = "Event",
                Status = "Success",
                Description = $"Ký quỹ tiền thưởng cho sự kiện ID: {eventId}",
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

            if ((ev.StartTime.Value - now).TotalHours > maxEarlyHours)
            {
                throw new Exception($"Bạn chỉ có thể bắt đầu sớm tối đa {maxEarlyHours} tiếng so với lịch dự kiến.");
            }

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
                // Sự kiện Thủ công: Nếu ngâm quá 12 tiếng kể từ giờ StartTime dự kiến -> cấm Start
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // 1. Thu phí hệ thống & Chuyển tiền vào Escrow (Ký quỹ)
                //await CollectSystemFeeAsync(wallet, ev);
                //await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);
                decimal totalEventSpending = ev.AppliedFee + totalPrizeAmount;
                await CheckSpendingLimitAsync(wallet, totalEventSpending);
                await CollectSystemFeeAsync(wallet, ev);
                await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

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
                            Title = "Sự kiện đã bắt đầu!",
                            Content = $"Sự kiện '{ev.Title}' đã chính thức diễn ra.",
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

            // 1. Kiểm tra quyền sở hữu
            if (ev.CreatorId != currentUserId)
                throw new Exception("Bạn không có quyền hủy sự kiện này.");

            // 2. Kiểm tra trạng thái cho phép hủy
            // Chỉ được hủy khi đang chờ duyệt (Pending_Review) hoặc đang mời chuyên gia (Inviting)
            // Một khi đã Active (đang diễn ra), không được phép hủy ngang để bảo vệ thí sinh.
            var allowedStatuses = new[] { "Pending_Review", "Inviting" };
            if (!allowedStatuses.Contains(ev.Status))
                throw new Exception($"Không thể hủy sự kiện ở trạng thái {ev.Status}.");

            string oldStatus = ev.Status;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);
                decimal totalToRefund = totalPrizeAmount + ev.AppliedFee;

                // 3. Thực hiện hoàn tiền (Refund)
                decimal beforeBalance = wallet.Balance;
                decimal beforeLocked = wallet.LockedBalance;

                wallet.LockedBalance -= totalToRefund;
                wallet.Balance += totalToRefund;
                _walletRepo.Update(wallet);

                // 4. Ghi Log giao dịch hoàn tiền
                await _transactionRepo.AddAsync(new Transaction
                {
                    TransactionCode = $"REFUND_{eventId}_{DateTime.Now.Ticks}",
                    WalletId = wallet.WalletId,
                    Amount = totalToRefund,
                    BalanceBefore = beforeBalance, // Log theo ví chính
                    BalanceAfter = wallet.Balance,
                    Type = "Event_Cancel_Refund",
                    ReferenceId = eventId,
                    ReferenceType = "Event",
                    Status = "Success",
                    Description = $"Hoàn tiền hủy sự kiện: {ev.Title}",
                    CreatedAt = DateTime.Now
                });

                // 5. Cập nhật trạng thái sự kiện và chuyên gia
                ev.Status = "Cancelled_By_Creator";
                _eventRepo.Update(ev);

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
                                Title = "Sự kiện đã bị hủy",
                                Content = $"Sự kiện '{ev.Title}' mà bạn được mời đã bị người tổ chức hủy bỏ.",
                                Type = "Event_Cancelled",
                                RelatedId = eventId.ToString()
                            });
                        }
                    }
                }

                foreach (var exp in experts)
                {
                    if (exp.Status == "Pending" || exp.Status == "Accepted" || exp.Status == "Awaiting_Review")
                    {
                        exp.Status = "Event_Cancelled";
                        _eventExpertRepo.Update(exp);
                    }
                }

                // 6. Hủy bỏ Background Job kích hoạt tự động (nếu có)
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

        private void ValidateEventRequest(CreateEventRequest dto, int minExpertsRequiredBySystem)
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

            if (dto.MinExpertsRequired < minExpertsRequiredBySystem)
            {
                throw new Exception($"Số lượng Expert yêu cầu tối thiểu cho mỗi sự kiện theo quy định hệ thống là {minExpertsRequiredBySystem} người.");
            }
            if (dto.Criteria == null || !dto.Criteria.Any())
            {
                throw new Exception("Sự kiện cần có ít nhất một tiêu chí chấm điểm.");
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

        private async Task CheckSpendingLimitAsync(Wallet wallet, decimal debitAmount)
        {
            if (wallet == null)
                throw new KeyNotFoundException("Ví không tồn tại.");

            if (debitAmount <= 0)
                throw new ArgumentException("Số tiền chi không hợp lệ.");

            if (!wallet.MonthlySpendingLimit.HasValue || wallet.MonthlySpendingLimit.Value <= 0)
                return;

            var now = DateTime.UtcNow;

            decimal spentThisMonth = await _transactionRepo.GetMonthlyDebitTotalAsync(
                wallet.WalletId,
                now.Month,
                now.Year);

            decimal projectedSpent = spentThisMonth + debitAmount;
            decimal limitAmount = wallet.MonthlySpendingLimit.Value;

            if (wallet.IsHardSpendingLimit && projectedSpent > limitAmount)
            {
                throw new InvalidOperationException(
                    $"Bạn đã vượt hạn mức chi tiêu tháng. " +
                    $"Đã chi: {spentThisMonth:N0} VND, " +
                    $"chi phí sự kiện: {debitAmount:N0} VND, " +
                    $"hạn mức: {limitAmount:N0} VND.");
            }
        }
    }
}
