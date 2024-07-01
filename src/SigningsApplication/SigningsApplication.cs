using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using SigningsApplication.Persistence;
using SigningsDomain;

namespace SigningsApplication;

public class SigningsApplication : ISigningsApplication
{
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly ISigningRepository _repository;

    public SigningsApplication(IRecorder recorder, IIdentifierFactory identifierFactory, ISigningRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _repository = repository;
    }

    public async Task<Result<SigningRequest, Error>> CreateDraftAsync(ICallerContext caller, string organizationId,
        List<Signee> signees, CancellationToken cancellationToken)
    {
        var created = SigningRequestRoot.Create(_recorder, _identifierFactory, organizationId.ToId());
        if (created.IsFailure)
        {
            return created.Error;
        }

        var signingRequest = created.Value;
        var saved = await _repository.SaveAsync(signingRequest, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "SigningRequest draft {Id} was created", signingRequest.Id);
        return signingRequest.ToSigningRequest();
    }
}

internal static class SigningRequestConversionExtensions
{
    public static SigningRequest ToSigningRequest(this SigningRequestRoot signingRequestRoot)
    {
        return new SigningRequest
        {
            Id = signingRequestRoot.Id,
            OrganizationId = signingRequestRoot.OrganizationId
            // Signees = signingRequestRoot.Signees.Select(s => new Signee
            // {
            //     Email = s.Email,
            //     Name = s.Name
            // }).ToList()
        };
    }
}