using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Packages
{
    public class PurchaseCoachPackagePayOsResponse
    {
        public Guid CoachPackageId { get; set; }

        public Guid PaymentId { get; set; }

        public long OrderCode { get; set; }

        public string CheckoutUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiredAt { get; set; }    
    }
}
