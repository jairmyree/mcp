// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Areas;
using Azure.Mcp.Core.Commands;
using Azure.Mcp.Tools.EventHubs.Commands.ConsumerGroup;
using Azure.Mcp.Tools.EventHubs.Commands.Namespace;
using Azure.Mcp.Tools.EventHubs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Mcp.Tools.EventHubs;

public class EventHubsSetup : IAreaSetup
{
    public string Name => "eventhubs";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IEventHubsService, EventHubsService>();
        services.AddSingleton<NamespaceGetCommand>();
        services.AddSingleton<ConsumerGroupUpdateCommand>();
        services.AddSingleton<ConsumerGroupDeleteCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var eventHubs = new CommandGroup(Name, "Azure Event Hubs operations - Commands for managing Azure Event Hubs namespaces, event hubs, and consumer groups. Includes operations for getting namespaces and managing consumer groups.");

        var namespaceGroup = new CommandGroup("namespace", "Event Hubs namespace operations");
        eventHubs.AddSubGroup(namespaceGroup);

        var namespaceGetCommand = serviceProvider.GetRequiredService<NamespaceGetCommand>();
        namespaceGroup.AddCommand(namespaceGetCommand.Name, namespaceGetCommand);

        var consumerGroupGroup = new CommandGroup("consumergroup", "Event Hubs consumer group operations");
        eventHubs.AddSubGroup(consumerGroupGroup);

        var consumerGroupUpdateCommand = serviceProvider.GetRequiredService<ConsumerGroupUpdateCommand>();
        consumerGroupGroup.AddCommand(consumerGroupUpdateCommand.Name, consumerGroupUpdateCommand);

        var consumerGroupDeleteCommand = serviceProvider.GetRequiredService<ConsumerGroupDeleteCommand>();
        consumerGroupGroup.AddCommand(consumerGroupDeleteCommand.Name, consumerGroupDeleteCommand);

        return eventHubs;
    }
}
