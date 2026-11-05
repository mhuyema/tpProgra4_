using Broker.Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Api.Handlers;
using Notifications.Api.Services;
using Xunit;

namespace Notifications.Tests;

// Pruebas de los dos handlers. Se arma el handler con dependencias falsas (ver Fakes.cs), se le pasa un
// mensaje como si hubiera llegado del broker y se verifica qué se envió y qué se auditó.
public class HandlersTests
{
    // NullLogger es un logger que no escribe nada: sirve cuando el logger no importa para la prueba.
    private static ReservationCreatedHandler NuevoCreated(
        InMemoryNotificationStore store, FakeSender sender, FakeAuditPublisher audit) =>
        new(store, sender, audit, NullLogger<ReservationCreatedHandler>.Instance);

    private static ReservationConfirmedHandler NuevoConfirmed(
        InMemoryNotificationStore store, FakeSender sender, FakeAuditPublisher audit) =>
        new(store, sender, audit, NullLogger<ReservationConfirmedHandler>.Instance);

    private static ReservationCreatedMessage Creada(Guid solicitud, string userId = "ana") =>
        new(solicitud, Guid.NewGuid(), EventId: 1, SeatId: 7, userId, Amount: 1500m, ExpiresAt: DateTime.UtcNow.AddMinutes(5));

    private static ReservationConfirmedMessage Confirmada(Guid solicitud, string userId = "ana") =>
        new(solicitud, Guid.NewGuid(), EventId: 1, SeatId: 7, userId, Amount: 1500m);

    // ---------- Reserva creada ----------

    [Fact]
    public async Task Created_MensajeValido_EnviaUnAvisoYAuditaInicioYFin()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var solicitud = Guid.NewGuid();

        await NuevoCreated(store, sender, audit).HandleAsync(Creada(solicitud), solicitud.ToString(), CancellationToken.None);

        var enviado = Assert.Single(sender.Sent);
        Assert.Equal("ReservaCreada", enviado.Type);
        Assert.Equal("ana", enviado.UserId);
        // Punto 3.f del TP: tiene que quedar el INICIO y el FIN de la cadena de sucesos.
        Assert.Equal(new[] { "Init", "End" }, audit.Events.Select(e => e.Action));
        Assert.Equal("Enviada", audit.Events[1].Result);
    }

    [Fact]
    public async Task Created_MensajeRepetido_NoVuelveAEnviar()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var handler = NuevoCreated(store, sender, audit);
        var solicitud = Guid.NewGuid();

        await handler.HandleAsync(Creada(solicitud), solicitud.ToString(), CancellationToken.None);
        await handler.HandleAsync(Creada(solicitud), solicitud.ToString(), CancellationToken.None);

        Assert.Single(sender.Sent);
        Assert.Equal("Duplicada", audit.Events[^1].Result);
    }

    [Fact]
    public async Task Created_MensajeSinUserId_SeDescartaYAuditaElError()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var solicitud = Guid.NewGuid();

        // No lanza excepción: un mensaje inválido no se arregla reintentando, así que se descarta.
        await NuevoCreated(store, sender, audit).HandleAsync(Creada(solicitud, userId: ""), solicitud.ToString(), CancellationToken.None);

        Assert.Empty(sender.Sent);
        Assert.Equal("Error", Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Created_FallaElEnvio_AuditaElErrorYRelanzaParaQueElBrokerReintente()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender { Fail = true };
        var audit = new FakeAuditPublisher();
        var solicitud = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NuevoCreated(store, sender, audit).HandleAsync(Creada(solicitud), solicitud.ToString(), CancellationToken.None));

        Assert.Contains(audit.Events, e => e.Action == "Error");
    }

    // ---------- Reserva confirmada ----------

    [Fact]
    public async Task Confirmed_MensajeValido_EnviaElAvisoConElMonto()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var solicitud = Guid.NewGuid();

        await NuevoConfirmed(store, sender, audit).HandleAsync(Confirmada(solicitud), solicitud.ToString(), CancellationToken.None);

        var enviado = Assert.Single(sender.Sent);
        Assert.Equal("ReservaConfirmada", enviado.Type);
        Assert.Contains("confirmada", enviado.Message);
        Assert.Equal(new[] { "Init", "End" }, audit.Events.Select(e => e.Action));
    }

    [Fact]
    public async Task Confirmed_MensajeRepetido_NoVuelveAEnviar()
    {
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var handler = NuevoConfirmed(store, sender, audit);
        var solicitud = Guid.NewGuid();

        await handler.HandleAsync(Confirmada(solicitud), solicitud.ToString(), CancellationToken.None);
        await handler.HandleAsync(Confirmada(solicitud), solicitud.ToString(), CancellationToken.None);

        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task CreadaYConfirmada_DeLaMismaSolicitud_GeneranDosAvisosDistintos()
    {
        // Es el recorrido feliz completo: primero "creada", después "confirmada".
        var store = new InMemoryNotificationStore();
        var sender = new FakeSender();
        var audit = new FakeAuditPublisher();
        var solicitud = Guid.NewGuid();

        await NuevoCreated(store, sender, audit).HandleAsync(Creada(solicitud), solicitud.ToString(), CancellationToken.None);
        await NuevoConfirmed(store, sender, audit).HandleAsync(Confirmada(solicitud), solicitud.ToString(), CancellationToken.None);

        Assert.Equal(new[] { "ReservaCreada", "ReservaConfirmada" }, sender.Sent.Select(n => n.Type));
        Assert.Equal(2, store.GetByUser("ana").Count);
    }
}