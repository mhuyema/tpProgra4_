using Microsoft.AspNetCore.Http;

namespace Booking.Api.Core.Exceptions
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "No tenés permisos para realizar esta acción.")
            : base(message, StatusCodes.Status403Forbidden)
        {
        }
    }
}
