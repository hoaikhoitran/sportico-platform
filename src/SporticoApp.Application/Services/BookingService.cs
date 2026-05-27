using System.Text.Json;
using FluentValidation;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
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
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayOsService _payOsService;
        private readonly ICoachWalletRepository _coachWalletRepository;
        private readonly IChatRepository _chatRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<PurchaseTrainingPackageManualRequest> _manualValidator;
        private readonly IValidator<PurchaseTrainingPackagePayOsRequest> _payOsValidator;
        private readonly IValidator<BookingFilterRequest> _filterValidator;

        public BookingService(
            ITrainingPackageRepository trainingPackageRepository,
            IBookingRepository bookingRepository,
            IPaymentRepository paymentRepository,
            IPayOsService payOsService,
            ICoachWalletRepository coachWalletRepository,
            IChatRepository chatRepository,
            INotificationRepository notificationRepository,
            IValidator<PurchaseTrainingPackageManualRequest> manualValidator,
            IValidator<PurchaseTrainingPackagePayOsRequest> payOsValidator,
            IValidator<BookingFilterRequest> filterValidator)
        {
            _trainingPackageRepository = trainingPackageRepository;
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
            _payOsService = payOsService;
            _coachWalletRepository = coachWalletRepository;
            _chatRepository = chatRepository;
            _notificationRepository = notificationRepository;
            _manualValidator = manualValidator;
            _payOsValidator = payOsValidator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<BookingResponse>> PurchaseManualAsync(
            Guid learnerId,
            PurchaseTrainingPackageManualRequest request)
        {
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

            var booking = CreateBookingSnapshot(trainingPackage, learnerId, BookingStatuses.Active);
            booking.PaidAt = DateTime.UtcNow;

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
                if (payment.Status != PaymentStatuses.Paid)
                {
                    payment.Status = PaymentStatuses.Paid;
                    payment.PaidAt = DateTime.UtcNow;
                }

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

                var shouldNotify = booking.Status != BookingStatuses.Active;

                if (shouldNotify)
                {
                    booking.Status = BookingStatuses.Active;
                    booking.PaidAt = DateTime.UtcNow;
                }

                await _bookingRepository.SaveChangesAsync();

                if (shouldNotify)
                {
                    await EnsureBookingActivatedAsync(booking, true);
                }

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
                    if (booking != null)
                    {
                        booking.Status = BookingStatuses.Cancelled;
                        booking.CancelledAt = DateTime.UtcNow;
                    }
                }

                await _bookingRepository.SaveChangesAsync();

                return Result<object>.Success(new { status = "ok" });
            }

            await _paymentRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ignored" });
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
                items.Select(x => x.ToResponse()).ToList(),
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

            return Result<BookingResponse>.Success(booking.ToResponse());
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
                items.Select(x => x.ToResponse()).ToList(),
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

            return Result<BookingResponse>.Success(booking.ToResponse());
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

        private async Task EnsureBookingActivatedAsync(Booking booking, bool notify)
        {
            await EnsureCoachWalletAsync(booking.CoachId);
            await EnsureChatRoomAsync(booking.LearnerId, booking.CoachId);

            if (!notify)
            {
                return;
            }

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = booking.CoachId,
                Title = "You have a new booking",
                Content = "A learner has purchased your training package",
                Type = NotificationTypeConstants.Booking,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = booking.LearnerId,
                Title = "Your booking is active",
                Content = "You can now request training sessions",
                Type = NotificationTypeConstants.Booking,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();
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

        private async Task EnsureChatRoomAsync(Guid learnerId, Guid coachId)
        {
            var room = await _chatRepository.GetRoomByUsersAsync(learnerId, coachId);
            if (room != null)
            {
                return;
            }

            var user1Id = learnerId.CompareTo(coachId) <= 0 ? learnerId : coachId;
            var user2Id = learnerId.CompareTo(coachId) <= 0 ? coachId : learnerId;

            await _chatRepository.AddRoomAsync(new ChatRoom
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow
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
