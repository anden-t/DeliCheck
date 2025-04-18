using DeliCheck.Schemas.Responses;
using System.Net;
using System.Runtime.Serialization;

namespace DeliCheck.Web.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; set; }

        public ApiResponse ApiResponse { get; set; }

        public ApiException() { }

        public ApiException(string? message) : base(message) { }

        public ApiException(string? message, Exception? innerException) : base(message, innerException) { }

        protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
