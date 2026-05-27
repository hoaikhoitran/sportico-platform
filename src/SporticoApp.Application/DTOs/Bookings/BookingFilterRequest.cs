namespace SporticoApp.Application.DTOs.Bookings
{
    public class BookingFilterRequest
    {
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
