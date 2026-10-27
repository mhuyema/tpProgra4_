namespace Booking.Api.Core
{
    public class Entity 
    {
        public Guid Id { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdate { get; set; }

        public DateTime DeletedAt { get; set; }
    }
}
