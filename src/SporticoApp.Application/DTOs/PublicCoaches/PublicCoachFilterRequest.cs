using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.PublicCoaches
{
    public class PublicCoachFilterRequest
    {
        public string? Keyword { get; set; }

        public int? SportId { get; set; }

        public string? City { get; set; }

        public string? District { get; set; }

        public bool? IsOnlineAvailable { get; set; }

        public bool? IsOfflineAvailable { get; set; }

        public decimal? MinRating { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
