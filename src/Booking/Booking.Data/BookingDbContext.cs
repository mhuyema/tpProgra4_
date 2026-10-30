using Booking.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booking.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reserva>(e =>
        {
            e.HasKey(r => r.Id);

            // Idempotencia: Los brokers entregan "Al menos una vez", asi que el mismo
            // mensaje puede llegar dos veces. SolicitudID evita duplica la reserva
            e.HasIndex(r => r.SolicitudId).IsUnique();

            // Anti doble reserva: Un asiento solo puede tener una reserva activa
            // Indice unico filtrado: aplica solo a Retenida (0) y confirmada (1),
            // por eso Rechazada y Vencida liberan el asiento. Si cambian el orden
            // del enum EstadoReserva, hay que actualizar el filtro.
            e.HasIndex(r => new { r.EventoId, r.AsientoId })
            .IsUnique()
            .HasFilter("[Estado] IN (0, 1)"); // Retenida o Confirmada

            // Define la precisión del importe almacenado en SQL Server.
            e.Property(r => r.Importe).HasPrecision(18, 2);
        });
    }
}