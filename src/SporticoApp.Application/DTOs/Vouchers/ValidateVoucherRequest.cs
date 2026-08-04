namespace SporticoApp.Application.DTOs.Vouchers
{
    public class ValidateVoucherRequest
    {
        public string Code { get; set; } = string.Empty;

        public Guid TrainingPackageId { get; set; }
    }
}
