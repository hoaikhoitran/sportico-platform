using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Payments
{
    public class CreatePayOsPaymentRequest
    {
        public long OrderCode { get; set; }

        public int Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public string BuyerName { get; set; } = string.Empty;

        public string? BuyerEmail { get; set; }

        public string? BuyerPhone { get; set; }
    }
}
