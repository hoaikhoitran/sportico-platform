namespace SporticoApp.Application.DTOs.Withdrawals
{
    public class WithdrawalRequestFilterRequest
    {
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
