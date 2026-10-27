using Microsoft.AspNetCore.Http;

namespace Payment.Api.Core.Exceptions
{
    public class BadRequestException : AppException
    {
        public IEnumerable<string>? Errors { get; }

        public BadRequestException(string message, IEnumerable<string>? errors = null)
            : base(message, StatusCodes.Status400BadRequest)
        {
            Errors = errors;
        }
    }
}
