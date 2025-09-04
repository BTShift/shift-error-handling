using System;
using System.Collections.Generic;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Base exception class for all custom application exceptions
/// </summary>
public abstract class ApplicationException : Exception
{
    /// <summary>
    /// Unique error code identifying the type of error
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Additional details about the error
    /// </summary>
    public Dictionary<string, object> Details { get; }
    
    /// <summary>
    /// Correlation ID for tracking the error across services
    /// </summary>
    public string? CorrelationId { get; set; }
    
    protected ApplicationException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
        Details = new Dictionary<string, object>();
    }
    
    protected ApplicationException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Add additional detail to the exception
    /// </summary>
    public ApplicationException WithDetail(string key, object value)
    {
        Details[key] = value;
        return this;
    }
    
    /// <summary>
    /// Set correlation ID for tracing
    /// </summary>
    public ApplicationException WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }
}