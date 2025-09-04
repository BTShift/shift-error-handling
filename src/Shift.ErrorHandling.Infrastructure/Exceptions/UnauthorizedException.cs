using System;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when authentication fails
/// Maps to HTTP 401 (Unauthorized) or gRPC Unauthenticated
/// </summary>
public class UnauthorizedException : ApplicationException
{
    /// <summary>
    /// The authentication scheme that failed
    /// </summary>
    public string? AuthenticationScheme { get; }
    
    public UnauthorizedException() 
        : base("Authentication required", "UNAUTHORIZED")
    {
    }
    
    public UnauthorizedException(string message) 
        : base(message, "UNAUTHORIZED")
    {
    }
    
    public UnauthorizedException(string message, string authenticationScheme) 
        : base(message, "UNAUTHORIZED")
    {
        AuthenticationScheme = authenticationScheme;
    }
    
    public UnauthorizedException(string message, Exception innerException) 
        : base(message, "UNAUTHORIZED", innerException)
    {
    }
}