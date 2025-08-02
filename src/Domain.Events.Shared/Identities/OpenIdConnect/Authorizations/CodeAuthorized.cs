using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Identities;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;

public sealed class CodeAuthorized : DomainEvent
{
    public CodeAuthorized(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public CodeAuthorized()
    {
    }

    public required DateTime AuthorizedAt { get; set; }

    public required string ClientId { get; set; }

    public required string Code { get; set; }

    public string? CodeChallenge { get; set; }

    public OpenIdConnectCodeChallengeMethod? CodeChallengeMethod { get; set; }

    public required DateTime ExpiresAt { get; set; }

    public string? Nonce { get; set; }

    public required string RedirectUri { get; set; }

    public required List<string> Scopes { get; set; }

    public required string UserId { get; set; }
}