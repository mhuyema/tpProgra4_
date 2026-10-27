using Microsoft.AspNetCore.Http;

namespace Payment.Api.Core.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, StatusCodes.Status409Conflict)
        {
        }
    }
}
