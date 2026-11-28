using System.Runtime.CompilerServices;

namespace PropertyDeals.Infrastructure.Outbox;

public class OutboxMessage
{
    public const int MaxAttempts = 5;
    public const int ErrorMaxLength = 2000;
    public const int TypeMaxLength = 200;

    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; } = null;

    private OutboxMessage() { }

    public bool CanRetry => ProcessedOnUtc is null && Attempts < MaxAttempts;

    public static OutboxMessage Create(Guid id, string type, string payload, DateTime occurredOnUtc)
    {
        EnsureUtc(occurredOnUtc);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be empty or whitespace.", nameof(type));
        }

        type = type.Trim();

        if (type.Length > TypeMaxLength)
        {
            throw new ArgumentException($"Type cannot be longer than {TypeMaxLength} characters.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload cannot be empty or whitespace.", nameof(payload));
        }

        return new OutboxMessage { Id = id, Type = type, Payload = payload, OccurredOnUtc = occurredOnUtc };
    }

    public void MarkProcessed(DateTime utcNow)
    {
        EnsureUtc(utcNow);

        ProcessedOnUtc = utcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        error = string.IsNullOrWhiteSpace(error) ? "Unknown error." : error.Trim();
        Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
        Attempts++;
    }

    private static void EnsureUtc(DateTime value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be a UTC time, but its Kind is {value.Kind}.", paramName);
        }
    }
}
