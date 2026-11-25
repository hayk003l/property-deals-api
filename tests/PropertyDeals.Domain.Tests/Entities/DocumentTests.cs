using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Documents;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Documents;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class DocumentTests
{
    [Fact]
    public void Create_WithValidInput_StartsPendingUpload()
    {
        var document = Document.Create(DealId, "contract.pdf", ListingAgentId, Now);

        Assert.Equal(DealId, document.DealId);
        Assert.Equal("contract.pdf", document.FileName);
        Assert.Equal(ListingAgentId, document.UploadedBy);
        Assert.Equal(DocumentStatusEnum.PendingUpload, document.Status);
    }

    [Fact]
    public void Create_WithValidInput_RaisesDocumentCreatedEvent()
    {
        var document = Document.Create(DealId, "contract.pdf", ListingAgentId, Now);

        var raised = Assert.IsType<DocumentCreatedEvent>(Assert.Single(document.DomainEvents));
        Assert.Same(document, raised.Document);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void Create_GeneratesS3KeyUnderDealFolderWithExtension()
    {
        var document = Document.Create(DealId, "contract.pdf", ListingAgentId, Now);

        Assert.Matches(@"^deals/42/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.pdf$", document.S3Key);
    }

    [Fact]
    public void Create_TwoDocumentsWithSameName_GetDifferentS3Keys()
    {
        var first = Document.Create(DealId, "contract.pdf", ListingAgentId, Now);
        var second = Document.Create(DealId, "contract.pdf", ListingAgentId, Now);

        Assert.NotEqual(first.S3Key, second.S3Key);
    }

    [Fact]
    public void Create_WithUpperCaseExtension_KeepsFileNameAndLowersKeyExtension()
    {
        var document = Document.Create(DealId, "REPORT.PDF", ListingAgentId, Now);

        Assert.Equal("REPORT.PDF", document.FileName);
        Assert.EndsWith(".pdf", document.S3Key);
    }

    [Fact]
    public void Create_DoesNotPutFileNameIntoS3Key()
    {
        var document = Document.Create(DealId, "John Smith passport.pdf", ListingAgentId, Now);

        Assert.DoesNotContain("John", document.S3Key);
    }

    [Theory]
    [InlineData("a.pdf")]
    [InlineData("a.doc")]
    [InlineData("a.docx")]
    [InlineData("a.jpg")]
    [InlineData("a.jpeg")]
    [InlineData("a.png")]
    public void Create_WithAllowedExtension_Succeeds(string fileName)
    {
        var document = Document.Create(DealId, fileName, ListingAgentId, Now);

        Assert.Equal(fileName, document.FileName);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("notes.txt")]
    [InlineData("contract.pdf.exe")]
    [InlineData("noextension")]
    public void Create_WithDisallowedExtension_ThrowsDomainValidationException(string fileName)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Document.Create(DealId, fileName, ListingAgentId, Now));

        Assert.Contains("not allowed", exception.Message);
    }

    [Theory]
    [InlineData("folder/contract.pdf")]
    [InlineData(@"folder\contract.pdf")]
    [InlineData("../../contract.pdf")]
    [InlineData(@"..\..\contract.pdf")]
    [InlineData(@"C:\Users\someone\contract.pdf")]
    [InlineData("  contract.pdf  ")]
    public void Create_WithPathOrSpaces_KeepsOnlyTheFileName(string fileName)
    {
        var document = Document.Create(DealId, fileName, ListingAgentId, Now);

        Assert.Equal("contract.pdf", document.FileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingFileName_ThrowsDomainValidationException(string? fileName)
    {
        Assert.Throws<DomainValidationException>(() => Document.Create(DealId, fileName!, ListingAgentId, Now));
    }

    [Theory]
    [InlineData("folder/")]
    [InlineData(@"folder\")]
    public void Create_WithFolderOnly_ThrowsDomainValidationException(string fileName)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Document.Create(DealId, fileName, ListingAgentId, Now));

        Assert.Contains("must contain a name", exception.Message);
    }

    [Fact]
    public void Create_WithFileNameAtMaxLength_Succeeds()
    {
        var fileName = new string('a', Document.FileNameMaxLength - 4) + ".pdf";

        var document = Document.Create(DealId, fileName, ListingAgentId, Now);

        Assert.Equal(Document.FileNameMaxLength, document.FileName.Length);
    }

    [Fact]
    public void Create_WithFileNameAboveMaxLength_ThrowsDomainValidationException()
    {
        var fileName = new string('a', Document.FileNameMaxLength - 3) + ".pdf";

        var exception = Assert.Throws<DomainValidationException>(
            () => Document.Create(DealId, fileName, ListingAgentId, Now));

        Assert.Contains("longer than", exception.Message);
    }

    [Theory]
    [InlineData("con\r\ntract.pdf")]
    [InlineData("con\ttract.pdf")]
    [InlineData("con\0tract.pdf")]
    public void Create_WithControlCharacters_ThrowsDomainValidationException(string fileName)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Document.Create(DealId, fileName, ListingAgentId, Now));

        Assert.Contains("invalid characters", exception.Message);
    }

    [Theory]
    [InlineData(0, ListingAgentId)]
    [InlineData(-1, ListingAgentId)]
    [InlineData(DealId, 0)]
    [InlineData(DealId, -1)]
    public void Create_WithNonPositiveIds_ThrowsDomainValidationException(int dealId, int uploadedBy)
    {
        Assert.Throws<DomainValidationException>(() => Document.Create(dealId, "contract.pdf", uploadedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        Assert.Throws<ArgumentException>(() => Document.Create(DealId, "contract.pdf", ListingAgentId, NowAs(kind)));
    }

    [Fact]
    public void MarkUploaded_WhenPendingUpload_SetsUploadedAndRaisesEvent()
    {
        var document = NewDocument();

        document.MarkUploaded(Now);

        Assert.Equal(DocumentStatusEnum.Uploaded, document.Status);
        var raised = Assert.IsType<DocumentUploadedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(document.Id, raised.DocumentId);
        Assert.Equal(DealId, raised.DealId);
        Assert.Equal(ListingAgentId, raised.UploadedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void MarkUploaded_WhenAlreadyUploaded_ThrowsInvalidDocumentStatusTransitionException()
    {
        var document = NewDocument();
        document.MarkUploaded(Now);

        var exception = Assert.Throws<InvalidDocumentStatusTransitionException>(() => document.MarkUploaded(Now));

        Assert.Equal(DocumentStatusEnum.Uploaded, exception.From);
        Assert.Equal(DocumentStatusEnum.Uploaded, exception.To);
    }

    [Fact]
    public void MarkUploaded_WhenDeleted_ThrowsAndStaysDeleted()
    {
        var document = NewDocument();
        document.Delete(ListingAgentId, Now);

        var exception = Assert.Throws<InvalidDocumentStatusTransitionException>(() => document.MarkUploaded(Now));

        Assert.Equal(DocumentStatusEnum.Deleted, exception.From);
        Assert.Equal(DocumentStatusEnum.Deleted, document.Status);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void MarkUploaded_WithNonUtcTime_ThrowsAndStaysPending(DateTimeKind kind)
    {
        var document = NewDocument();

        Assert.Throws<ArgumentException>(() => document.MarkUploaded(NowAs(kind)));

        Assert.Equal(DocumentStatusEnum.PendingUpload, document.Status);
    }

    [Fact]
    public void Delete_WhenPendingUpload_SetsDeletedAndRaisesEvent()
    {
        var document = NewDocument();

        document.Delete(BuyerId, Now);

        Assert.Equal(DocumentStatusEnum.Deleted, document.Status);
        var raised = Assert.IsType<DocumentDeletedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(document.Id, raised.DocumentId);
        Assert.Equal(DealId, raised.DealId);
        Assert.Equal(BuyerId, raised.DeletedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void Delete_WhenUploaded_SetsDeleted()
    {
        var document = NewDocument();
        document.MarkUploaded(Now);

        document.Delete(ListingAgentId, Now);

        Assert.Equal(DocumentStatusEnum.Deleted, document.Status);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsInvalidDocumentStatusTransitionException()
    {
        var document = NewDocument();
        document.Delete(ListingAgentId, Now);

        var exception = Assert.Throws<InvalidDocumentStatusTransitionException>(
            () => document.Delete(ListingAgentId, Now));

        Assert.Equal(DocumentStatusEnum.Deleted, exception.From);
        Assert.Equal(DocumentStatusEnum.Deleted, exception.To);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Delete_WithNonPositiveDeletedBy_ThrowsAndKeepsStatus(int deletedBy)
    {
        var document = NewDocument();

        Assert.Throws<DomainValidationException>(() => document.Delete(deletedBy, Now));

        Assert.Equal(DocumentStatusEnum.PendingUpload, document.Status);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Delete_WithNonUtcTime_ThrowsAndKeepsStatus(DateTimeKind kind)
    {
        var document = NewDocument();

        Assert.Throws<ArgumentException>(() => document.Delete(ListingAgentId, NowAs(kind)));

        Assert.Equal(DocumentStatusEnum.PendingUpload, document.Status);
    }
}
