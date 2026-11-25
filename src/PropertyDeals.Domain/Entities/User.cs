using PropertyDeals.Domain.Entities.Base;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Users;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Entities;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public RolesEnum Role { get; private set; }

    private User() { }

    public static User Create(Email email, string passwordHash, RolesEnum role, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (email is null)
        {
            throw new DomainValidationException("Email cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("Password hash cannot be null or whitespace.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException($"'{role}' is not a valid user role.");
        }

        var user = new User { Email = email, PasswordHash = passwordHash, Role = role };
        user.RaiseDomainEvent(new UserCreatedEvent(user, utcNow));

        return user;
    }

    public void ChangeRole(RolesEnum newRole, int changedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (!Enum.IsDefined(newRole))
        {
            throw new DomainValidationException($"'{newRole}' is not a valid user role.");
        }

        if (changedBy <= 0)
        {
            throw new DomainValidationException("ChangedBy Id must be positive.");
        }

        if (Role == newRole)
        {
            return;
        }

        var oldRole = Role;
        Role = newRole;
        RaiseDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole, changedBy, utcNow));
    }

    public void ChangePasswordHash(string newHash, int changedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (string.IsNullOrWhiteSpace(newHash))
        {
            throw new DomainValidationException("Password hash cannot be empty or whitespace.");
        }

        if (changedBy <= 0)
        {
            throw new DomainValidationException("ChangedBy Id must be positive.");
        }

        PasswordHash = newHash;
        RaiseDomainEvent(new UserPasswordChangedEvent(Id, changedBy, utcNow));
    }
}
