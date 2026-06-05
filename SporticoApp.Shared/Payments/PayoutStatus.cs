using System;
using System.Collections.Generic;

namespace SporticoApp.Shared.Payments
{
    /// <summary>Normalized outcome of a PayOS payout state.</summary>
    public enum PayoutOutcome
    {
        Success,
        Processing,
        Failed
    }

    /// <summary>
    /// Single, centralized normalization of PayOS payout (Chi) status strings. Used by both the
    /// payout-initiation path and the refresh/reconciliation path so the mapping never diverges.
    ///
    /// PayOS documents PROCESSING | SUCCEEDED | FAILED | CANCELLED, but real responses (incl. batch
    /// payouts) vary, so common spelling variants are accepted. Any UNKNOWN / empty / pending value
    /// is classified as <see cref="PayoutOutcome.Processing"/> — never finalized or rolled back
    /// blindly (rolling back an unknown state risks a double-payout).
    /// </summary>
    public static class PayoutStatus
    {
        private static readonly HashSet<string> SuccessStates = new(StringComparer.Ordinal)
        {
            "SUCCESS", "SUCCEEDED", "SUCCEED", "PAID", "COMPLETED", "COMPLETE", "DONE"
        };

        private static readonly HashSet<string> FailureStates = new(StringComparer.Ordinal)
        {
            "FAILED", "FAIL", "CANCELLED", "CANCELED", "REJECTED", "REJECT", "ERROR"
        };

        /// <summary>Upper-cases and trims a raw PayOS status (null/empty → "").</summary>
        public static string Normalize(string? rawStatus)
            => (rawStatus ?? string.Empty).Trim().ToUpperInvariant();

        public static bool IsSuccess(string? status) => SuccessStates.Contains(Normalize(status));

        public static bool IsFailure(string? status) => FailureStates.Contains(Normalize(status));

        /// <summary>True for processing/pending/received/unknown/empty — anything not terminal.</summary>
        public static bool IsProcessing(string? status) => !IsSuccess(status) && !IsFailure(status);

        public static PayoutOutcome Classify(string? status)
        {
            if (IsSuccess(status))
            {
                return PayoutOutcome.Success;
            }

            if (IsFailure(status))
            {
                return PayoutOutcome.Failed;
            }

            return PayoutOutcome.Processing;
        }
    }
}
