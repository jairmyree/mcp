// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.EventHubs.Options.ConsumerGroup;

public class ConsumerGroupDeleteOptions : BaseEventHubsOptions
{
    [JsonPropertyName(EventHubsOptionDefinitions.EventHubNameName)]
    public string EventHubName { get; set; } = string.Empty;

    [JsonPropertyName(EventHubsOptionDefinitions.NamespaceName)]
    public string NamespaceName { get; set; } = string.Empty;

    [JsonPropertyName(EventHubsOptionDefinitions.ConsumerGroupNameName)]
    public string ConsumerGroupName { get; set; } = string.Empty;
}
