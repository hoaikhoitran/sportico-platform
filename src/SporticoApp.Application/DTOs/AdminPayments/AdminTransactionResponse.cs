namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>One row of the admin transactions list / recent-transactions feed.</summary>
    public class AdminTransactionResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Method { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string ReferenceType { get; set; } = string.Empty;

        public Guid? ReferenceId { get; set; }

        public string? TransactionCode { get; set; }

        public long? OrderCode { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
