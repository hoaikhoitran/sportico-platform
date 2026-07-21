namespace SporticoApp.Application.DTOs.AdminPayments
{
    /// <summary>Filter for the top-coaches / top-sports revenue leaderboards.</summary>
    public class TopEntitiesFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int Limit { get; set; } = 5;
    }
}
