using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Infraestructura.Data
{
    // Sin Identity: los usuarios viven en el servidor de autorización, este servicio solo valida tokens.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
