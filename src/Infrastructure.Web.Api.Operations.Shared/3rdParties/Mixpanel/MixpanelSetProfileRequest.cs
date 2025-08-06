using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;

/// <summary>
///     Sets the profile of the user
/// </summary>
[Interfaces.Route("/engage#profile-set", OperationMethod.Post)]
public class MixpanelSetProfileRequest : UnTenantedRequest<MixpanelSetProfileRequest, MixpanelSetProfileResponse>,
    ICollection<MixpanelProfile>
{
    private readonly Collection<MixpanelProfile> _events = new();

    [JsonIgnore]
    [JsonPropertyName("verbose")]
    [FromQuery]
    public int Verbose { get; set; } = 1;

    public void Add(MixpanelProfile item)
    {
        _events.Add(item);
    }

    public void Clear()
    {
        _events.Clear();
    }

    public bool Contains(MixpanelProfile item)
    {
        return _events.Contains(item);
    }

    public void CopyTo(MixpanelProfile[] array, int arrayIndex)
    {
        _events.CopyTo(array, arrayIndex);
    }

#pragma warning disable SAASWEB035
    public int Count => _events.Count;
#pragma warning restore SAASWEB035

    public IEnumerator<MixpanelProfile> GetEnumerator()
    {
        return _events.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_events).GetEnumerator();
    }

#pragma warning disable SAASWEB035
    public bool IsReadOnly => ((ICollection<MixpanelProfile>)_events).IsReadOnly;
#pragma warning restore SAASWEB035

    public bool Remove(MixpanelProfile item)
    {
        return _events.Remove(item);
    }
}

public class MixpanelProfile
{
    [JsonPropertyName(MixpanelConstants.MetadataProperties.DistinctId)]
    public string? DistinctId { get; set; }

    [JsonPropertyName("$set")] public MixpanelProfileProperties? Set { get; set; }

    [JsonPropertyName("$token")] public string? Token { get; set; }
}

public class MixpanelProfileProperties : Dictionary<string, object?>
{
    public MixpanelProfileProperties()
    {
        Unsubscribed = true;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName)]
    public string? Avatar
    {
        get => (string?)this[$"${nameof(Avatar).ToSnakeCase()}"];
        set => this[$"${nameof(Avatar).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("$city")]
    public string? City
    {
        get => (string?)this[$"${nameof(City).ToSnakeCase()}"];
        set => this[$"${nameof(City).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName)]
    public string? CountryCode
    {
        get => (string?)this[$"${nameof(CountryCode).ToSnakeCase()}"];
        set => this[$"${nameof(CountryCode).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("$created")]
    public string? Created
    {
        get => (string?)this[$"${nameof(Created).ToSnakeCase()}"];
        set => this[$"${nameof(Created).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName)]
    public string? Email
    {
        get => (string?)this[$"${nameof(Email).ToSnakeCase()}"];
        set => this[$"${nameof(Email).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("$first_name")]
    public string? FirstName
    {
        get => (string?)this[$"${nameof(FirstName).ToSnakeCase()}"];
        set => this[$"${nameof(FirstName).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("$last_name")]
    public string? LastName
    {
        get => (string?)this[$"${nameof(LastName).ToSnakeCase()}"];
        set => this[$"${nameof(LastName).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ProfileNamePropertyName)]
    public string? Name
    {
        get => (string?)this[$"${nameof(Name).ToSnakeCase()}"];
        set => this[$"${nameof(Name).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("$phone")]
    public string? Phone
    {
        get => (string?)this[$"${nameof(Phone).ToSnakeCase()}"];
        set => this[$"${nameof(Phone).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName)]
    public string? Timezone
    {
        get => (string?)this[$"${nameof(Timezone).ToSnakeCase()}"];
        set => this[$"${nameof(Timezone).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.UnsubscribedPropertyName)]
    public bool? Unsubscribed
    {
        get => (bool?)this[$"${nameof(Unsubscribed).ToSnakeCase()}"];
        set => this[$"${nameof(Unsubscribed).ToSnakeCase()}"] = value;
    }
}