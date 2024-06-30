using System.Reflection;
using Common;
using Common.Configuration;
using Common.FeatureFlags;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

namespace TestingStubApiHost.Api;

[WebService("/flagsmith")]
public sealed class StubFlagsmithApi : StubApiBase
{
    private static readonly List<FlagsmithFlag> Flags = GetAllFlags();

    public StubFlagsmithApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiPostResult<string, FlagsmithCreateIdentityResponse>> CreateIdentity(
        FlagsmithCreateIdentityRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubFlagsmith: CreateIdentity");
        return () =>
            new PostResult<FlagsmithCreateIdentityResponse>(new FlagsmithCreateIdentityResponse
            {
                Flags = Flags,
                Identifier = request.Identifier,
                Traits = request.Traits
            });
    }

    public async Task<ApiGetResult<string, FlagsmithGetEnvironmentFlagsResponse>> GetEnvironmentFlags(
        FlagsmithGetEnvironmentFlagsRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "StubFlagsmith: GetEnvironmentFlags");
        return () =>
            new Result<FlagsmithGetEnvironmentFlagsResponse, Error>(new FlagsmithGetEnvironmentFlagsResponse(Flags));
    }

    private static List<FlagsmithFlag> GetAllFlags()
    {
        var allFlags = typeof(Flag).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Flag))
            .Select(f => (Flag)f.GetValue(null)!)
            .ToList();

        var counter = 1000;
        return allFlags.Select(f => new FlagsmithFlag
        {
            Id = null,
            Enabled = false,
            Value = null,
            Feature = new FlagsmithFeature
            {
                Id = ++counter,
                Name = f.Name
            }
        }).ToList();
    }
}