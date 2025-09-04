using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Shift.ErrorHandling.Infrastructure.Exceptions;
using Shift.ErrorHandling.Infrastructure.Interceptors;
using ApplicationException = Shift.ErrorHandling.Infrastructure.Exceptions.ApplicationException;

namespace Shift.ErrorHandling.Infrastructure.Tests.Interceptors;

public class GrpcExceptionInterceptorTests
{
    private readonly Mock<ILogger<GrpcExceptionInterceptor>> _loggerMock;
    private readonly GrpcExceptionInterceptor _interceptor;

    public GrpcExceptionInterceptorTests()
    {
        _loggerMock = new Mock<ILogger<GrpcExceptionInterceptor>>();
        _interceptor = new GrpcExceptionInterceptor(_loggerMock.Object);
    }

    [Fact]
    public async Task Should_Map_BusinessException_To_InvalidArgument()
    {
        // Arrange
        var exception = new BusinessException("Business rule violated");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.InvalidArgument);
        rpcException.Status.Detail.Should().Be("Business rule violated");
    }
    
    [Fact]
    public async Task Should_Map_ValidationException_To_InvalidArgument()
    {
        // Arrange
        var exception = new ValidationException("Validation failed");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
    
    [Fact]
    public async Task Should_Map_NotFoundException_To_NotFound()
    {
        // Arrange
        var exception = new NotFoundException("User", "123");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.NotFound);
        rpcException.Status.Detail.Should().Contain("User with id '123' was not found");
    }
    
    [Fact]
    public async Task Should_Map_UnauthorizedException_To_Unauthenticated()
    {
        // Arrange
        var exception = new UnauthorizedException("Invalid token");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task Should_Map_ForbiddenException_To_PermissionDenied()
    {
        // Arrange
        var exception = new ForbiddenException("Access denied");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }
    
    [Fact]
    public async Task Should_Map_ConflictException_To_AlreadyExists()
    {
        // Arrange
        var exception = new ConflictException("Resource already exists");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.AlreadyExists);
    }
    
    [Fact]
    public async Task Should_Map_TimeoutException_To_DeadlineExceeded()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.DeadlineExceeded);
    }
    
    [Fact]
    public async Task Should_Map_UnknownException_To_Internal()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        rpcException!.StatusCode.Should().Be(StatusCode.Internal);
    }
    
    [Fact]
    public async Task Should_Include_ErrorCode_In_Metadata()
    {
        // Arrange
        var exception = new BusinessException("Error", "CUSTOM_CODE");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        var errorCode = GetMetadataValue(rpcException!.Trailers, "error-code");
        errorCode.Should().Be("CUSTOM_CODE");
    }
    
    [Fact]
    public async Task Should_Include_CorrelationId_In_Metadata()
    {
        // Arrange
        var exception = new BusinessException("Error");
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => exception, context);
        
        // Assert
        rpcException.Should().NotBeNull();
        var correlationId = GetMetadataValue(rpcException!.Trailers, "correlation-id");
        correlationId.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task Should_Rethrow_RpcException_Without_Modification()
    {
        // Arrange
        var originalException = new RpcException(new Status(StatusCode.Cancelled, "Cancelled by client"));
        var context = CreateServerCallContext();
        
        // Act
        var rpcException = await CatchRpcException(() => originalException, context);
        
        // Assert
        rpcException.Should().BeSameAs(originalException);
    }
    
    private async Task<RpcException?> CatchRpcException(Func<Exception> exceptionFactory, ServerCallContext context)
    {
        try
        {
            await _interceptor.UnaryServerHandler<string, string>(
                request: "test",
                context,
                continuation: (req, ctx) => throw exceptionFactory());
            
            return null;
        }
        catch (RpcException ex)
        {
            return ex;
        }
    }
    
    private static ServerCallContext CreateServerCallContext()
    {
        return new DefaultServerCallContext();
    }
    
    private static string? GetMetadataValue(Metadata metadata, string key)
    {
        var entry = metadata.Get(key);
        return entry?.Value;
    }
    
    private class DefaultServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "TestHost";
        protected override string PeerCore => "TestPeer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new AuthContext(string.Empty, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        {
            throw new NotImplementedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }
    }
}