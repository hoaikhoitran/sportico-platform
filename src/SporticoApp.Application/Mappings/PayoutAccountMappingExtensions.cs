using SporticoApp.Application.DTOs.PayoutAccounts;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class PayoutAccountMappingExtensions
    {
        public static CoachPayoutAccount ToEntity(
            this UpsertCoachPayoutAccountRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new CoachPayoutAccount
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                PayoutMethod = request.PayoutMethod.Trim(),
                BankName = request.BankName.Trim(),
                BankBin = request.BankBin.Trim(),
                BankAccountNumber = request.BankAccountNumber.Trim(),
                BankAccountHolder = request.BankAccountHolder.Trim(),
                Status = PayoutAccountStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this CoachPayoutAccount account,
            UpsertCoachPayoutAccountRequest request)
        {
            account.PayoutMethod = request.PayoutMethod.Trim();
            account.BankName = request.BankName.Trim();
            account.BankBin = request.BankBin.Trim();
            account.BankAccountNumber = request.BankAccountNumber.Trim();
            account.BankAccountHolder = request.BankAccountHolder.Trim();
            account.Status = PayoutAccountStatuses.Pending;
            account.UpdatedAt = DateTime.UtcNow;
        }

        public static CoachPayoutAccountResponse ToResponse(this CoachPayoutAccount account)
        {
            return new CoachPayoutAccountResponse
            {
                Id = account.Id,
                CoachId = account.CoachId,
                PayoutMethod = account.PayoutMethod,
                BankName = account.BankName,
                BankBin = account.BankBin,
                BankAccountNumber = account.BankAccountNumber,
                BankAccountHolder = account.BankAccountHolder,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            };
        }
    }
}
