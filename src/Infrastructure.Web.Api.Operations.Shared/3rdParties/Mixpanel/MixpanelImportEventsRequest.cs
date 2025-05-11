using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Common.Extensions;
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
    [JsonPropertyName("distinct_id")]
    public string? DistinctId
    {
        get => (string?)this[nameof(DistinctId).ToSnakeCase()];
        set => this[nameof(DistinctId).ToSnakeCase()] = value;
    }

    [JsonPropertyName("$insert_id")]
    public string? InsertId
    {
        get => (string?)this[$"${nameof(InsertId).ToSnakeCase()}"];
        set => this[$"${nameof(InsertId).ToSnakeCase()}"] = value;
    }

    [JsonPropertyName("ip")]
    public string? Ip
    {
        get => (string?)this[nameof(Ip).ToSnakeCase()];
        set => this[nameof(Ip).ToSnakeCase()] = value;
    }

    [JsonPropertyName("Referred by")]
    public string? ReferredBy
    {
        get => (string?)this["Referred by"];
        set => this["Referred by"] = value;
    }

    [JsonPropertyName("time")]
    public long? Time // unix timestamp
    {
        get => (long?)this[nameof(Time).ToSnakeCase()];
        set => this[nameof(Time).ToSnakeCase()] = value;
    }

    [JsonPropertyName("$current_url")]
    public string? Url
    {
        get => (string?)this["$current_url"];
        set => this["$current_url"] = value;
    }
}