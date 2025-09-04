# Shift Error Handling Infrastructure

[![NuGet Package](https://github.com/BTShift/shift-error-handling/actions/workflows/publish-package.yml/badge.svg)](https://github.com/BTShift/shift-error-handling/actions/workflows/publish-package.yml)
[![GitHub Package](https://img.shields.io/badge/package-GitHub%20Packages-blue)](https://github.com/BTShift/shift-error-handling/packages)

Comprehensive error handling infrastructure for BTShift microservices, providing standardized exception types, gRPC interceptors, and HTTP middleware for consistent error responses across all services.

## Features

✅ **Typed Exception Classes** - Business, Validation, NotFound, Unauthorized, Forbidden, and Conflict exceptions  
✅ **gRPC Exception Interceptor** - Automatic mapping to appropriate gRPC status codes  
✅ **HTTP Exception Middleware** - Standardized JSON error responses  
✅ **Correlation ID Tracking** - Automatic correlation across service boundaries  
✅ **Detailed Error Information** - Structured error details with metadata support  
✅ **Easy Integration** - Simple extension methods for ASP.NET Core  

## Installation

### Package Registry Setup

First, configure your NuGet to access GitHub Packages:

```bash
dotnet nuget add source https://nuget.pkg.github.com/BTShift/index.json \
  -n github \
  -u YOUR_GITHUB_USERNAME \
  -p YOUR_GITHUB_TOKEN \
  --store-password-in-clear-text
```

Or add to your `NuGet.Config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/BTShift/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Package Installation

```xml
<PackageReference Include="Shift.ErrorHandling.Infrastructure" Version="1.0.0" />
```

## Quick Start

### Basic Setup

```csharp
// Program.cs
using Shift.ErrorHandling.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add error handling services
builder.Services.AddShiftErrorHandling(options =>
{
    options.EnableGrpcInterceptor = true;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

var app = builder.Build();

// Use error handling middleware
app.UseShiftErrorHandling();

app.Run();
```

### gRPC Service Setup

```csharp
// For gRPC services
builder.Services.AddShiftGrpcErrorHandling(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddGrpc(); // Your gRPC configuration
```

### HTTP/REST Service Setup

```csharp
// For HTTP/REST services only
builder.Services.AddShiftHttpErrorHandling(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

app.UseShiftErrorHandling(); // Add middleware
```

## Exception Types

### BusinessException
For business logic violations - Maps to HTTP 400 or gRPC InvalidArgument

```csharp
throw new BusinessException("Insufficient balance")
    .WithDetail("requiredAmount", 100)
    .WithDetail("availableAmount", 50)
    .WithCorrelationId(correlationId);
```

### ValidationException
For input validation errors - Maps to HTTP 400 or gRPC InvalidArgument

```csharp
var exception = new ValidationException("Validation failed")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Email", "Email format is invalid")
    .AddFieldError("Age", "Must be 18 or older");

throw exception;
```

### NotFoundException
For resource not found - Maps to HTTP 404 or gRPC NotFound

```csharp
throw new NotFoundException("User", "123");
// Message: "User with id '123' was not found"
```

### UnauthorizedException
For authentication failures - Maps to HTTP 401 or gRPC Unauthenticated

```csharp
throw new UnauthorizedException("Invalid or expired token");
```

### ForbiddenException
For authorization failures - Maps to HTTP 403 or gRPC PermissionDenied

```csharp
throw new ForbiddenException("users", "delete", userPermissions);
// Includes resource, required permission, and user's actual permissions
```

### ConflictException
For conflicts like duplicates - Maps to HTTP 409 or gRPC AlreadyExists

```csharp
throw new ConflictException("Email already exists", "DUPLICATE_EMAIL");
```

## Error Response Format

### HTTP/REST Response
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "validationErrors": {
        "Email": ["Email is required", "Email format is invalid"],
        "Age": ["Must be 18 or older"]
      }
    },
    "traceId": "00-abc123-def456-00",
    "timestamp": "2025-01-04T10:00:00Z"
  }
}
```

### gRPC Metadata
Error information is included in gRPC metadata:
- `error-code`: The error code
- `correlation-id`: Request correlation ID
- `detail-*`: Additional error details
- `validation-*`: Field validation errors

## Advanced Usage

### Custom Error Codes

```csharp
throw new BusinessException("Custom error", "CUSTOM_CODE")
    .WithDetail("customField", "customValue");
```

### Adding Correlation IDs

```csharp
public class MyService
{
    public async Task ProcessRequest(string correlationId)
    {
        try
        {
            // Your logic here
        }
        catch (ApplicationException ex)
        {
            ex.WithCorrelationId(correlationId);
            throw;
        }
    }
}
```

### Configuration Options

```csharp
builder.Services.AddShiftErrorHandling(options =>
{
    // Enable gRPC interceptor (default: true)
    options.EnableGrpcInterceptor = true;
    
    // Show detailed errors (set to false in production)
    options.EnableDetailedErrors = false;
    
    // Include stack traces (set to false in production)
    options.IncludeStackTrace = false;
    
    // Log sensitive data (set to false in production)
    options.LogSensitiveData = false;
    
    // Custom message for internal errors
    options.InternalErrorMessage = "An error occurred";
    
    // Use correlation IDs from requests
    options.UseCorrelationIds = true;
});
```

## Integration Examples

### With MassTransit
```csharp
public class OrderConsumer : IConsumer<CreateOrder>
{
    public async Task Consume(ConsumeContext<CreateOrder> context)
    {
        try
        {
            // Process order
        }
        catch (Exception ex) when (ex is not ApplicationException)
        {
            // Wrap in application exception
            throw new BusinessException("Order processing failed", ex)
                .WithCorrelationId(context.CorrelationId?.ToString());
        }
    }
}
```

### With Entity Framework
```csharp
public class UserRepository
{
    public async Task<User> GetByIdAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }
        return user;
    }
}
```

### With FluentValidation
```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}

// In your service
var validationResult = await validator.ValidateAsync(request);
if (!validationResult.IsValid)
{
    var exception = new ValidationException("Validation failed");
    foreach (var error in validationResult.Errors)
    {
        exception.AddFieldError(error.PropertyName, error.ErrorMessage);
    }
    throw exception;
}
```

## Migration Guide

### From Old Pattern
```csharp
// Old
try
{
    // logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    return StatusCode(500, "Internal error");
}

// New
throw new BusinessException("Specific error message")
    .WithDetail("context", "value");
// Middleware handles logging and response
```

### Updating Existing Services
1. Add the package reference
2. Add `AddShiftErrorHandling()` in Program.cs
3. Add `UseShiftErrorHandling()` middleware
4. Replace generic exceptions with typed exceptions
5. Remove manual error response creation

## Best Practices

1. **Use specific exception types** - Choose the most appropriate exception type
2. **Include context** - Add relevant details using `WithDetail()`
3. **Preserve correlation IDs** - Always pass through correlation IDs
4. **Don't expose sensitive data** - Be careful with error details in production
5. **Let middleware handle responses** - Don't catch exceptions just to format responses
6. **Log at the boundary** - The interceptor/middleware handles logging

## Testing

```csharp
[Fact]
public async Task Should_Throw_NotFoundException_When_User_Not_Found()
{
    // Arrange
    var userId = "non-existent";
    
    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(
        () => userService.GetByIdAsync(userId)
    );
    
    exception.ResourceType.Should().Be("User");
    exception.ResourceId.Should().Be(userId);
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Submit a pull request

## License

This project is part of the BTShift platform and follows the same licensing terms.
