using Availability.Domain.Entities;
using Availability.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;


namespace Availability.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Asiento> Asientos { get; set; }
        public DbSet<SolicitudReserva> Solicitudes { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Evento>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Asiento>().Property(a => a.RowVersion).IsRowVersion();

            modelBuilder.Entity<Evento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
                entity.Property(e => e.Lugar).HasMaxLength(200);
                entity.Property(e => e.FechaInicio).IsRequired();
                entity.Property(e => e.FechaFin).IsRequired();
                entity.Property(e => e.Capacidad).IsRequired();
                entity.Property(e => e.ImageUrl).HasMaxLength(300);
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            modelBuilder.Entity<Asiento>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Fila).IsRequired().HasMaxLength(10);
                entity.Property(a => a.Numero).IsRequired();
                entity.Property(a => a.Precio).IsRequired().HasColumnType("decimal(18,2)");

                entity.HasOne(a => a.Evento)
                      .WithMany(e => e.Asientos)
                      .HasForeignKey(a => a.EventoId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(a => new { a.EventoId, a.Fila, a.Numero }).IsUnique();
                entity.HasQueryFilter(e => !e.IsDeleted);

            });
            modelBuilder.Entity<SolicitudReserva>(entity =>
            {
                entity.HasKey(s => s.RequestId);
                entity.Property(s => s.AsientoId).IsRequired();
                entity.Property(s => s.UsuarioId).IsRequired().HasMaxLength(100);
                entity.Property(s => s.EventoId).IsRequired();
          

            });

        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entrada in ChangeTracker.Entries<EntidadBase>().Where(e => e.State == EntityState.Added))
                entrada.Entity.FechaCreacion = DateTime.UtcNow;

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
