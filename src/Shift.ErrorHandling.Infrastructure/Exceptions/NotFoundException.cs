using System;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// Maps to HTTP 404 (Not Found) or gRPC NotFound
/// </summary>
public class NotFoundException : ApplicationException
{
    /// <summary>
    /// The type of resource that was not found
    /// </summary>
    public string? ResourceType { get; }
    
    /// <summary>
    /// The identifier of the resource that was not found
    /// </summary>
    public string? ResourceId { get; }
    
    public NotFoundException(string message) 
        : base(message, "NOT_FOUND")
    {
    }
    
    public NotFoundException(string resourceType, string resourceId) 
        : base($"{resourceType} with id '{resourceId}' was not found", "NOT_FOUND")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    public NotFoundException(string message, string resourceType, string resourceId) 
        : base(message, "NOT_FOUND")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    public NotFoundException(string message, Exception innerException) 
        : base(message, "NOT_FOUND", innerException)
    {
    }
}