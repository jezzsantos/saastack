using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;

namespace UserProfilesDomain;

public sealed class UserProfileRoot : AggregateRootBase
{
    public static Result<UserProfileRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ProfileType type, Identifier userId, PersonName name)
    {
        var root = new UserProfileRoot(recorder, idFactory);
        root.RaiseCreateEvent(UserProfilesDomain.Events.Created.Create(root.Id, type, userId, name));
        return root;
    }

    private UserProfileRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private UserProfileRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Address Address { get; private set; } = Address.Default;

    public Optional<string> AvatarUrl { get; private set; }

    public Optional<PersonDisplayName> DisplayName { get; private set; }

    public Optional<EmailAddress> EmailAddress { get; private set; }

    public Optional<PersonName> Name { get; private set; }

    public Optional<PhoneNumber> PhoneNumber { get; private set; }

    public Timezone Timezone { get; private set; } = Timezone.Default;

    public ProfileType Type { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public static AggregateRootFactory<UserProfileRoot> Rehydrate()
    {
        return (identifier, container, _) => new UserProfileRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (!Name.HasValue || !DisplayName.HasValue)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotNamed);
        }

        if (EmailAddress.HasValue && Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_PersonHasNoEmailAddress);
        }

        if (Type == ProfileType.Machine && EmailAddress.HasValue)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_MachineHasEmailAddress);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.Created created:
            {
                UserId = created.UserId.ToId();
                Type = created.Type.ToEnumOrDefault(ProfileType.Person);
                var name = PersonName.Create(created.FirstName, created.LastName);
                if (!name.IsSuccessful)
                {
                    return name.Error;
                }

                Name = name.Value;

                var displayName = PersonDisplayName.Create(created.DisplayName);
                if (!displayName.IsSuccessful)
                {
                    return displayName.Error;
                }

                DisplayName = displayName.Value;

                Address = Address.Default;
                EmailAddress = Optional<EmailAddress>.None;
                Timezone = Timezone.Default;
                AvatarUrl = Optional<string>.None;
                return Result.Ok;
            }

            case Events.EmailAddressChanged changed:
            {
                var email = Domain.Shared.EmailAddress.Create(changed.EmailAddress);
                if (!email.IsSuccessful)
                {
                    return email.Error;
                }

                EmailAddress = email.Value;
                Recorder.TraceDebug(null, "Person {Id} changed email to {EmailAddress}", Id,
                    changed.EmailAddress);
                return Result.Ok;
            }

            case Events.ContactAddressChanged changed:
            {
                var address = Address.Create(changed.Line1, changed.Line2, changed.Line3, changed.City, changed.State,
                    CountryCodes.FindOrDefault(changed.CountryCode), changed.Zip);
                if (!address.IsSuccessful)
                {
                    return address.Error;
                }

                Address = address.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed address", Id);
                return Result.Ok;
            }

            case Events.TimezoneChanged changed:
            {
                var timezone = Timezone.Create(changed.Timezone);
                if (!timezone.IsSuccessful)
                {
                    return timezone.Error;
                }

                Timezone = timezone.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed timezone to {Timezone}", Id, changed.Timezone);
                return Result.Ok;
            }

            case Events.NameChanged changed:
            {
                var name = PersonName.Create(changed.FirstName, changed.LastName);
                if (!name.IsSuccessful)
                {
                    return name.Error;
                }

                Name = name.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed name to {Name}", Id, name.Value.FullName);
                return Result.Ok;
            }

            case Events.DisplayNameChanged changed:
            {
                var name = PersonDisplayName.Create(changed.DisplayName);
                if (!name.IsSuccessful)
                {
                    return name.Error;
                }

                DisplayName = name.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed display name to {DisplayName}", Id, name.Value);
                return Result.Ok;
            }

            case Events.PhoneNumberChanged changed:
            {
                var number = Domain.Shared.PhoneNumber.Create(changed.Number);
                if (!number.IsSuccessful)
                {
                    return number.Error;
                }

                PhoneNumber = number.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed phone number to {PhoneNumber}", Id, number.Value);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeDisplayName(Identifier modifierId, PersonDisplayName displayName)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.DisplayNameChanged.Create(Id, UserId, displayName));
    }

    public Result<Error> ChangeName(Identifier modifierId, PersonName name)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.NameChanged.Create(Id, UserId, name));
    }

    public Result<Error> ChangePhoneNumber(Identifier modifierId, PhoneNumber number)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfilesDomain_NotAPerson);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.PhoneNumberChanged.Create(Id, UserId, number));
    }

    public Result<Error> SetContactAddress(Identifier modifierId, Address address)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.ContactAddressChanged.Create(Id, UserId, address));
    }

    public Result<Error> SetEmailAddress(Identifier modifierId, EmailAddress emailAddress)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfilesDomain_NotAPerson);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.EmailAddressChanged.Create(Id, UserId, emailAddress));
    }

    public Result<Error> SetTimezone(Identifier modifierId, Timezone timezone)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfilesDomain_NotOwner);
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.TimezoneChanged.Create(Id, UserId, timezone));
    }

#if TESTINGONLY
    public void TestingOnly_ChangeType(ProfileType type)
    {
        Type = type;
    }
#endif

    private bool IsNotOwner(Identifier modifierId)
    {
        return modifierId != UserId;
    }
}