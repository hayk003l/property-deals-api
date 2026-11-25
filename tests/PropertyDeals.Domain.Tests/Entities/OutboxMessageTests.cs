using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Exceptions;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class OutboxMessageTests
{
    private const string Type = "DealStatusChanged";
    private const string Payload = "{\"dealId\":1}";

    private static OutboxMessage NewMessage() => OutboxMessage.Create(Guid.NewGuid(), Type, Payload, Now);

    [Fact]
    public void Create_WithValidInput_StartsPendingWithNoAttempts()
    {
        var id = Guid.NewGuid();

        var message = OutboxMessage.Create(id, Type, Payload, Now);

        Assert.Equal(id, message.Id);
        Assert.Equal(Type, message.Type);
        Assert.Equal(Payload, message.Payload);
        Assert.Equal(Now, message.OccurredOnUtc);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.Error);
        Assert.True(message.CanRetry);
    }

    [Fact]
    public void Create_TrimsType()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), "  DealStatusChanged  ", Payload, Now);

        Assert.Equal(Type, message.Type);
    }

    [Fact]
    public void Create_WithEmptyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => OutboxMessage.Create(Guid.Empty, Type, Payload, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingType_ThrowsDomainValidationException(string? type)
    {
        Assert.Throws<DomainValidationException>(() => OutboxMessage.Create(Guid.NewGuid(), type!, Payload, Now));
    }

    [Fact]
    public void Create_WithTypeAtMaxLength_Succeeds()
    {
        var type = new string('a', OutboxMessage.TypeMaxLength);

        var message = OutboxMessage.Create(Guid.NewGuid(), type, Payload, Now);

        Assert.Equal(type, message.Type);
    }

    [Fact]
    public void Create_WithTypeAboveMaxLength_ThrowsDomainValidationException()
    {
        var type = new string('a', OutboxMessage.TypeMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => OutboxMessage.Create(Guid.NewGuid(), type, Payload, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingPayload_ThrowsDomainValidationException(string? payload)
    {
        Assert.Throws<DomainValidationException>(() => OutboxMessage.Create(Guid.NewGuid(), Type, payload!, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentExceptionNamingTheParameter(DateTimeKind kind)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => OutboxMessage.Create(Guid.NewGuid(), Type, Payload, NowAs(kind)));

        Assert.Equal("occurredOnUtc", exception.ParamName);
    }

    [Fact]
    public void MarkProcessed_SetsProcessedTimeAndStopsRetries()
    {
        var message = NewMessage();
        var processedAt = Now.AddSeconds(5);

        message.MarkProcessed(processedAt);

        Assert.Equal(processedAt, message.ProcessedOnUtc);
        Assert.False(message.CanRetry);
    }

    [Fact]
    public void MarkProcessed_AfterFailure_ClearsError()
    {
        var message = NewMessage();
        message.MarkFailed("RabbitMQ unavailable");

        message.MarkProcessed(Now);

        Assert.Null(message.Error);
        Assert.Equal(1, message.Attempts);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void MarkProcessed_WithNonUtcTime_ThrowsAndStaysPending(DateTimeKind kind)
    {
        var message = NewMessage();

        Assert.Throws<ArgumentException>(() => message.MarkProcessed(NowAs(kind)));

        Assert.Null(message.ProcessedOnUtc);
    }

    [Fact]
    public void MarkFailed_StoresTrimmedErrorAndCountsAttempt()
    {
        var message = NewMessage();

        message.MarkFailed("  RabbitMQ unavailable  ");

        Assert.Equal("RabbitMQ unavailable", message.Error);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.ProcessedOnUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WithMissingError_StoresUnknownErrorAndCountsAttempt(string? error)
    {
        var message = NewMessage();

        message.MarkFailed(error!);

        Assert.Equal("Unknown error.", message.Error);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public void MarkFailed_WithErrorAboveMaxLength_TruncatesAndCountsAttempt()
    {
        var message = NewMessage();

        message.MarkFailed(new string('x', OutboxMessage.ErrorMaxLength + 500));

        Assert.Equal(OutboxMessage.ErrorMaxLength, message.Error!.Length);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public void CanRetry_BeforeMaxAttempts_IsTrue()
    {
        var message = NewMessage();

        for (var i = 0; i < OutboxMessage.MaxAttempts - 1; i++)
        {
            message.MarkFailed("boom");
        }

        Assert.True(message.CanRetry);
    }

    [Fact]
    public void CanRetry_AtMaxAttempts_IsFalse()
    {
        var message = NewMessage();

        for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
        {
            message.MarkFailed("boom");
        }

        Assert.Equal(OutboxMessage.MaxAttempts, message.Attempts);
        Assert.False(message.CanRetry);
    }
}
