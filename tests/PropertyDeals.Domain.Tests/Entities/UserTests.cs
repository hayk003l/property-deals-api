using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Users;
using PropertyDeals.Domain.Exceptions;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidInput_SetsAllProperties()
    {
        var email = ValidEmail();

        var user = User.Create(email, "hash-v1", RolesEnum.Client, Now);

        Assert.Equal(email, user.Email);
        Assert.Equal("hash-v1", user.PasswordHash);
        Assert.Equal(RolesEnum.Client, user.Role);
    }

    [Fact]
    public void Create_WithValidInput_RaisesUserCreatedEvent()
    {
        var user = User.Create(ValidEmail(), "hash-v1", RolesEnum.Agent, Now);

        var raised = Assert.IsType<UserCreatedEvent>(Assert.Single(user.DomainEvents));
        Assert.Same(user, raised.User);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => User.Create(null!, "hash-v1", RolesEnum.Agent, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingPasswordHash_ThrowsDomainValidationException(string? passwordHash)
    {
        Assert.Throws<DomainValidationException>(() => User.Create(ValidEmail(), passwordHash!, RolesEnum.Agent, Now));
    }

    [Fact]
    public void Create_WithUndefinedRole_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => User.Create(ValidEmail(), "hash-v1", (RolesEnum)99, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        Assert.Throws<ArgumentException>(() => User.Create(ValidEmail(), "hash-v1", RolesEnum.Agent, NowAs(kind)));
    }

    [Fact]
    public void ChangeRole_ToNewRole_UpdatesRoleAndRaisesEventWithOldAndNewRole()
    {
        var user = NewUser(RolesEnum.Agent);

        user.ChangeRole(RolesEnum.Client, AdminId, Now);

        Assert.Equal(RolesEnum.Client, user.Role);
        var raised = Assert.IsType<UserRoleChangedEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, raised.UserId);
        Assert.Equal(RolesEnum.Agent, raised.OldRole);
        Assert.Equal(RolesEnum.Client, raised.NewRole);
        Assert.Equal(AdminId, raised.ChangedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void ChangeRole_ToSameRole_DoesNothing()
    {
        var user = NewUser(RolesEnum.Agent);

        user.ChangeRole(RolesEnum.Agent, AdminId, Now);

        Assert.Equal(RolesEnum.Agent, user.Role);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void ChangeRole_ToUndefinedRole_ThrowsDomainValidationException()
    {
        var user = NewUser();

        Assert.Throws<DomainValidationException>(() => user.ChangeRole((RolesEnum)99, AdminId, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeRole_WithNonPositiveChangedBy_ThrowsDomainValidationException(int changedBy)
    {
        var user = NewUser();

        Assert.Throws<DomainValidationException>(() => user.ChangeRole(RolesEnum.Client, changedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ChangeRole_WithNonUtcTime_ThrowsAndKeepsRole(DateTimeKind kind)
    {
        var user = NewUser(RolesEnum.Agent);

        Assert.Throws<ArgumentException>(() => user.ChangeRole(RolesEnum.Client, AdminId, NowAs(kind)));

        Assert.Equal(RolesEnum.Agent, user.Role);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void ChangePasswordHash_WithNewHash_UpdatesHashAndRaisesEvent()
    {
        var user = NewUser();

        user.ChangePasswordHash("hash-v2", ListingAgentId, Now);

        Assert.Equal("hash-v2", user.PasswordHash);
        var raised = Assert.IsType<UserPasswordChangedEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, raised.UserId);
        Assert.Equal(ListingAgentId, raised.ChangedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void UserPasswordChangedEvent_DoesNotExposeThePasswordHash()
    {
        var properties = typeof(UserPasswordChangedEvent).GetProperties();

        Assert.DoesNotContain(properties, p => p.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePasswordHash_WithMissingHash_ThrowsAndKeepsHash(string? newHash)
    {
        var user = NewUser();

        Assert.Throws<DomainValidationException>(() => user.ChangePasswordHash(newHash!, ListingAgentId, Now));

        Assert.Equal("hash-v1", user.PasswordHash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangePasswordHash_WithNonPositiveChangedBy_ThrowsDomainValidationException(int changedBy)
    {
        var user = NewUser();

        Assert.Throws<DomainValidationException>(() => user.ChangePasswordHash("hash-v2", changedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ChangePasswordHash_WithNonUtcTime_ThrowsAndKeepsHash(DateTimeKind kind)
    {
        var user = NewUser();

        Assert.Throws<ArgumentException>(() => user.ChangePasswordHash("hash-v2", ListingAgentId, NowAs(kind)));

        Assert.Equal("hash-v1", user.PasswordHash);
        Assert.Empty(user.DomainEvents);
    }
}
