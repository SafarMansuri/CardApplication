using CardApplicationAPI.Models.ResponseModel;
using System.Text.Json;

namespace CardApplicationAPI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            // Map common exception types to status codes
            var statusCode = ex switch
            {
                UnauthorizedAccessException => ApplicationActionStatusCodes.Unauthorized,
                KeyNotFoundException => ApplicationActionStatusCodes.NotFound,
                ArgumentException => ApplicationActionStatusCodes.BadRequest,
                _ => ApplicationActionStatusCodes.Error
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new InternalActionResult<object>
            {
                StatusCode = statusCode,
                StopProccess = true,
                StatusText = statusCode == ApplicationActionStatusCodes.Error ? "InternalServerError" : ex.GetType().Name,
                StatusMessage = statusCode == ApplicationActionStatusCodes.Error
                    ? "An unexpected error occurred."
                    : ex.Message,
                ResultObject = null
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await context.Response.WriteAsync(json);
        }
    }
}
