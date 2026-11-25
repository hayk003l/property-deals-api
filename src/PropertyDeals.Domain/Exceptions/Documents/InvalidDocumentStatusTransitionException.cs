using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Exceptions.Documents;

public sealed class InvalidDocumentStatusTransitionException : DomainException
{
    public DocumentStatusEnum From { get; }
    public DocumentStatusEnum To { get; }

    public InvalidDocumentStatusTransitionException(DocumentStatusEnum from, DocumentStatusEnum to)
        : base("document.invalid_status_transition", $"A document cannot move from {from} to {to}.")
    {
        From = from;
        To = to;
    }
}