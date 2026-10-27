namespace Booking.Api.Core.Exceptions
{
    /// <summary>
    /// Excepción base para errores de negocio esperables. El <see cref="ErrorHandlingMiddleware"/>
    /// la traduce directamente al <see cref="StatusCode"/> indicado en vez de responder 500.
    /// </summary>
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
