using Booking.Business.Auth;
using Booking.Business.Messages;
using Booking.Business.Services;
using Booking.Data.Entities;

namespace Booking.Tests;

// Dobles de prueba ("fakes"): versiones falsas y simples de las dependencias reales.
// Permiten probar la autenticación de Booking SIN levantar el Authorization Server ni la base de datos.

// Valida tokens "de mentira": acepta solo el token que le configuramos y le asigna el subject que queramos.
internal sealed class FakeValidadorToken : IValidadorToken
{
    public string? TokenValido { get; init; }
    public string? Subject { get; init; }

    // Si es true, simula que el Authorization Server no responde.
    public bool Caido { get; init; }

    public Task<ResultadoToken> ValidarAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (Caido)
            throw new HttpRequestException("Authorization Server caído.");

        return Task.FromResult(token is not null && token == TokenValido
            ? new ResultadoToken(true, Subject)
            : ResultadoToken.Invalido);
    }
}

// Servicio de reservas falso: solo implementa lo que usa el endpoint de estado. El resto no se debería llamar.
internal sealed class FakeReservaService(Reserva? reserva) : IReservaService
{
    public Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId) => Task.FromResult(reserva);

    public Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage mensaje) => throw new NotImplementedException();
    public Task<Reserva> RechazarPorTokenAsync(SolicitudReservaMessage mensaje) => throw new NotImplementedException();
    public Task<bool> ConfirmarAsync(Guid reservaId) => throw new NotImplementedException();
    public Task<bool> RechazarAsync(Guid reservaId, MotivoRechazo motivo) => throw new NotImplementedException();
    public Task<int> VencerReservasAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<int>> ObtenerAsientosOcupadosPorEventoAsync(int eventoId) => throw new NotImplementedException();
    public Task<Reserva> ProcesarPagoAsync(Guid solicitudId) => throw new NotImplementedException();
}