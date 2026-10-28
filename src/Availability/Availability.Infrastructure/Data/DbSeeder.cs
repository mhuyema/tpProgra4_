using Availability.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Data
{
    /// <summary>
    /// Carga eventos y asientos de prueba. Es idempotente: si ya hay eventos no hace nada.
    /// Solo se usa en Development.
    /// </summary>
    public static class DbSeeder
    {
        private const string Filas = "ABCDEFGHIJ";
        private const int AsientosPorFila = 14;

        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Eventos.IgnoreQueryFilters().AnyAsync()) return;

            var hoy = DateTime.UtcNow.Date;
            var eventos = new[]
            {
                Crear("Eventos en vivo", "Estadio Obras Sanitarias, Buenos Aires", hoy.AddDays(14).AddHours(21), "/imgs/recitales/EstadioObrasSanitariasRecital-1.png"),
                Crear("Niceto", "Niceto Club, Córdoba", hoy.AddDays(21).AddHours(23), "/imgs/recitales/nicetoclub.jpg"),
                Crear("Parque Centenario", "Parque Centenario, Rosario", hoy.AddDays(23).AddHours(18), "/imgs/recitales/recitales-aire-libre-caba-1071961.jpg"),
                Crear("Estadio Monumental", "Estadio Monumental, Mendoza", hoy.AddDays(29).AddHours(20), "/imgs/recitales/Estadio-Monumental-recitales.webp"),
                Crear("Festival Aire Libre", "Hipódromo, La Plata", hoy.AddDays(36).AddHours(16), "/imgs/recitales/images.jpg"),
            };

            db.Eventos.AddRange(eventos);
            await db.SaveChangesAsync();
        }

        private static Evento Crear(string nombre, string lugar, DateTime inicio, string imagen)
        {
            var evento = new Evento
            {
                Id = Guid.NewGuid(),
                Nombre = nombre,
                Descripcion = $"{nombre} en vivo.",
                Lugar = lugar,
                FechaInicio = inicio,
                FechaFin = inicio.AddHours(5),
                Capacidad = Filas.Length * AsientosPorFila,
                ImageUrl = imagen,
            };

            for (var f = 0; f < Filas.Length; f++)
            {
                var precio = f < 3 ? 35000m : f < 7 ? 28000m : 20000m;
                for (var n = 1; n <= AsientosPorFila; n++)
                {
                    evento.Asientos.Add(new Asiento
                    {
                        Id = Guid.NewGuid(),
                        EventoId = evento.Id,
                        Fila = Filas[f].ToString(),
                        Numero = n,
                        Precio = precio,
                    });
                }
            }

            return evento;
        }
    }
}
