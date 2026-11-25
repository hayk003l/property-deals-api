using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Tests;

internal static class TestData
{
    public const int PropertyId = 1;
    public const int ListingAgentId = 10;
    public const int BuyerAgentId = 20;
    public const int BuyerId = 30;
    public const int SellerId = 40;
    public const int StrangerId = 50;
    public const int AdminId = 90;
    public const int DealId = 42;

    public static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now);
    public static readonly DateOnly DefaultClosingDate = Today.AddDays(30);

    public static DateTime NowAs(DateTimeKind kind) => DateTime.SpecifyKind(Now, kind);

    public static Money Price(decimal amount = 250_000m, string currency = "USD") => Money.Create(amount, currency);

    public static Address ValidAddress() => Address.Create("1 Main St", "Springfield", "IL", "62701", "US");

    public static Email ValidEmail() => Email.Create("agent@example.com");

    public static Deal NewDeal()
    {
        var deal = Deal.Create(PropertyId, ListingAgentId, RolesEnum.Agent, Now);
        deal.ClearDomainEvents();
        return deal;
    }

    public static Deal DealIn(DealStatusEnum status) => MoveTo(NewDeal(), status);

    public static Deal MoveTo(Deal deal, DealStatusEnum target)
    {
        foreach (var step in PathTo(target))
        {
            DateOnly? closingDate = step == DealStatusEnum.UnderContract ? DefaultClosingDate : null;
            deal.ChangeStatus(step, ListingAgentId, Now, closingDate);
        }

        deal.ClearDomainEvents();
        return deal;
    }

    public static Property NewProperty()
    {
        var property = Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(), 3, ListingAgentId, Now);
        property.ClearDomainEvents();
        return property;
    }

    public static User NewUser(RolesEnum role = RolesEnum.Agent)
    {
        var user = User.Create(ValidEmail(), "hash-v1", role, Now);
        user.ClearDomainEvents();
        return user;
    }

    public static Document NewDocument(string fileName = "contract.pdf")
    {
        var document = Document.Create(DealId, fileName, ListingAgentId, Now);
        document.ClearDomainEvents();
        return document;
    }

    private static DealStatusEnum[] PathTo(DealStatusEnum target) => target switch
    {
        DealStatusEnum.Draft => [],
        DealStatusEnum.Active => [DealStatusEnum.Active],
        DealStatusEnum.UnderContract => [DealStatusEnum.Active, DealStatusEnum.UnderContract],
        DealStatusEnum.Closing => [DealStatusEnum.Active, DealStatusEnum.UnderContract, DealStatusEnum.Closing],
        DealStatusEnum.Closed => [DealStatusEnum.Active, DealStatusEnum.UnderContract, DealStatusEnum.Closing, DealStatusEnum.Closed],
        DealStatusEnum.Cancelled => [DealStatusEnum.Cancelled],
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
    };
}
