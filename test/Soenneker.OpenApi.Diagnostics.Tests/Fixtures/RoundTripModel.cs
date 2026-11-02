using System;
using System.Collections.Generic;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Soenneker.OpenApi.Diagnostics.Tests.Fixtures;

public sealed class RoundTripModel : IParsable, IAdditionalDataHolder
{
    public string? Name { get; set; }
    public IDictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    public static RoundTripModel CreateFromDiscriminatorValue(IParseNode node) => new();
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() => new Dictionary<string, Action<IParseNode>>
    {
        ["name"] = node => Name = node.GetStringValue()
    };
    public void Serialize(ISerializationWriter writer)
    {
        writer.WriteStringValue("name", Name);
        writer.WriteAdditionalData(AdditionalData);
    }
}
