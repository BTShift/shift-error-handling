namespace Shift.ErrorHandling.Infrastructure.Options;

/// <summary>
/// Options for configuring error handling behavior
/// </summary>
public class ErrorHandlingOptions
{
    /// <summary>
    /// Whether to enable the gRPC exception interceptor
    /// </summary>
    public bool EnableGrpcInterceptor { get; set; } = true;
    
    /// <summary>
    /// Whether to include detailed error information in responses
    /// Should be false in production environments
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
    
    /// <summary>
    /// Whether to include stack traces in error responses
    /// Should be false in production environments
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;
    
    /// <summary>
    /// Whether to log sensitive information
    /// Should be false in production environments
    /// </summary>
    public bool LogSensitiveData { get; set; } = false;
    
    /// <summary>
    /// Custom error message for internal server errors
    /// </summary>
    public string InternalErrorMessage { get; set; } = "An error occurred while processing your request";
    
    /// <summary>
    /// Whether to use correlation IDs from incoming requests
    /// </summary>
    public bool UseCorrelationIds { get; set; } = true;
}