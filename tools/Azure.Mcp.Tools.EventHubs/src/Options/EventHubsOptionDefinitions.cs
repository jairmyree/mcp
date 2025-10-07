// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.EventHubs.Options;

public static class EventHubsOptionDefinitions
{
    public const string Namespace = "namespace";
    public const string EventHub = "eventhub";
    public const string PartitionCount = "partition-count";
    public const string MessageRetentionInHours = "message-retention-in-hours";
    public const string EventHubStatus = "status";

    public static readonly Option<string> NamespaceOption = new(
        $"--{Namespace}"
    )
    {
        Description = "The name of the Event Hubs namespace to retrieve. Must be used with --resource-group option.",
        Required = false
    };

    public static readonly Option<string> EventHubOption = new(
        $"--{EventHub}"
    )
    {
        Description = "The name of the event hub to retrieve from the namespace. Must be used with --namespace and --resource-group options.",
        Required = false
    };

    public static readonly Option<int?> PartitionCountOption = new(
        $"--{PartitionCount}"
    )
    {
        Description = "The number of partitions for the event hub. Must be between 1 and 32 (or higher based on namespace tier).",
        Required = false
    };

    public static readonly Option<long?> MessageRetentionInHoursOption = new(
        $"--{MessageRetentionInHours}"
    )
    {
        Description = "The message retention time in hours. Minimum is 1 hour, maximum depends on the namespace tier.",
        Required = false
    };

    public static readonly Option<string> StatusOption = new(
        $"--{EventHubStatus}"
    )
    {
        Description = "The status of the event hub (Active, Disabled, etc.). Note: Status may be read-only in some operations.",
        Required = false
    };
}
