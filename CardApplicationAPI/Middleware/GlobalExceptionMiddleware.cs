using CardApplicationAPI.Models.ResponseModel;
using System.Text.Json;

namespace CardApplicationAPI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
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
                StatusText = ex.GetType().Name,
                StatusMessage = ex.Message,
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
