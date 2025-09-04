using System;
using System.Collections.Generic;

namespace Shift.ErrorHandling.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when input validation fails
/// Maps to HTTP 400 (Bad Request) or gRPC InvalidArgument
/// </summary>
public class ValidationException : ApplicationException
{
    /// <summary>
    /// Validation errors grouped by field name
    /// </summary>
    public Dictionary<string, List<string>> ValidationErrors { get; }
    
    public ValidationException(string message) 
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = new Dictionary<string, List<string>>();
    }
    
    public ValidationException(string message, Dictionary<string, List<string>> errors) 
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = errors ?? new Dictionary<string, List<string>>();
    }
    
    public ValidationException(Dictionary<string, List<string>> errors) 
        : base("One or more validation errors occurred", "VALIDATION_ERROR")
    {
        ValidationErrors = errors ?? new Dictionary<string, List<string>>();
    }
    
    /// <summary>
    /// Add a validation error for a specific field
    /// </summary>
    public ValidationException AddFieldError(string field, string error)
    {
        if (!ValidationErrors.ContainsKey(field))
        {
            ValidationErrors[field] = new List<string>();
        }
        ValidationErrors[field].Add(error);
        return this;
    }
}