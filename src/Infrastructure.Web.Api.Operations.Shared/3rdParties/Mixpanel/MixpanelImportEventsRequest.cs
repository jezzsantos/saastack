using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;

/// <summary>
///     Imports product usage events
/// </summary>
[Interfaces.Route("/import", OperationMethod.Post)]
public class MixpanelImportEventsRequest : UnTenantedRequest<MixpanelImportEventsRequest, MixpanelImportEventsResponse>,
    ICollection<MixpanelImportEvent>
{
    private readonly Collection<MixpanelImportEvent> _events = new();

    [JsonIgnore]
    [JsonPropertyName("project_id")]
    [FromQuery]
    public string? ProjectId { get; set; }

    [JsonIgnore]
    [JsonPropertyName("strict")]
    [FromQuery]
    public int Strict { get; set; } = 1;

    public void Add(MixpanelImportEvent item)
    {
        _events.Add(item);
    }

    public void Clear()
    {
        _events.Clear();
    }

    public bool Contains(MixpanelImportEvent item)
    {
        return _events.Contains(item);
    }

    public void CopyTo(MixpanelImportEvent[] array, int arrayIndex)
    {
        _events.CopyTo(array, arrayIndex);
    }

#pragma warning disable SAASWEB035
    public int Count => _events.Count;
#pragma warning restore SAASWEB035

    public IEnumerator<MixpanelImportEvent> GetEnumerator()
    {
        return _events.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

#pragma warning disable SAASWEB035
    public bool IsReadOnly => ((ICollection<MixpanelImportEvent>)_events).IsReadOnly;
#pragma warning restore SAASWEB035

    public bool Remove(MixpanelImportEvent item)
    {
        return _events.Remove(item);
    }
}

public class MixpanelImportEvent
{
    [JsonPropertyName("event")] public string? Event { get; set; }

    [JsonPropertyName("properties")] public MixpanelEventProperties? Properties { get; set; }
}

public class MixpanelEventProperties : Dictionary<string, object?>
{
    [JsonPropertyName(MixpanelConstants.MetadataProperties.DistinctId)]
    public string? DistinctId
    {
        get => (string?)this[MixpanelConstants.MetadataProperties.DistinctId];
        set => this[MixpanelConstants.MetadataProperties.DistinctId] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.InsertId)]
    public string? InsertId
    {
        get => (string?)this[MixpanelConstants.MetadataProperties.InsertId];
        set => this[MixpanelConstants.MetadataProperties.InsertId] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.IpAddress)]
    public string? Ip
    {
        get => (string?)this[MixpanelConstants.MetadataProperties.IpAddress];
        set => this[MixpanelConstants.MetadataProperties.IpAddress] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.ReferredBy)]
    public string? ReferredBy
    {
        get => (string?)this[MixpanelConstants.MetadataProperties.ReferredBy];
        set => this[MixpanelConstants.MetadataProperties.ReferredBy] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.Time)]
    public long? Time // unix timestamp
    {
        get => (long?)this[MixpanelConstants.MetadataProperties.Time];
        set => this[MixpanelConstants.MetadataProperties.Time] = value;
    }

    [JsonPropertyName(MixpanelConstants.MetadataProperties.Url)]
    public string? Url
    {
        get => (string?)this[MixpanelConstants.MetadataProperties.Url];
        set => this[MixpanelConstants.MetadataProperties.Url] = value;
    }
}