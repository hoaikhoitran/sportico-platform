namespace SporticoApp.Application.DTOs.Withdrawals
{
    /// <summary>
    /// Safe diagnostic summary of the payout account linked to a withdrawal request.
    /// Intended for admin debugging of PayOS Chi rejections — never expose to non-admin callers.
    /// Contains no credentials, no full account number.
    /// </summary>
    public class PayoutAccountDebugResponse
    {
        public Guid WithdrawalId { get; set; }

        public Guid? PayoutAccountId { get; set; }

        // ── Bank identity ────────────────────────────────────────────────────
        public string? BankName { get; set; }

        public string? BankBin { get; set; }

        /// <summary>Whether BankBin is exactly 6 ASCII digits.</summary>
        public bool BankBinValid { get; set; }

        // ── Account number ───────────────────────────────────────────────────
        /// <summary>Masked account number, e.g. "01****89".</summary>
        public string? MaskedAccountNumber { get; set; }

        public int AccountNumberLength { get; set; }

        /// <summary>Whether the account number consists of digits only (no spaces or dashes).</summary>
        public bool AccountNumberDigitsOnly { get; set; }

        // ── Account holder name ──────────────────────────────────────────────
        /// <summary>Raw value stored in BankAccountHolder (no masking — not a secret).</summary>
        public string? RawAccountHolder { get; set; }

        /// <summary>
        /// Value after normalisation: uppercase, Vietnamese diacritics removed, whitespace collapsed.
        /// This is what PayOS receives in the toAccountName field and what is signed.
        /// </summary>
        public string? NormalizedAccountHolder { get; set; }

        public int RawAccountHolderLength { get; set; }

        public int NormalizedAccountHolderLength { get; set; }

        /// <summary>
        /// True when the raw name contained characters that were stripped during normalisation
        /// (diacritics, non-ASCII), meaning the name in the database differs from what was sent to PayOS.
        /// </summary>
        public bool HadDiacriticsOrNonAscii { get; set; }

        // ── Withdrawal status ────────────────────────────────────────────────
        public decimal WithdrawalAmount { get; set; }

        public string? WithdrawalStatus { get; set; }

        public string? FailureReason { get; set; }
    }
}
