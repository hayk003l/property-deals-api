using PropertyDeals.Domain.Exceptions;

namespace PropertyDeals.Domain.Entities;

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
        Guard.EnsureUtc(occurredOnUtc);

        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new DomainValidationException("Type cannot be empty or whitespace.");
        }

        type = type.Trim();

        if (type.Length > TypeMaxLength)
        {
            throw new DomainValidationException($"Type cannot be longer than {TypeMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainValidationException("Payload cannot be empty or whitespace.");
        }

        return new OutboxMessage { Id = id, Type = type, Payload = payload, OccurredOnUtc = occurredOnUtc };
    }

    public void MarkProcessed(DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        ProcessedOnUtc = utcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        error = string.IsNullOrWhiteSpace(error) ? "Unknown error." : error.Trim();
        Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
        Attempts++;
    }
}