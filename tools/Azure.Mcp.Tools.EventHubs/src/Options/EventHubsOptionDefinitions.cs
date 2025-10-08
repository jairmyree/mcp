// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.EventHubs.Options;

public static class EventHubsOptionDefinitions
{
    public const string Namespace = "namespace";
    public const string EventHubName = "eventhub";
    public const string ConsumerGroupName = "consumer-group";
    public const string UserMetadata = "user-metadata";

    public static readonly Option<string> NamespaceName = new(
        $"--{Namespace}"
    )
    {
        Description = "The name of the Event Hubs namespace to retrieve. Must be used with --resource-group option.",
        Required = false
    };

    public static readonly Option<string> EventHubNameOption = new(
        $"--{EventHubName}"
    )
    {
        Description = "The name of the Event Hub within the namespace.",
        Required = false
    };

    public static readonly Option<string> ConsumerGroupNameOption = new(
        $"--{ConsumerGroupName}"
    )
    {
        Description = "The name of the consumer group within the Event Hub.",
        Required = false
    };

    public static readonly Option<string> UserMetadataOption = new(
        $"--{UserMetadata}"
    )
    {
        Description = "User metadata for the consumer group.",
        Required = false
    };
}
