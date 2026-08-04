namespace SporticoApp.Application.DTOs.Bookings
{
    public class PurchaseTrainingPackageManualRequest
    {
        public Guid TrainingPackageId { get; set; }

        /// <summary>Optional. Re-validated and re-priced server-side — never trust a client-sent discount.</summary>
        public string? VoucherCode { get; set; }
    }
}
