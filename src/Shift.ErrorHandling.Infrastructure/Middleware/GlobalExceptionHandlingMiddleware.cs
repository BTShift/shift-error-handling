using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shift.ErrorHandling.Infrastructure.Middleware;

/// <summary>
/// Middleware that handles exceptions globally and returns standardized error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = GetCorrelationId(context);
        
        _logger.LogError(exception, 
            "Unhandled exception occurred. Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
            context.Request.Path,
            context.Request.Method,
            correlationId);

        var (statusCode, errorCode) = GetResponseDetails(exception);
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        var response = BuildErrorResponse(exception, errorCode, correlationId);
        
        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private (int statusCode, string errorCode) GetResponseDetails(Exception exception)
    {
        return exception switch
        {
            Exceptions.BusinessException => (StatusCodes.Status400BadRequest, GetErrorCode(exception)),
            Exceptions.ValidationException => (StatusCodes.Status400BadRequest, GetErrorCode(exception)),
            Exceptions.NotFoundException => (StatusCodes.Status404NotFound, GetErrorCode(exception)),
            Exceptions.UnauthorizedException => (StatusCodes.Status401Unauthorized, GetErrorCode(exception)),
            Exceptions.ForbiddenException => (StatusCodes.Status403Forbidden, GetErrorCode(exception)),
            Exceptions.ConflictException => (StatusCodes.Status409Conflict, GetErrorCode(exception)),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "OPERATION_CANCELLED"),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "NOT_IMPLEMENTED"),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "TIMEOUT"),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR")
        };
    }

    private object BuildErrorResponse(Exception exception, string errorCode, string correlationId)
    {
        var errorDetails = new Dictionary<string, object>();
        
        // Add exception-specific details
        if (exception is Exceptions.ApplicationException appEx)
        {
            foreach (var detail in appEx.Details)
            {
                errorDetails[detail.Key] = detail.Value;
            }
        }
        
        // Add validation errors if present
        if (exception is Exceptions.ValidationException validationEx && validationEx.ValidationErrors.Count > 0)
        {
            errorDetails["validationErrors"] = validationEx.ValidationErrors;
        }
        
        // Add resource information for NotFound exceptions
        if (exception is Exceptions.NotFoundException notFoundEx)
        {
            if (!string.IsNullOrEmpty(notFoundEx.ResourceType))
                errorDetails["resourceType"] = notFoundEx.ResourceType;
            if (!string.IsNullOrEmpty(notFoundEx.ResourceId))
                errorDetails["resourceId"] = notFoundEx.ResourceId;
        }
        
        // Add conflict information
        if (exception is Exceptions.ConflictException conflictEx && !string.IsNullOrEmpty(conflictEx.ConflictType))
        {
            errorDetails["conflictType"] = conflictEx.ConflictType;
        }
        
        // Add permission information for Forbidden exceptions
        if (exception is Exceptions.ForbiddenException forbiddenEx)
        {
            if (!string.IsNullOrEmpty(forbiddenEx.Resource))
                errorDetails["resource"] = forbiddenEx.Resource;
            if (!string.IsNullOrEmpty(forbiddenEx.RequiredPermission))
                errorDetails["requiredPermission"] = forbiddenEx.RequiredPermission;
            if (forbiddenEx.UserPermissions != null && forbiddenEx.UserPermissions.Count > 0)
                errorDetails["userPermissions"] = forbiddenEx.UserPermissions;
        }
        
        var response = new
        {
            error = new
            {
                code = errorCode,
                message = GetSafeErrorMessage(exception),
                details = errorDetails.Count > 0 ? errorDetails : null,
                traceId = correlationId,
                timestamp = DateTime.UtcNow
            }
        };
        
        return response;
    }
    
    private string GetSafeErrorMessage(Exception exception)
    {
        // For production, we might want to hide internal error details
        if (exception is Exceptions.ApplicationException)
        {
            return exception.Message;
        }
        
        // For other exceptions, return a generic message in production
        // You can make this configurable based on environment
        return IsProductionEnvironment() 
            ? "An error occurred while processing your request" 
            : exception.Message;
    }
    
    private bool IsProductionEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }
    
    private string GetErrorCode(Exception exception)
    {
        if (exception is Exceptions.ApplicationException appEx)
        {
            return appEx.ErrorCode;
        }
        
        return exception.GetType().Name.Replace("Exception", "").ToUpperInvariant();
    }
    
    private string GetCorrelationId(HttpContext context)
    {
        // Try to get from Activity
        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrEmpty(activityId))
        {
            return activityId;
        }
        
        // Try to get from request headers
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) ||
            context.Request.Headers.TryGetValue("X-Request-Id", out correlationId) ||
            context.Request.Headers.TryGetValue("Correlation-Id", out correlationId))
        {
            return correlationId.ToString();
        }
        
        // Use TraceIdentifier as fallback
        return context.TraceIdentifier;
    }
}