using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HotelListing.Api.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId=Activity.Current?.Id ?? httpContext.TraceIdentifier;

            logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method:{Method}",
                traceId,
                httpContext.Request.Path,
                httpContext.Request.Method
            );
            //                      a standard way to communicate when there is a problem
            var problemDetails = new ProblemDetails
            {
                Title = "An  error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError,
                Type= "https://datatracker.ietf.org/doc/html/rfc9110#name-500-internal-server-error",
                Instance = httpContext.Request.Path,
                Detail = httpContext.RequestServices.GetRequiredService<IHostEnvironment>()
                    .IsDevelopment()
                    ?exception.Message
                    :"An unexpected error occurred.Please try again later."
                
            };

            problemDetails.Extensions["traceId"]=traceId;
            // check in if we are in development environment
            if (httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                // we can also include the stack trace in development environment for better debugging
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
