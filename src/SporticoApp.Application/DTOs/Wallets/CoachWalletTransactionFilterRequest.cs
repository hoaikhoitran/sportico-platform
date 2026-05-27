namespace SporticoApp.Application.DTOs.Wallets
{
    public class CoachWalletTransactionFilterRequest
    {
        public string? Type { get; set; }

        public string? Direction { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
