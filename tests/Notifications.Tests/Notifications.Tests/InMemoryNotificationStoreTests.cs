using Notifications.Api.Services;
using Xunit;

namespace Notifications.Tests;

// Pruebas del registro en memoria de avisos. Convención de nombres: Metodo_Situacion_ResultadoEsperado.
// Cada prueba sigue el patrón AAA: Arrange (preparar), Act (ejecutar), Assert (verificar).
public class InMemoryNotificationStoreTests
{
    private static Notification Aviso(Guid requestId, string tipo, string userId) =>
        new(requestId, tipo, userId, "texto", DateTime.UtcNow);

    [Fact]
    public void TryAdd_AvisoNuevo_DevuelveTrue()
    {
        var store = new InMemoryNotificationStore();

        var agregado = store.TryAdd(Aviso(Guid.NewGuid(), "ReservaCreada", "u1"));

        Assert.True(agregado);
    }

    [Fact]
    public void TryAdd_MismaSolicitudYMismoTipo_DevuelveFalse()
    {
        // Es la idempotencia: el broker puede entregar el mismo mensaje dos veces.
        var store = new InMemoryNotificationStore();
        var solicitud = Guid.NewGuid();
        store.TryAdd(Aviso(solicitud, "ReservaCreada", "u1"));

        var segundoIntento = store.TryAdd(Aviso(solicitud, "ReservaCreada", "u1"));

        Assert.False(segundoIntento);
    }

    [Fact]
    public void TryAdd_MismaSolicitudPeroOtroTipo_DevuelveTrue()
    {
        // "Creada" y "Confirmada" de una misma reserva son dos avisos distintos y los dos se guardan.
        var store = new InMemoryNotificationStore();
        var solicitud = Guid.NewGuid();
        store.TryAdd(Aviso(solicitud, "ReservaCreada", "u1"));

        var agregado = store.TryAdd(Aviso(solicitud, "ReservaConfirmada", "u1"));

        Assert.True(agregado);
    }

    [Fact]
    public void GetByUser_DevuelveSoloLosAvisosDeEseUsuario()
    {
        var store = new InMemoryNotificationStore();
        store.TryAdd(Aviso(Guid.NewGuid(), "ReservaCreada", "ana"));
        store.TryAdd(Aviso(Guid.NewGuid(), "ReservaCreada", "beto"));

        var avisosDeAna = store.GetByUser("ana");

        Assert.Single(avisosDeAna);
        Assert.Equal("ana", avisosDeAna[0].UserId);
    }
}
