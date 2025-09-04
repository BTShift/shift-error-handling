using System;
using System.Collections.Generic;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when authorization/permission check fails
/// Maps to HTTP 403 (Forbidden) or gRPC PermissionDenied
/// </summary>
public class ForbiddenException : ApplicationException
{
    /// <summary>
    /// The resource that access was denied to
    /// </summary>
    public string? Resource { get; }
    
    /// <summary>
    /// The permission that was required
    /// </summary>
    public string? RequiredPermission { get; }
    
    /// <summary>
    /// The user's actual permissions
    /// </summary>
    public List<string>? UserPermissions { get; }
    
    public ForbiddenException() 
        : base("Access denied", "FORBIDDEN")
    {
    }
    
    public ForbiddenException(string message) 
        : base(message, "FORBIDDEN")
    {
    }
    
    public ForbiddenException(string message, string resource, string requiredPermission) 
        : base(message, "FORBIDDEN")
    {
        Resource = resource;
        RequiredPermission = requiredPermission;
    }
    
    public ForbiddenException(string resource, string requiredPermission, List<string> userPermissions) 
        : base($"Access denied to {resource}. Required permission: {requiredPermission}", "FORBIDDEN")
    {
        Resource = resource;
        RequiredPermission = requiredPermission;
        UserPermissions = userPermissions;
    }
    
    public ForbiddenException(string message, Exception innerException) 
        : base(message, "FORBIDDEN", innerException)
    {
    }
}