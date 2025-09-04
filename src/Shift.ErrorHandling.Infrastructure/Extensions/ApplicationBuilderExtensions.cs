using Microsoft.AspNetCore.Builder;
using Shift.ErrorHandling.Infrastructure.Middleware;

namespace Shift.ErrorHandling.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring the application pipeline
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseShiftErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
    
    /// <summary>
    /// Adds the global exception handling middleware to the pipeline (WebApplication version)
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseShiftErrorHandling(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        return app;
    }
}