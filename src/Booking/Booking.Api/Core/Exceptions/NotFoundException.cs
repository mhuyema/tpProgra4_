using Microsoft.AspNetCore.Http;

namespace Booking.Api.Core.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message = "El recurso solicitado no existe.")
            : base(message, StatusCodes.Status404NotFound)
        {
        }
    }
}
