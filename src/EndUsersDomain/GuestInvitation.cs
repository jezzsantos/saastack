using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using JetBrains.Annotations;

namespace EndUsersDomain;

public sealed class GuestInvitation : ValueObjectBase<GuestInvitation>
{
    public static readonly TimeSpan DefaultTokenExpiry = TimeSpan.FromDays(14);
    public static readonly GuestInvitation Empty = new();

    public static Result<GuestInvitation, Error> Create()
    {
        return new GuestInvitation();
    }

    private GuestInvitation()
    {
        Token = Optional<string>.None;
        InviteeEmailAddress = Optional<EmailAddress>.None;
        ExpiresOnUtc = Optional<DateTime>.None;
        InvitedById = Optional<Identifier>.None;
        InvitedAtUtc = Optional<DateTime>.None;
        AcceptedEmailAddress = Optional<EmailAddress>.None;
        AcceptedAtUtc = Optional<DateTime>.None;
    }

    private GuestInvitation(Optional<string> token, Optional<EmailAddress> inviteeEmailAddress,
        Optional<DateTime> expiresOnUtc,
        Optional<Identifier> invitedById, Optional<DateTime> invitedAtUtc, Optional<EmailAddress> acceptedEmailAddress,
        Optional<DateTime> acceptedAtUtc)
    {
        Token = token;
        InviteeEmailAddress = inviteeEmailAddress;
        ExpiresOnUtc = expiresOnUtc;
        InvitedById = invitedById;
        InvitedAtUtc = invitedAtUtc;
        AcceptedEmailAddress = acceptedEmailAddress;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public Optional<DateTime> AcceptedAtUtc { get; }

    public Optional<EmailAddress> AcceptedEmailAddress { get; }

    public bool CanAccept => IsInvited && !IsAccepted;

    public Optional<DateTime> ExpiresOnUtc { get; }

    public Optional<DateTime> InvitedAtUtc { get; }

    public Optional<Identifier> InvitedById { get; }

    public Optional<EmailAddress> InviteeEmailAddress { get; }

    public bool IsAccepted => IsInvited && AcceptedAtUtc.HasValue;

    public bool IsInvited => Token.HasValue && InviteeEmailAddress.Exists();

    public bool IsStillOpen => IsInvited && ExpiresOnUtc.HasValue && ExpiresOnUtc > DateTime.UtcNow;

    public Optional<string> Token { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<GuestInvitation> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new GuestInvitation(
                parts[0],
                EmailAddress.Rehydrate()(parts[1], container),
                parts[2].ToOptional(val => val.FromIso8601()),
                Identifier.Rehydrate()(parts[3], container),
                parts[4].ToOptional(val => val.FromIso8601()),
                EmailAddress.Rehydrate()(parts[5], container),
                parts[6].ToOptional(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return
        [
            Token, InviteeEmailAddress, ExpiresOnUtc, InvitedById, InvitedAtUtc, AcceptedEmailAddress, AcceptedAtUtc
        ];
    }

    public Result<GuestInvitation, Error> Accept(EmailAddress acceptedWithEmail)
    {
        if (!IsInvited)
        {
            return Error.RuleViolation(Resources.GuestInvitation_NotInvited);
        }

        if (IsAccepted)
        {
            return Error.RuleViolation(Resources.GuestInvitation_AlreadyAccepted);
        }

        return new GuestInvitation(Token, InviteeEmailAddress, Optional<DateTime>.None, InvitedById, InvitedAtUtc,
            acceptedWithEmail,
            DateTime.UtcNow);
    }

    public Result<GuestInvitation, Error> Invite(string token, EmailAddress inviteeEmailAddress, Identifier invitedById)
    {
        if (token.IsInvalidParameter(Validations.Invitation.Token, nameof(token),
                Resources.GuestInvitation_InvalidToken, out var error))
        {
            return error;
        }

        if (IsInvited)
        {
            return Error.RuleViolation(Resources.GuestInvitation_AlreadyInvited);
        }

        if (IsAccepted)
        {
            return Error.RuleViolation(Resources.GuestInvitation_AlreadyAccepted);
        }

        return new GuestInvitation(token, inviteeEmailAddress, DateTime.UtcNow.Add(DefaultTokenExpiry), invitedById,
            DateTime.UtcNow, Optional<EmailAddress>.None, Optional<DateTime>.None);
    }

    public Result<GuestInvitation, Error> Renew(string token, EmailAddress inviteeEmailAddress)
    {
        if (token.IsInvalidParameter(Validations.Invitation.Token, nameof(token),
                Resources.GuestInvitation_InvalidToken, out var error))
        {
            return error;
        }

        if (!IsInvited)
        {
            return Error.RuleViolation(Resources.GuestInvitation_NotInvited);
        }

        if (IsAccepted)
        {
            return Error.RuleViolation(Resources.GuestInvitation_AlreadyAccepted);
        }

        return new GuestInvitation(token, inviteeEmailAddress, DateTime.UtcNow.Add(DefaultTokenExpiry), InvitedById,
            InvitedAtUtc, Optional<EmailAddress>.None, Optional<DateTime>.None);
    }

#if TESTINGONLY
    public GuestInvitation TestingOnly_ExpireNow()
    {
        return new GuestInvitation(Token, InviteeEmailAddress, DateTime.UtcNow, InvitedById, InvitedAtUtc,
            Optional<EmailAddress>.None, Optional<DateTime>.None);
    }
#endif
}