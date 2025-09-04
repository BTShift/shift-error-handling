using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shift.ErrorHandling.Infrastructure.Interceptors;
using Shift.ErrorHandling.Infrastructure.Options;

namespace Shift.ErrorHandling.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring error handling services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Shift error handling infrastructure to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShiftErrorHandling(
        this IServiceCollection services,
        Action<ErrorHandlingOptions>? configure = null)
    {
        var options = new ErrorHandlingOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        
        // Add HTTP context accessor if not already registered
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        
        // Register the gRPC interceptor
        services.AddScoped<GrpcExceptionInterceptor>();
        
        // Configure gRPC with the interceptor if gRPC is being used
        if (options.EnableGrpcInterceptor)
        {
            services.AddGrpc(grpcOptions =>
            {
                grpcOptions.Interceptors.Add<GrpcExceptionInterceptor>();
                grpcOptions.EnableDetailedErrors = options.EnableDetailedErrors;
            });
        }
        
        return services;
    }
    
    /// <summary>
    /// Adds Shift error handling for gRPC services only
    /// </summary>
    public static IServiceCollection AddShiftGrpcErrorHandling(
        this IServiceCollection services,
        Action<ErrorHandlingOptions>? configure = null)
    {
        var options = new ErrorHandlingOptions { EnableGrpcInterceptor = true };
        configure?.Invoke(options);
        
        return services.AddShiftErrorHandling(options => 
        {
            options.EnableGrpcInterceptor = true;
            options.EnableDetailedErrors = options.EnableDetailedErrors;
        });
    }
    
    /// <summary>
    /// Adds Shift error handling for HTTP/REST services only
    /// </summary>
    public static IServiceCollection AddShiftHttpErrorHandling(
        this IServiceCollection services,
        Action<ErrorHandlingOptions>? configure = null)
    {
        var options = new ErrorHandlingOptions { EnableGrpcInterceptor = false };
        configure?.Invoke(options);
        
        return services.AddShiftErrorHandling(options => 
        {
            options.EnableGrpcInterceptor = false;
            options.EnableDetailedErrors = options.EnableDetailedErrors;
        });
    }
}