using PropertyDeals.Domain.Entities.Base;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Documents;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Documents;

namespace PropertyDeals.Domain.Entities;

public class Document : AggregateRoot
{
    public const int FileNameMaxLength = 255;
    public const int S3KeyMaxLength = 100;

    private static readonly HashSet<string> AllowedExtensions =
    [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];

    public int DealId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string S3Key { get; private set; } = null!;
    public int UploadedBy { get; private set; }
    public DocumentStatusEnum Status { get; private set; }

    private Document() { }

    public static Document Create(int dealId, string fileName, int uploadedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (dealId <= 0 || uploadedBy <= 0)
        {
            throw new DomainValidationException("DealId and uploadedBy must be positive.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainValidationException("File name cannot be empty or whitespace.");
        }

        fileName = Path.GetFileName(fileName.Trim().Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainValidationException("File name must contain a name, not only a folder path.");
        }

        if (fileName.Length > FileNameMaxLength)
        {
            throw new DomainValidationException($"File name cannot be longer than {FileNameMaxLength} characters.");
        }

        if (fileName.Any(char.IsControl))
        {
            throw new DomainValidationException("File name contains invalid characters.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new DomainValidationException($"Files of type '{extension}' are not allowed.");
        }

        var s3Key = $"deals/{dealId}/{Guid.NewGuid()}{extension}";

        var document = new Document { DealId = dealId, FileName = fileName, S3Key = s3Key, Status = DocumentStatusEnum.PendingUpload, UploadedBy = uploadedBy };
        document.RaiseDomainEvent(new DocumentCreatedEvent(document, utcNow));

        return document;
    }

    public void MarkUploaded(DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (Status != DocumentStatusEnum.PendingUpload)
        {
            throw new InvalidDocumentStatusTransitionException(Status, DocumentStatusEnum.Uploaded);
        }

        Status = DocumentStatusEnum.Uploaded;
        RaiseDomainEvent(new DocumentUploadedEvent(Id, DealId, UploadedBy, utcNow));
    }

    public void Delete(int deletedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (deletedBy <= 0)
        {
            throw new DomainValidationException("DeletedBy must be positive.");
        }

        if (Status == DocumentStatusEnum.Deleted)
        {
            throw new InvalidDocumentStatusTransitionException(Status, DocumentStatusEnum.Deleted);
        }

        Status = DocumentStatusEnum.Deleted;
        RaiseDomainEvent(new DocumentDeletedEvent(Id, DealId, deletedBy, utcNow));
    }
}
