// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Identity;
using Azure.Mcp.Core.Commands;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.EventHubs.Options;
using Azure.Mcp.Tools.EventHubs.Options.EventHub;
using Azure.Mcp.Tools.EventHubs.Services;
using Microsoft.Extensions.Logging;

namespace Azure.Mcp.Tools.EventHubs.Commands.EventHub;

public sealed class EventHubDeleteCommand(ILogger<EventHubDeleteCommand> logger, IEventHubsService service)
    : BaseEventHubsCommand<EventHubDeleteOptions>
{
    private const string CommandTitle = "Delete Event Hub";
    private readonly IEventHubsService _service = service;
    private readonly ILogger<EventHubDeleteCommand> _logger = logger;

    public override string Name => "delete";

    public override string Description =>
        """
        Delete an Event Hub from an Azure Event Hubs namespace. This operation permanently removes
        the specified Event Hub and all its data. This is a destructive operation.

        The operation is idempotent - if the Event Hub doesn't exist, the command reports success
        with Deleted = false. If the Event Hub is successfully deleted, Deleted = true is returned.
        Warning: This operation cannot be undone. All messages and consumer groups in the Event Hub
        will be permanently deleted.
        """;

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new()
    {
        OpenWorld = false,
        Destructive = true,
        Idempotent = true,
        ReadOnly = false,
        Secret = false,
        LocalRequired = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(EventHubsOptionDefinitions.NamespaceOption.AsRequired());
        command.Options.Add(EventHubsOptionDefinitions.EventHubOption.AsRequired());
    }

    protected override EventHubDeleteOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.Namespace = parseResult.GetValueOrDefault<string>(EventHubsOptionDefinitions.NamespaceOption.Name);
        options.EventHub = parseResult.GetValueOrDefault<string>(EventHubsOptionDefinitions.EventHubOption.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        try
        {
            var deleted = await _service.DeleteEventHubAsync(
                options.EventHub!,
                options.Namespace!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy);

            context.Response.Results = ResponseResult.Create(
                new EventHubDeleteCommandResult(deleted, options.EventHub!),
                EventHubsJsonContext.Default.EventHubDeleteCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting event hub. EventHub: {EventHub}, Namespace: {Namespace}, ResourceGroup: {ResourceGroup}, Subscription: {Subscription}, Options: {@Options}",
                options.EventHub, options.Namespace, options.ResourceGroup, options.Subscription, options);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override HttpStatusCode GetStatusCode(Exception ex) => ex switch
    {
        RequestFailedException reqEx => (HttpStatusCode)reqEx.Status,
        AuthenticationFailedException => HttpStatusCode.Unauthorized,
        ArgumentException => HttpStatusCode.BadRequest,
        _ => base.GetStatusCode(ex)
    };

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        AuthenticationFailedException =>
            "Authentication failed. Please ensure your Azure credentials are properly configured and have not expired.",
        RequestFailedException reqEx when reqEx.Status == 403 =>
            "Access denied. Please ensure you have sufficient permissions to delete Event Hubs in the specified namespace and resource group.",
        RequestFailedException reqEx when reqEx.Status == 404 =>
            "The specified namespace, resource group, or subscription was not found. Note: If the event hub doesn't exist, the operation succeeds with Deleted = false.",
        RequestFailedException reqEx when reqEx.Status == 409 =>
            "Conflict occurred. The event hub may be in use or in a transitional state. Please try again later.",
        ArgumentException argEx when argEx.ParamName == "eventHubName" =>
            "Invalid event hub name. Please provide a valid event hub name.",
        ArgumentException argEx when argEx.ParamName == "namespaceName" =>
            "Invalid namespace name. Please provide a valid Event Hubs namespace name.",
        ArgumentException argEx when argEx.ParamName == "resourceGroup" =>
            "Invalid resource group name. Please provide a valid resource group name.",
        ArgumentException argEx when argEx.ParamName == "subscription" =>
            "Invalid subscription. Please provide a valid subscription ID or name.",
        _ => base.GetErrorMessage(ex)
    };

    internal record EventHubDeleteCommandResult(bool Deleted, string EventHubName);
}
