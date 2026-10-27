namespace Booking.Api.DTOS
{
    public class ErrorResponseDTO
    {
        public int StatusCode { get; set; }

        public string Message { get; set; } = default!;

        public IEnumerable<string>? Errors { get; set; }

        public string? Detail { get; set; }
    }
}
