using System;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when an operation results in a conflict (e.g., duplicate resource)
/// Maps to HTTP 409 (Conflict) or gRPC AlreadyExists
/// </summary>
public class ConflictException : ApplicationException
{
    /// <summary>
    /// The type of conflict that occurred
    /// </summary>
    public string? ConflictType { get; }
    
    public ConflictException(string message) 
        : base(message, "CONFLICT")
    {
    }
    
    public ConflictException(string message, string conflictType) 
        : base(message, "CONFLICT")
    {
        ConflictType = conflictType;
    }
    
    public ConflictException(string message, string errorCode, string conflictType) 
        : base(message, errorCode)
    {
        ConflictType = conflictType;
    }
    
    public ConflictException(string message, Exception innerException) 
        : base(message, "CONFLICT", innerException)
    {
    }
}