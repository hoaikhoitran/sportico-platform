using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Application.Options;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class BookingService : IBookingService
    {
        private const decimal PlatformFeeRate = 0.15m;

        private readonly ITrainingPackageRepository _trainingPackageRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ITrainingSessionRepository _trainingSessionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayOsService _payOsService;
        private readonly ICoachWalletRepository _coachWalletRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBookingSessionUsageService _sessionUsageService;
        private readonly ILogger<BookingService> _logger;
        private readonly bool _enableManualPurchase;
        private readonly IValidator<PurchaseTrainingPackageManualRequest> _manualValidator;
        private readonly IValidator<PurchaseTrainingPackagePayOsRequest> _payOsValidator;
        private readonly IValidator<BookingFilterRequest> _filterValidator;

        public BookingService(
            ITrainingPackageRepository trainingPackageRepository,
            IBookingRepository bookingRepository,
            ITrainingSessionRepository trainingSessionRepository,
            IPaymentRepository paymentRepository,
            IPayOsService payOsService,
            ICoachWalletRepository coachWalletRepository,
            INotificationRepository notificationRepository,
            IBookingSessionUsageService sessionUsageService,
            ILogger<BookingService> logger,
            IOptions<FeatureOptions> featureOptions,
            IValidator<PurchaseTrainingPackageManualRequest> manualValidator,
            IValidator<PurchaseTrainingPackagePayOsRequest> payOsValidator,
            IValidator<BookingFilterRequest> filterValidator)
        {
            _trainingPackageRepository = trainingPackageRepository;
            _bookingRepository = bookingRepository;
            _trainingSessionRepository = trainingSessionRepository;
            _paymentRepository = paymentRepository;
            _payOsService = payOsService;
            _coachWalletRepository = coachWalletRepository;
            _notificationRepository = notificationRepository;
            _sessionUsageService = sessionUsageService;
            _logger = logger;
            _enableManualPurchase = featureOptions.Value.EnableManualPurchase;
            _manualValidator = manualValidator;
            _payOsValidator = payOsValidator;
            _filterValidator = filterValidator;
        }

        /// <summary>Maps a page of bookings, enriching each with real session usage (one grouped query).</summary>
        private async Task<List<BookingResponse>> MapWithUsageAsync(IReadOnlyCollection<Booking> bookings)
        {
            if (bookings.Count == 0)
            {
                return new List<BookingResponse>();
            }

            var totals = bookings
                .GroupBy(b => b.Id)
                .ToDictionary(g => g.Key, g => g.First().TotalSessions);

            var usageMap = await _sessionUsageService.GetMapAsync(totals);

            return bookings
                .Select(b => usageMap.TryGetValue(b.Id, out var usage)
                    ? b.ToResponse(usage)
                    : b.ToResponse())
                .ToList();
        }

        public async Task<Result<BookingResponse>> PurchaseManualAsync(
            Guid learnerId,
            PurchaseTrainingPackageManualRequest request)
        {
            // Manual purchase is a dev/test-only path that mints an already-paid active booking.
            // In production it is disabled (Features:EnableManualPurchase=false) so normal learners
            // must go through PayOS. Return a clean business error, never a 500.
            if (!_enableManualPurchase)
            {
                throw new ForbiddenException(
                    ErrorCodes.ManualPurchaseDisabled,
                    "Manual purchase is disabled. Please use the PayOS purchase flow.");
            }

            var validationResult = await _manualValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var trainingPackage = await _trainingPackageRepository.GetByIdAsync(request.TrainingPackageId);
            if (trainingPackage == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Published)
            {
                throw new ConflictException(
                    ErrorCodes.TrainingPackageNotPublished,
                    "Training package is not published");
            }

            if (trainingPackage.CoachId == learnerId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You cannot purchase your own training package");
            }

            // New flow: every package session must still have a free seat. Reserve one seat per
            // scheduled session, then auto-create the sessions — the learner never books manually.
            var slots = await _trainingPackageRepository.GetSessionSlotsForUpdateAsync(trainingPackage.Id);
            ReserveSlots(slots);

            var booking = CreateBookingSnapshot(trainingPackage, learnerId, BookingStatuses.Active);
            booking.PaidAt = DateTime.UtcNow;
            booking.ExpiresAt = booking.PaidAt.Value.AddDays(trainingPackage.DurationDays);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = learnerId,
                Amount = booking.TotalAmount,
                Method = PaymentMethods.Manual,
                ReferenceType = PaymentReferenceTypes.Booking,
                ReferenceId = booking.Id,
                Status = PaymentStatuses.Paid,
                CreatedAt = booking.CreatedAt,
                PaidAt = booking.PaidAt
            };

            await _bookingRepository.AddWithoutSaveAsync(booking);
            await _paymentRepository.AddWithoutSaveAsync(payment);

            // Auto-create one scheduled training session per package slot. Booking insert + slot
            // reservation + session inserts share the one scoped DbContext, so this single
            // SaveChanges persists everything atomically and asserts the slot Version tokens
            // (no overselling under concurrent purchases).
            await GenerateSessionsAsync(booking, slots);
            await _bookingRepository.SaveChangesAsync();

            await EnsureBookingActivatedAsync(booking, true);

            // Attach for response mapping only (after all saves are complete).
            booking.TrainingPackage = trainingPackage;

            var response = booking.ToResponse();

            return Result<BookingResponse>.Success(response);
        }

        public async Task<Result<PurchaseTrainingPackagePayOsResponse>> PurchaseWithPayOsAsync(
            Guid learnerId,
            PurchaseTrainingPackagePayOsRequest request)
        {
            var validationResult = await _payOsValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var trainingPackage = await _trainingPackageRepository.GetByIdAsync(request.TrainingPackageId);
            if (trainingPackage == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Published)
            {
                throw new ConflictException(
                    ErrorCodes.TrainingPackageNotPublished,
                    "Training package is not published");
            }

            if (trainingPackage.CoachId == learnerId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You cannot purchase your own training package");
            }

            // Reserve a seat per scheduled session up-front so a pending payment cannot be oversold.
            // Reserved seats are released if the payment is later cancelled / failed / expired, and
            // the booked sessions are generated only once the payment is confirmed paid.
            var slots = await _trainingPackageRepository.GetSessionSlotsForUpdateAsync(trainingPackage.Id);
            ReserveSlots(slots);

            var booking = CreateBookingSnapshot(trainingPackage, learnerId, BookingStatuses.PendingPayment);

            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = learnerId,
                Amount = booking.TotalAmount,
                Method = PaymentMethods.PayOs,
                ReferenceType = PaymentReferenceTypes.Booking,
                ReferenceId = booking.Id,
                Status = PaymentStatuses.Pending,
                TransactionCode = orderCode.ToString(),
                OrderCode = orderCode,
                CreatedAt = booking.CreatedAt
            };

            var payOsResult = await _payOsService.CreatePaymentLinkAsync(
                new CreatePayOsPaymentRequest
                {
                    OrderCode = orderCode,
                    Amount = (int)booking.TotalAmount,
                    Description = $"SPT{orderCode}",
                    BuyerName = "Sportico Learner"
                });

            payment.PaymentLinkId = payOsResult.PaymentLinkId;
            payment.CheckoutUrl = payOsResult.CheckoutUrl;
            payment.ExpiredAt = payOsResult.ExpiredAt;

            await _bookingRepository.AddWithoutSaveAsync(booking);
            await _paymentRepository.AddWithoutSaveAsync(payment);
            await _bookingRepository.SaveChangesAsync();

            var response = new PurchaseTrainingPackagePayOsResponse
            {
                BookingId = booking.Id,
                PaymentId = payment.Id,
                OrderCode = orderCode,
                CheckoutUrl = payOsResult.CheckoutUrl,
                Status = payment.Status,
                ExpiredAt = payOsResult.ExpiredAt
            };

            return Result<PurchaseTrainingPackagePayOsResponse>.Success(response);
        }

        public async Task<Result<object>> HandlePayOsWebhookAsync(
            PayOsWebhookRequest request)
        {
            if (!_payOsService.VerifyWebhookSignature(request.Data, request.Signature ?? string.Empty))
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid webhook signature");
            }

            if (!request.Data.TryGetProperty("orderCode", out var orderCodeElement) ||
                !orderCodeElement.TryGetInt64(out var orderCode))
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Missing orderCode in webhook data");
            }

            var payment = await _paymentRepository.GetByOrderCodeForUpdateAsync(orderCode);

            if (payment == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PaymentNotFound,
                    "Payment not found");
            }

            await _paymentRepository.AddTransactionWithoutSaveAsync(new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                payment_id = payment.Id,
                GatewayResponse = JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            });

            var status = ExtractPayOsStatus(request.Data);

            if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                var bookingId = payment.ReferenceId;
                if (!bookingId.HasValue)
                {
                    throw new FailureException(
                        ErrorCodes.PaymentNotFound,
                        "Payment reference is missing");
                }

                var booking = await _bookingRepository.GetByIdForUpdateAsync(bookingId.Value);
                if (booking == null)
                {
                    throw new NotFoundException(
                        ErrorCodes.BookingNotFound,
                        "Booking not found");
                }

                await ActivatePaidBookingAsync(payment, booking, source: "webhook");

                return Result<object>.Success(new { status = "ok" });
            }

            if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = status == "failed"
                    ? PaymentStatuses.Failed
                    : PaymentStatuses.Cancelled;

                var bookingId = payment.ReferenceId;
                if (bookingId.HasValue)
                {
                    var booking = await _bookingRepository.GetByIdForUpdateAsync(bookingId.Value);

                    // Only the first pending→cancelled transition releases the reserved seats, so
                    // repeated webhook calls cannot release a slot more than once.
                    if (booking != null && booking.Status == BookingStatuses.PendingPayment)
                    {
                        booking.Status = BookingStatuses.Cancelled;
                        booking.CancelledAt = DateTime.UtcNow;
                        booking.UpdatedAt = DateTime.UtcNow;
                        ReleaseReservedSlots(booking);
                    }
                }

                await _bookingRepository.SaveChangesAsync();

                return Result<object>.Success(new { status = "ok" });
            }

            await _paymentRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ignored" });
        }

        public async Task<Result<ReconcilePayOsResponse>> ReconcilePayOsAsync(
            Guid learnerId,
            ReconcilePayOsRequest request)
        {
            if (request.OrderCode is null && request.PaymentId is null)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Either orderCode or paymentId is required");
            }

            var payment = request.PaymentId.HasValue
                ? await _paymentRepository.GetByIdForUpdateAsync(request.PaymentId.Value)
                : await _paymentRepository.GetByOrderCodeForUpdateAsync(request.OrderCode!.Value);

            if (payment == null)
            {
                throw new NotFoundException(ErrorCodes.PaymentNotFound, "Payment not found");
            }

            // Ownership guard — a learner may only reconcile their own payment.
            if (payment.UserId != learnerId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You do not have access to this payment");
            }

            if (payment.Method != PaymentMethods.PayOs)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Payment is not a PayOS payment");
            }

            var bookingId = payment.ReferenceId;
            var booking = bookingId.HasValue
                ? await _bookingRepository.GetByIdForUpdateAsync(bookingId.Value)
                : null;

            // ── Already settled: idempotent success, no PayOS call needed ─────────
            if (payment.Status == PaymentStatuses.Paid &&
                booking != null &&
                booking.Status == BookingStatuses.Active)
            {
                return Result<ReconcilePayOsResponse>.Success(
                    BuildReconcileResponse(payment, booking, payOsStatus: null,
                        "Payment already confirmed. Booking is active."));
            }

            // ── Terminal failure already recorded ─────────────────────────────────
            if (payment.Status == PaymentStatuses.Cancelled ||
                payment.Status == PaymentStatuses.Failed)
            {
                return Result<ReconcilePayOsResponse>.Success(
                    BuildReconcileResponse(payment, booking, payOsStatus: null,
                        "Payment was cancelled or failed."));
            }

            // ── Verify the real state against PayOS (never the frontend query string) ──
            if (!payment.OrderCode.HasValue)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Payment has no PayOS orderCode to reconcile");
            }

            var payOs = await _payOsService.GetPaymentStatusAsync(payment.OrderCode.Value);
            var payOsStatus = payOs.Status; // PAID | PENDING | PROCESSING | CANCELLED | EXPIRED

            if (string.Equals(payOsStatus, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                if (booking == null)
                {
                    throw new NotFoundException(ErrorCodes.BookingNotFound, "Booking not found");
                }

                // Audit the gateway response, then activate idempotently.
                await _paymentRepository.AddTransactionWithoutSaveAsync(new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    payment_id = payment.Id,
                    GatewayResponse = payOs.RawJson,
                    CreatedAt = DateTime.UtcNow
                });

                await ActivatePaidBookingAsync(payment, booking, source: "reconcile");

                return Result<ReconcilePayOsResponse>.Success(
                    BuildReconcileResponse(payment, booking, payOsStatus,
                        "Payment confirmed by PayOS. Booking is now active."));
            }

            if (string.Equals(payOsStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payOsStatus, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = string.Equals(payOsStatus, "EXPIRED", StringComparison.OrdinalIgnoreCase)
                    ? PaymentStatuses.Failed
                    : PaymentStatuses.Cancelled;

                if (booking != null && booking.Status == BookingStatuses.PendingPayment)
                {
                    booking.Status = BookingStatuses.Cancelled;
                    booking.CancelledAt = DateTime.UtcNow;
                    booking.UpdatedAt = DateTime.UtcNow;
                    ReleaseReservedSlots(booking);
                }

                await _bookingRepository.SaveChangesAsync();

                return Result<ReconcilePayOsResponse>.Success(
                    BuildReconcileResponse(payment, booking, payOsStatus,
                        "PayOS reports the payment was cancelled or expired."));
            }

            // PENDING / PROCESSING / unknown — PayOS has not confirmed payment yet.
            // Do NOT activate; return the current status so the learner can retry.
            return Result<ReconcilePayOsResponse>.Success(
                BuildReconcileResponse(payment, booking, payOsStatus,
                    "Payment is still being confirmed. Please try syncing again shortly."));
        }

        public async Task<Result<PagedResult<BookingResponse>>> GetMyBookingsAsync(
            Guid learnerId,
            BookingFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var (items, totalCount) = await _bookingRepository.GetPagedByLearnerAsync(learnerId, filter);

            var response = new PagedResult<BookingResponse>(
                await MapWithUsageAsync(items),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<BookingResponse>>.Success(response);
        }

        public async Task<Result<BookingResponse>> GetMyBookingByIdAsync(
            Guid learnerId,
            Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdForLearnerAsync(learnerId, bookingId);

            if (booking == null)
            {
                var existing = await _bookingRepository.GetByIdAsync(bookingId);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.BookingNotOwned,
                        "Booking is not owned by the current learner");
                }

                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            var usage = await _sessionUsageService.GetAsync(booking.Id, booking.TotalSessions);
            return Result<BookingResponse>.Success(booking.ToResponse(usage));
        }

        public async Task<Result<PagedResult<BookingResponse>>> GetCoachBookingsAsync(
            Guid coachId,
            BookingFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var (items, totalCount) = await _bookingRepository.GetPagedByCoachAsync(coachId, filter);

            var response = new PagedResult<BookingResponse>(
                await MapWithUsageAsync(items),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<BookingResponse>>.Success(response);
        }

        public async Task<Result<BookingResponse>> GetCoachBookingByIdAsync(
            Guid coachId,
            Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdForCoachAsync(coachId, bookingId);

            if (booking == null)
            {
                var existing = await _bookingRepository.GetByIdAsync(bookingId);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.BookingNotOwned,
                        "Booking is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            var usage = await _sessionUsageService.GetAsync(booking.Id, booking.TotalSessions);
            return Result<BookingResponse>.Success(booking.ToResponse(usage));
        }

        private Booking CreateBookingSnapshot(
            Core.Entities.TrainingPackage trainingPackage,
            Guid learnerId,
            string status)
        {
            if (PlatformFeeRate <= 0 || PlatformFeeRate >= 1)
            {
                throw new FailureException(
                    ErrorCodes.InvalidCommissionRate,
                    "Invalid platform commission rate");
            }

            var now = DateTime.UtcNow;
            var totalAmount = trainingPackage.Price;
            var platformFeeAmount = totalAmount * PlatformFeeRate;
            var coachReceiveAmount = totalAmount - platformFeeAmount;
            var totalSessions = trainingPackage.SessionCount;
            var perSessionAmount = totalSessions > 0
                ? coachReceiveAmount / totalSessions
                : 0m;

            return new Booking
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CoachId = trainingPackage.CoachId,
                TrainingPackageId = trainingPackage.Id,
                TotalAmount = totalAmount,
                PlatformFeeRate = PlatformFeeRate,
                PlatformFeeAmount = platformFeeAmount,
                CoachReceiveAmount = coachReceiveAmount,
                PerSessionCoachAmount = perSessionAmount,
                TotalSessions = totalSessions,
                CompletedSessions = 0,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
                // NOTE: do not attach the (no-tracking) TrainingPackage navigation here.
                // The booking is added to the context as a new graph root, which would
                // mark the existing package/sport as Added and trigger spurious INSERTs.
                // The TrainingPackageId FK above is sufficient for persistence; the
                // navigation is set after saving only when needed for the response.
            };
        }

        /// <summary>
        /// Capacity gate + seat reservation for the new purchase flow. Rejects the whole purchase if
        /// the package has no schedule, or if ANY scheduled session is already full. Otherwise consumes
        /// one seat on every session slot (bumping the optimistic concurrency token so the last seat
        /// cannot be sold twice under concurrent purchases). Mutates the tracked slots in place; the
        /// caller commits them in its unit of work.
        /// </summary>
        private static void ReserveSlots(IReadOnlyList<TrainingPackageSessionSlot> slots)
        {
            if (slots == null || slots.Count == 0)
            {
                throw new ConflictException(
                    ErrorCodes.TrainingPackageHasNoSchedule,
                    "This training package has no scheduled sessions and cannot be purchased");
            }

            foreach (var slot in slots)
            {
                if (slot.Status == TrainingPackageSessionSlotStatuses.Cancelled ||
                    slot.BookedParticipants >= slot.MaxParticipants)
                {
                    throw new ConflictException(
                        ErrorCodes.TrainingPackageSessionSlotFull,
                        $"Session {slot.SessionNumber} is full and cannot be booked");
                }
            }

            var now = DateTime.UtcNow;
            foreach (var slot in slots)
            {
                slot.BookedParticipants++;
                slot.Status = slot.BookedParticipants >= slot.MaxParticipants
                    ? TrainingPackageSessionSlotStatuses.Full
                    : TrainingPackageSessionSlotStatuses.Open;
                slot.Version++;
                slot.UpdatedAt = now;
            }
        }

        /// <summary>
        /// Releases the seats reserved by a pending booking when its payment is cancelled / failed /
        /// expired. Guarded by the caller so it runs only on the first pending→cancelled transition.
        /// </summary>
        private static void ReleaseReservedSlots(Booking booking)
        {
            var slots = booking.TrainingPackage?.SessionSlots;
            if (slots == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var slot in slots)
            {
                if (slot.BookedParticipants <= 0)
                {
                    continue;
                }

                slot.BookedParticipants--;
                if (slot.Status != TrainingPackageSessionSlotStatuses.Cancelled)
                {
                    slot.Status = TrainingPackageSessionSlotStatuses.Open;
                }

                slot.Version++;
                slot.UpdatedAt = now;
            }
        }

        /// <summary>
        /// Auto-creates one scheduled training session per package slot for the booking. Idempotent:
        /// a no-op if the booking already has generated sessions (so repeated webhook/reconcile calls
        /// never duplicate sessions). Adds the sessions to the unit of work without saving.
        /// </summary>
        private async Task GenerateSessionsAsync(Booking booking, IEnumerable<TrainingPackageSessionSlot>? slots)
        {
            if (slots == null)
            {
                return;
            }

            var slotList = slots.ToList();
            if (slotList.Count == 0)
            {
                return;
            }

            if (await _trainingSessionRepository.HasPackageGeneratedSessionsAsync(booking.Id))
            {
                return;
            }

            var sessions = slotList
                .OrderBy(s => s.SessionNumber)
                .Select(slot => slot.ToGeneratedSession(booking))
                .ToList();

            await _trainingSessionRepository.AddRangeWithoutSaveAsync(sessions);
        }

        /// <summary>
        /// Shared, idempotent activation for a paid booking. Safe to call from both the
        /// PayOS webhook and the reconcile endpoint; if the booking is already active it
        /// performs no duplicate side effects (no extra notifications, wallet creation, etc.).
        ///
        /// Requires <paramref name="booking"/> to be a tracked entity with TrainingPackage
        /// loaded (used to compute ExpiresAt) and <paramref name="payment"/> to be tracked.
        /// </summary>
        /// <param name="source">Audit hint: "webhook" or "reconcile".</param>
        private async Task ActivatePaidBookingAsync(Payment payment, Booking booking, string source)
        {
            var alreadyActive = booking.Status == BookingStatuses.Active;

            // Payment is always reconciled to paid (idempotent — no-op if already paid).
            if (payment.Status != PaymentStatuses.Paid)
            {
                payment.Status = PaymentStatuses.Paid;
            }

            payment.PaidAt ??= DateTime.UtcNow;

            if (!alreadyActive)
            {
                if (booking.TrainingPackage == null)
                {
                    // Both callers Include the package; guard defensively rather than NRE → 500.
                    throw new FailureException(
                        ErrorCodes.BookingNotFound,
                        "Booking training package is not loaded; cannot activate booking");
                }

                var paidAt = DateTime.UtcNow;
                booking.Status = BookingStatuses.Active;
                booking.PaidAt ??= paidAt;
                booking.ExpiresAt = booking.PaidAt.Value.AddDays(booking.TrainingPackage.DurationDays);
                booking.UpdatedAt = paidAt;

                // Bump the optimistic concurrency token so two activations racing (webhook + reconcile)
                // cannot both create the generated sessions — the loser's SaveChanges raises a
                // concurrency conflict (surfaced as 409) rather than violating the unique session index.
                booking.Version++;

                // Auto-create one scheduled training session per reserved package slot (idempotent —
                // skipped if this booking already has generated sessions). Seats were already reserved
                // when the pending payment was created, so no further reservation happens here.
                await GenerateSessionsAsync(booking, booking.TrainingPackage.SessionSlots);

                // Side effect: ensure the coach wallet exists (idempotent).
                await EnsureCoachWalletAsync(booking.CoachId);
            }

            // Persist payment + booking + generated sessions (and any logged PaymentTransaction) in one save.
            await _bookingRepository.SaveChangesAsync();

            // Side effect: notifications — only on the first transition to active.
            if (!alreadyActive)
            {
                await SendBookingActivatedNotificationsAsync(booking);
            }
        }

        private async Task EnsureBookingActivatedAsync(Booking booking, bool notify)
        {
            await EnsureCoachWalletAsync(booking.CoachId);

            if (!notify)
            {
                return;
            }

            await SendBookingActivatedNotificationsAsync(booking);
        }

        /// <summary>
        /// Booking-activation notifications. Raised AFTER the booking has been committed (manual
        /// purchase, PayOS webhook, or reconcile), so a notification failure is logged with context
        /// but never turned into an error response.
        /// </summary>
        private async Task SendBookingActivatedNotificationsAsync(Booking booking)
        {
            var notifications = new[]
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.CoachId,
                    Title = "You have a new booking",
                    Content = "A learner has purchased your training package",
                    Type = NotificationTypeConstants.Booking,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.LearnerId,
                    Title = "Your booking is active",
                    Content = "Your training sessions have been scheduled",
                    Type = NotificationTypeConstants.Booking,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var error = await _notificationRepository.TryAddAndSaveAsync(notifications);
            if (error is not null)
            {
                _logger.LogError(
                    error,
                    "Notification side-effect failed after booking activation (booking already committed). " +
                    "bookingId={BookingId} coachId={CoachId} learnerId={LearnerId} types={Types}",
                    booking.Id,
                    booking.CoachId,
                    booking.LearnerId,
                    NotificationTypeConstants.Booking);
            }
        }

        private static ReconcilePayOsResponse BuildReconcileResponse(
            Payment payment,
            Booking? booking,
            string? payOsStatus,
            string message)
        {
            return new ReconcilePayOsResponse
            {
                PaymentId = payment.Id,
                OrderCode = payment.OrderCode,
                PaymentStatus = payment.Status,
                BookingId = booking?.Id ?? payment.ReferenceId,
                BookingStatus = booking?.Status,
                Activated = booking?.Status == BookingStatuses.Active,
                PayOsStatus = payOsStatus,
                Message = message
            };
        }

        private async Task EnsureCoachWalletAsync(Guid coachId)
        {
            var wallet = await _coachWalletRepository.GetByCoachIdAsync(coachId);
            if (wallet != null)
            {
                return;
            }

            await _coachWalletRepository.AddAsync(new CoachWallet
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalEarned = 0,
                TotalWithdrawn = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        private static string ExtractPayOsStatus(JsonElement data)
        {
            if (data.TryGetProperty("status", out var statusElement))
            {
                var statusValue = statusElement.GetString();
                if (!string.IsNullOrWhiteSpace(statusValue))
                {
                    return statusValue.Trim().ToLowerInvariant();
                }
            }

            if (data.TryGetProperty("code", out var codeElement))
            {
                var codeValue = codeElement.GetString();
                if (string.Equals(codeValue, "00", StringComparison.OrdinalIgnoreCase))
                {
                    return "paid";
                }
            }

            return string.Empty;
        }
    }
}
