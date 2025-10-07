// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.EventHubs.Options;

namespace Azure.Mcp.Tools.EventHubs.Options.ConsumerGroup;

public class ConsumerGroupUpdateOptions : BaseEventHubsOptions
{
    [JsonPropertyName("eventHubName")]
    public string EventHubName { get; set; } = string.Empty;

    [JsonPropertyName("namespaceName")]
    public string NamespaceName { get; set; } = string.Empty;

    [JsonPropertyName("consumerGroupName")]
    public string ConsumerGroupName { get; set; } = string.Empty;

    [JsonPropertyName("userMetadata")]
    public string? UserMetadata { get; set; }
}
