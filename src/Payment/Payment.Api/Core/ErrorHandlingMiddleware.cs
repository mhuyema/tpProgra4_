using Payment.Api.Core.Exceptions;
using Payment.Api.DTOS;

namespace Payment.Api.Core
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                _logger.LogWarning(ex, "Error de aplicación controlado: {Message}", ex.Message);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex.StatusCode;

                var response = new ErrorResponseDTO
                {
                    StatusCode = ex.StatusCode,
                    Message = ex.Message,
                    Errors = (ex as BadRequestException)?.Errors
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error no controlado.");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var response = new ErrorResponseDTO
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Ocurrió un error interno en el servidor. Por favor intenta más tarde.",
                    // Solo se expone el detalle real en Development para no filtrar información interna en producción.
                    Detail = _environment.IsDevelopment() ? ex.Message : null
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
