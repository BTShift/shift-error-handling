using System;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when business logic rules are violated
/// Maps to HTTP 400 (Bad Request) or gRPC InvalidArgument
/// </summary>
public class BusinessException : ApplicationException
{
    public BusinessException(string message) 
        : base(message, "BUSINESS_ERROR")
    {
    }
    
    public BusinessException(string message, string errorCode) 
        : base(message, errorCode)
    {
    }
    
    public BusinessException(string message, Exception innerException) 
        : base(message, "BUSINESS_ERROR", innerException)
    {
    }
    
    public BusinessException(string message, string errorCode, Exception innerException) 
        : base(message, errorCode, innerException)
    {
    }
}