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
        Token = null;
        InviteeEmailAddress = null;
        ExpiresOnUtc = null;
        InvitedById = null;
        InvitedAtUtc = null;
        AcceptedEmailAddress = null;
        AcceptedAtUtc = null;
    }

    private GuestInvitation(string? token, EmailAddress? inviteeEmailAddress, DateTime? expiresOnUtc,
        Identifier? invitedById, DateTime? invitedAtUtc, EmailAddress? acceptedEmailAddress, DateTime? acceptedAtUtc)
    {
        Token = token;
        InviteeEmailAddress = inviteeEmailAddress;
        ExpiresOnUtc = expiresOnUtc;
        InvitedById = invitedById;
        InvitedAtUtc = invitedAtUtc;
        AcceptedEmailAddress = acceptedEmailAddress;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public DateTime? AcceptedAtUtc { get; }

    public EmailAddress? AcceptedEmailAddress { get; }

    public bool CanAccept => IsInvited && !IsAccepted;

    public DateTime? ExpiresOnUtc { get; }

    public DateTime? InvitedAtUtc { get; }

    public Identifier? InvitedById { get; }

    public EmailAddress? InviteeEmailAddress { get; }

    public bool IsAccepted => IsInvited && AcceptedAtUtc.HasValue;

    public bool IsInvited => Token.HasValue() && InviteeEmailAddress.Exists();

    public bool IsStillOpen => IsInvited && ExpiresOnUtc.HasValue() && ExpiresOnUtc > DateTime.UtcNow;

    public string? Token { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<GuestInvitation> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new GuestInvitation(parts[0]!,
                EmailAddress.Rehydrate()(parts[1]!, container),
                parts[2]?.FromIso8601(),
                Identifier.Rehydrate()(parts[3]!, container),
                parts[4]?.FromIso8601(),
                EmailAddress.Rehydrate()(parts[1]!, container),
                parts[6]?.FromIso8601());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[]
        {
            Token, InviteeEmailAddress, ExpiresOnUtc, InvitedById, InvitedAtUtc, AcceptedEmailAddress, AcceptedAtUtc
        };
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

        return new GuestInvitation(Token, InviteeEmailAddress, null, InvitedById, InvitedAtUtc, acceptedWithEmail,
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
            DateTime.UtcNow, null, null);
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
            InvitedAtUtc, null, null);
    }

#if TESTINGONLY
    public GuestInvitation TestingOnly_ExpireNow()
    {
        return new GuestInvitation(Token, InviteeEmailAddress, DateTime.UtcNow, InvitedById, InvitedAtUtc, null, null);
    }
#endif
}