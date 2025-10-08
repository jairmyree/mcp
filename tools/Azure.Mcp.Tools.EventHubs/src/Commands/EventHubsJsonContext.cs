// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.EventHubs.Commands.ConsumerGroup;
using Azure.Mcp.Tools.EventHubs.Commands.Namespace;
using Azure.Mcp.Tools.EventHubs.Models;

namespace Azure.Mcp.Tools.EventHubs.Commands;

[JsonSerializable(typeof(Models.ConsumerGroup))]
[JsonSerializable(typeof(ConsumerGroupDeleteCommand.ConsumerGroupDeleteCommandResult))]
[JsonSerializable(typeof(ConsumerGroupUpdateCommand.ConsumerGroupUpdateCommandResult))]
[JsonSerializable(typeof(EventHubsNamespaceData))]
[JsonSerializable(typeof(EventHubsNamespaceSku))]
[JsonSerializable(typeof(Models.Namespace))]
[JsonSerializable(typeof(NamespaceGetCommand.NamespaceGetCommandResult))]
[JsonSerializable(typeof(NamespaceGetCommand.NamespacesGetCommandResult))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class EventHubsJsonContext : JsonSerializerContext;
