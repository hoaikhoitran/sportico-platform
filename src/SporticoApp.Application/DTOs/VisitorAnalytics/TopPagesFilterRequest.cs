namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class TopPagesFilterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int Limit { get; set; } = 10;
    }
}
