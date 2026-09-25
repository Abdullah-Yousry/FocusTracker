using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using ValidationException = FluentValidation.ValidationException;

namespace TaskManager.Api.Middlewares
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation failed for request {Path}. Errors: {@Errors}",
                    context.Request.Path, ex.Errors.Select(e => e.ErrorMessage));

                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                _logger.LogError(ex, "An unhandled exception occurred while processing request {Path}", context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ValidationException validationEx =>
                    ((int)HttpStatusCode.BadRequest, string.Join(" | ", validationEx.Errors.Select(e => e.ErrorMessage))),

                KeyNotFoundException => ((int)HttpStatusCode.NotFound, exception.Message),
                ArgumentException => ((int)HttpStatusCode.BadRequest, exception.Message),
                InvalidOperationException => ((int)HttpStatusCode.BadRequest, exception.Message),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                Status = statusCode,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}
