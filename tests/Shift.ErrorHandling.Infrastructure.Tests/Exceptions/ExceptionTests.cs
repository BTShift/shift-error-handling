using System;
using FluentAssertions;
using Shift.ErrorHandling.Infrastructure.Exceptions;
using ApplicationException = Shift.ErrorHandling.Infrastructure.Exceptions.ApplicationException;

namespace Shift.ErrorHandling.Infrastructure.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void BusinessException_Should_Have_Correct_ErrorCode()
    {
        // Arrange & Act
        var exception = new BusinessException("Business rule violated");
        
        // Assert
        exception.ErrorCode.Should().Be("BUSINESS_ERROR");
        exception.Message.Should().Be("Business rule violated");
    }
    
    [Fact]
    public void BusinessException_Should_Allow_Custom_ErrorCode()
    {
        // Arrange & Act
        var exception = new BusinessException("Business rule violated", "CUSTOM_ERROR");
        
        // Assert
        exception.ErrorCode.Should().Be("CUSTOM_ERROR");
    }
    
    [Fact]
    public void ValidationException_Should_Store_ValidationErrors()
    {
        // Arrange
        var errors = new Dictionary<string, List<string>>
        {
            { "Email", new List<string> { "Email is required", "Email format is invalid" } },
            { "Name", new List<string> { "Name is too short" } }
        };
        
        // Act
        var exception = new ValidationException("Validation failed", errors);
        
        // Assert
        exception.ValidationErrors.Should().HaveCount(2);
        exception.ValidationErrors["Email"].Should().HaveCount(2);
        exception.ValidationErrors["Name"].Should().HaveCount(1);
    }
    
    [Fact]
    public void ValidationException_Should_Allow_Adding_FieldErrors()
    {
        // Arrange
        var exception = new ValidationException("Validation failed");
        
        // Act
        exception
            .AddFieldError("Email", "Email is required")
            .AddFieldError("Email", "Email format is invalid")
            .AddFieldError("Name", "Name is required");
        
        // Assert
        exception.ValidationErrors.Should().HaveCount(2);
        exception.ValidationErrors["Email"].Should().HaveCount(2);
        exception.ValidationErrors["Name"].Should().HaveCount(1);
    }
    
    [Fact]
    public void NotFoundException_Should_Store_ResourceInfo()
    {
        // Arrange & Act
        var exception = new NotFoundException("User", "123");
        
        // Assert
        exception.ResourceType.Should().Be("User");
        exception.ResourceId.Should().Be("123");
        exception.Message.Should().Be("User with id '123' was not found");
        exception.ErrorCode.Should().Be("NOT_FOUND");
    }
    
    [Fact]
    public void ConflictException_Should_Store_ConflictType()
    {
        // Arrange & Act
        var exception = new ConflictException("Resource already exists", "DUPLICATE");
        
        // Assert
        exception.ConflictType.Should().Be("DUPLICATE");
        exception.Message.Should().Be("Resource already exists");
        exception.ErrorCode.Should().Be("CONFLICT");
    }
    
    [Fact]
    public void UnauthorizedException_Should_Have_Default_Message()
    {
        // Arrange & Act
        var exception = new UnauthorizedException();
        
        // Assert
        exception.Message.Should().Be("Authentication required");
        exception.ErrorCode.Should().Be("UNAUTHORIZED");
    }
    
    [Fact]
    public void ForbiddenException_Should_Store_PermissionInfo()
    {
        // Arrange
        var userPermissions = new List<string> { "read", "write" };
        
        // Act
        var exception = new ForbiddenException("users", "delete", userPermissions);
        
        // Assert
        exception.Resource.Should().Be("users");
        exception.RequiredPermission.Should().Be("delete");
        exception.UserPermissions.Should().BeEquivalentTo(userPermissions);
        exception.Message.Should().Contain("Access denied to users");
        exception.Message.Should().Contain("Required permission: delete");
    }
    
    [Fact]
    public void ApplicationException_Should_Support_Details_And_CorrelationId()
    {
        // Arrange
        var exception = new BusinessException("Test error");
        var correlationId = Guid.NewGuid().ToString();
        
        // Act
        exception
            .WithDetail("userId", "123")
            .WithDetail("action", "create")
            .WithCorrelationId(correlationId);
        
        // Assert
        exception.Details.Should().HaveCount(2);
        exception.Details["userId"].Should().Be("123");
        exception.Details["action"].Should().Be("create");
        exception.CorrelationId.Should().Be(correlationId);
    }
    
    [Fact]
    public void All_Exceptions_Should_Support_InnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        
        // Act
        var businessEx = new BusinessException("Business error", innerException);
        var notFoundEx = new NotFoundException("Not found error", innerException);
        var unauthorizedEx = new UnauthorizedException("Auth error", innerException);
        var forbiddenEx = new ForbiddenException("Forbidden error", innerException);
        var conflictEx = new ConflictException("Conflict error", innerException);
        
        // Assert
        businessEx.InnerException.Should().Be(innerException);
        notFoundEx.InnerException.Should().Be(innerException);
        unauthorizedEx.InnerException.Should().Be(innerException);
        forbiddenEx.InnerException.Should().Be(innerException);
        conflictEx.InnerException.Should().Be(innerException);
    }
}