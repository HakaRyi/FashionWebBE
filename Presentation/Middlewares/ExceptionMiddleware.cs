using System.Text.Json;

namespace Presentation.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
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

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            int statusCode;
            string message;

            switch (ex)
            {
                case ArgumentException:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = ex.Message;
                    break;

                case InvalidOperationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = ex.Message;
                    break;

                case KeyNotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    message = ex.Message;
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status403Forbidden;
                    message = string.IsNullOrWhiteSpace(ex.Message)
                        ? "You are not allowed to perform this action."
                        : ex.Message;
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    message = "An unexpected error occurred. Please try again later.";
                    break;
            }

            _logger.LogError(ex, "An exception occurred while processing the request.");

            var response = new
            {
                success = false,
                statusCode,
                message,
                detail = _environment.IsDevelopment()
                    ? ex.ToString()
                    : null
            };

            var json = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(json);
        }
    }
}