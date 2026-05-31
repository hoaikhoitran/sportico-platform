using SporticoApp.Application.DTOs.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPayOsService
    {
        Task<CreatePayOsPaymentResult> CreatePaymentLinkAsync(
            CreatePayOsPaymentRequest request);

        /// <summary>
        /// Queries PayOS for the real payment state by orderCode
        /// (GET /v2/payment-requests/{orderCode}). Used by the reconcile flow so the
        /// backend never trusts the frontend success-page query string.
        /// </summary>
        Task<PayOsPaymentStatusResult> GetPaymentStatusAsync(long orderCode);

        bool VerifyWebhookSignature(
            object data,
            string signature);
    }
}
