using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TaskManager.API.Middleware
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
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(ex, "Response has already started, unable to modify the response.");
                    return;
                }

                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            var code = StatusCodes.Status500InternalServerError;

            switch (exception)
            {
                case UnauthorizedAccessException:
                case TaskManager.Core.Exceptions.AuthorizationException:
                    code = StatusCodes.Status401Unauthorized;
                    break;
                case ArgumentException:
                case InvalidOperationException:
                    code = StatusCodes.Status400BadRequest;
                    break;
                case System.Collections.Generic.KeyNotFoundException:
                    code = StatusCodes.Status404NotFound;
                    break;
            }

            if (code >= 500)
            {
                logger.LogError(exception, "An internal server error occurred.");
            }
            else
            {
                logger.LogWarning(exception, "Handled application exception: {Message}", exception.Message);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;

            // Only include exception details for client errors (4xx); use generic message for server errors (5xx)
            var detail = code >= 500
                ? "An internal error occurred."
                : exception.Message;

            var problemDetails = new ProblemDetails
            {
                Status = code,
                Title = "An error occurred while processing your request.",
                Detail = detail,
                Instance = context.Request.Path
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(problemDetails, options);

            return context.Response.WriteAsync(result);
        }
    }
}
