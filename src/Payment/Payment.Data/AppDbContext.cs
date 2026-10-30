using Microsoft.EntityFrameworkCore;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Data
{
    // Sin Identity: los usuarios viven en el servidor de autorización, este servicio solo valida tokens.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentEntity>(e =>
            {
                e.ToTable("Payments");
                e.HasKey(p => p.PaymentId);

                e.Property(p => p.PaymentId)
                    .HasColumnType("uniqueidentifier")
                    .ValueGeneratedNever();
                e.Property(p => p.BookingId).HasColumnType("uniqueidentifier");
                e.Property(p => p.PaymentAttemptId).HasColumnType("uniqueidentifier");
                e.Property(p => p.Amount).HasPrecision(18, 2);
                e.Property(p => p.Status).HasConversion<int>().HasColumnType("int");
                e.Property(p => p.CreatedAt).HasColumnType("datetimeoffset");

                // Idempotencia: un mismo intento de pago no se puede registrar dos veces.
                e.HasIndex(p => new { p.BookingId, p.PaymentAttemptId }).IsUnique();

                // Solo un pago PROCESSING (0) o APPROVED (1) por BookingId.
                // REJECTED libera la reserva para otro pago. Si cambia el orden
                // del enum PaymentStatus, hay que actualizar el filtro.
                e.HasIndex(p => p.BookingId)
                    .IsUnique()
                    .HasFilter("[Status] IN (0, 1)");
            });
        }
    }
}
