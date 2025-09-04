using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shift.ErrorHandling.Infrastructure.Interceptors;

/// <summary>
/// gRPC interceptor that handles exceptions and maps them to appropriate gRPC status codes
/// </summary>
public class GrpcExceptionInterceptor : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GrpcExceptionInterceptor(
        ILogger<GrpcExceptionInterceptor> logger,
        IHttpContextAccessor httpContextAccessor = null!)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            // Already a gRPC exception, just rethrow
            throw;
        }
        catch (Exception ex)
        {
            var correlationId = GetCorrelationId(context);
            
            _logger.LogError(ex, 
                "Unhandled exception in {Method}. CorrelationId: {CorrelationId}", 
                context.Method, 
                correlationId);
            
            var (status, errorCode) = MapExceptionToStatus(ex);
            
            var metadata = new Metadata
            {
                { "error-code", errorCode },
                { "correlation-id", correlationId }
            };
            
            // Add additional details if available
            if (ex is Exceptions.ApplicationException appEx)
            {
                foreach (var detail in appEx.Details)
                {
                    metadata.Add($"detail-{detail.Key}", detail.Value?.ToString() ?? string.Empty);
                }
            }
            
            // Add validation errors if it's a ValidationException
            if (ex is Exceptions.ValidationException validationEx && validationEx.ValidationErrors.Count > 0)
            {
                foreach (var error in validationEx.ValidationErrors)
                {
                    metadata.Add($"validation-{error.Key}", string.Join(", ", error.Value));
                }
            }
            
            throw new RpcException(new Status(status, ex.Message), metadata);
        }
    }
    
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            throw;
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            throw;
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            throw;
        }
    }
    
    private void HandleException(Exception ex, ServerCallContext context)
    {
        var correlationId = GetCorrelationId(context);
        
        _logger.LogError(ex, 
            "Unhandled exception in {Method}. CorrelationId: {CorrelationId}", 
            context.Method, 
            correlationId);
        
        var (status, errorCode) = MapExceptionToStatus(ex);
        
        var metadata = new Metadata
        {
            { "error-code", errorCode },
            { "correlation-id", correlationId }
        };
        
        throw new RpcException(new Status(status, ex.Message), metadata);
    }
    
    private (StatusCode status, string errorCode) MapExceptionToStatus(Exception exception)
    {
        return exception switch
        {
            Exceptions.BusinessException => (StatusCode.InvalidArgument, GetErrorCode(exception)),
            Exceptions.ValidationException => (StatusCode.InvalidArgument, GetErrorCode(exception)),
            Exceptions.NotFoundException => (StatusCode.NotFound, GetErrorCode(exception)),
            Exceptions.UnauthorizedException => (StatusCode.Unauthenticated, GetErrorCode(exception)),
            Exceptions.ForbiddenException => (StatusCode.PermissionDenied, GetErrorCode(exception)),
            Exceptions.ConflictException => (StatusCode.AlreadyExists, GetErrorCode(exception)),
            OperationCanceledException => (StatusCode.Cancelled, "OPERATION_CANCELLED"),
            NotImplementedException => (StatusCode.Unimplemented, "NOT_IMPLEMENTED"),
            TimeoutException => (StatusCode.DeadlineExceeded, "TIMEOUT"),
            _ => (StatusCode.Internal, "INTERNAL_ERROR")
        };
    }
    
    private string GetErrorCode(Exception exception)
    {
        if (exception is Exceptions.ApplicationException appEx)
        {
            return appEx.ErrorCode;
        }
        
        return exception.GetType().Name.Replace("Exception", "").ToUpperInvariant();
    }
    
    private string GetCorrelationId(ServerCallContext context)
    {
        // Try to get correlation ID from Activity
        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrEmpty(activityId))
        {
            return activityId;
        }
        
        // Try to get from HTTP context if available
        if (_httpContextAccessor?.HttpContext != null)
        {
            return _httpContextAccessor.HttpContext.TraceIdentifier;
        }
        
        // Try to get from gRPC metadata
        var metadata = context.RequestHeaders;
        var correlationId = metadata?.GetValue("correlation-id") 
                          ?? metadata?.GetValue("x-correlation-id")
                          ?? metadata?.GetValue("x-request-id");
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            return correlationId;
        }
        
        // Generate a new one as last resort
        return Guid.NewGuid().ToString();
    }
}