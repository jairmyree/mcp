// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Azure.Mcp.Tools.EventHubs.Models;
using Azure.ResourceManager.EventHubs;
using Microsoft.Extensions.Logging;

namespace Azure.Mcp.Tools.EventHubs.Services;

public class EventHubsService(ISubscriptionService subscriptionService, ITenantService tenantService, ILogger<EventHubsService> logger)
    : BaseAzureResourceService(subscriptionService, tenantService), IEventHubsService
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ITenantService _tenantService = tenantService;
    private readonly ILogger<EventHubsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<List<Namespace>> GetNamespacesAsync(
        string? resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        var namespaces = await ExecuteResourceQueryAsync(
                "Microsoft.EventHub/namespaces",
                resourceGroup,
                subscription,
                retryPolicy,
                ConvertToNamespace);
        return namespaces ?? [];
    }

    private static Namespace ConvertToNamespace(JsonElement item)
    {
        Models.EventHubsNamespaceData? eventHubsNamespace = Models.EventHubsNamespaceData.FromJson(item);
        if (eventHubsNamespace == null)
        {
            throw new InvalidOperationException("Failed to parse EventHubs namespace data");
        }


        if (string.IsNullOrEmpty(eventHubsNamespace.ResourceId))
        {
            throw new InvalidOperationException("Resource ID is missing");
        }

        var id = new ResourceIdentifier(eventHubsNamespace.ResourceId)!;

        if (string.IsNullOrEmpty(id.ResourceGroupName))
        {
            throw new InvalidOperationException("Resource ID is missing resource group");
        }

        if (string.IsNullOrEmpty(eventHubsNamespace.ResourceName))
        {
            throw new InvalidOperationException("Resource Name is missing");
        }

        return new Namespace(
            Name: eventHubsNamespace.ResourceName,
            Id: eventHubsNamespace.ResourceId,
            ResourceGroup: id.ResourceGroupName,
            Location: eventHubsNamespace.Location,
            Sku: new EventHubsNamespaceSku(
                Name: eventHubsNamespace.Sku.Name,
                Tier: eventHubsNamespace.Sku.Tier,
                Capacity: eventHubsNamespace.Sku.Capacity),
            Status: eventHubsNamespace.Properties?.Status,
            ProvisioningState: eventHubsNamespace.Properties?.ProvisioningState,
            CreationTime: eventHubsNamespace.Properties?.CreatedOn,
            UpdatedTime: eventHubsNamespace.Properties?.UpdatedOn,
            ServiceBusEndpoint: eventHubsNamespace.Properties?.ServiceBusEndpoint,
            MetricId: eventHubsNamespace.Properties?.MetricId,
            IsAutoInflateEnabled: eventHubsNamespace.Properties?.IsAutoInflateEnabled,
            MaximumThroughputUnits: eventHubsNamespace.Properties?.MaximumThroughputUnits,
            KafkaEnabled: eventHubsNamespace.Properties?.KafkaEnabled,
            ZoneRedundant: eventHubsNamespace.Properties?.ZoneRedundant,
            Tags: eventHubsNamespace.Tags != null ? new Dictionary<string, string>(eventHubsNamespace.Tags) : null);
    }

    public async Task<Namespace> GetNamespaceAsync(
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);

        try
        {
            var namespaceDetails = await ExecuteSingleResourceQueryAsync(
                            "Microsoft.EventHub/namespaces",
                            resourceGroup,
                            subscription,
                            retryPolicy,
                            ConvertToNamespace,
                            $"name =~ '{EscapeKqlString(namespaceName)}'");

            if (namespaceDetails == null)
            {
                throw new KeyNotFoundException($"Event Hubs namespace '{namespaceName}' not found for subscription '{subscription}'.");
            }
            return namespaceDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving Event Hubs namespace '{NamespaceName}' for subscription '{Subscription}'",
                namespaceName, subscription);
            throw;
        }
    }

    public async Task<List<EventHubInfo>> ListEventHubsAsync(
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);

        try
        {
            var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy);
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);

            if (resourceGroupResource?.Value == null)
            {
                throw new InvalidOperationException($"Resource group '{resourceGroup}' not found");
            }

            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);

            if (namespaceResource?.Value == null)
            {
                throw new KeyNotFoundException($"Event Hubs namespace '{namespaceName}' not found in resource group '{resourceGroup}'");
            }

            var eventHubList = new List<EventHubInfo>();

            await foreach (var eventHub in namespaceResource.Value.GetEventHubs())
            {
                eventHubList.Add(ConvertToEventHubInfo(eventHub.Data, resourceGroup));
            }

            return eventHubList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error listing event hubs in namespace '{NamespaceName}' for subscription '{Subscription}'",
                namespaceName, subscription);
            throw;
        }
    }

    public async Task<EventHubInfo?> GetEventHubAsync(
        string eventHubName,
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);

        try
        {
            var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy);
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);

            if (resourceGroupResource?.Value == null)
            {
                throw new InvalidOperationException($"Resource group '{resourceGroup}' not found");
            }

            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);

            if (namespaceResource?.Value == null)
            {
                throw new KeyNotFoundException($"Event Hubs namespace '{namespaceName}' not found in resource group '{resourceGroup}'");
            }

            var eventHubResource = await namespaceResource.Value.GetEventHubs().GetAsync(eventHubName);

            if (eventHubResource?.Value == null)
            {
                return null;
            }

            return ConvertToEventHubInfo(eventHubResource.Value.Data, resourceGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving event hub '{EventHubName}' in namespace '{NamespaceName}' for subscription '{Subscription}'",
                eventHubName, namespaceName, subscription);
            throw;
        }
    }

    private static EventHubInfo ConvertToEventHubInfo(EventHubData eventHub, string resourceGroup)
    {
        return new EventHubInfo(
            Name: eventHub.Name,
            Id: eventHub.Id.ToString(),
            ResourceGroup: resourceGroup,
            Location: null, // Event hubs inherit location from namespace
            PartitionCount: eventHub.PartitionCount.HasValue ? (int)eventHub.PartitionCount.Value : null,
            MessageRetentionInDays: eventHub.RetentionDescription?.RetentionTimeInHours.HasValue == true
                ? (int)(eventHub.RetentionDescription.RetentionTimeInHours.Value / 24)
                : null,
            Status: eventHub.Status?.ToString(),
            CreatedOn: eventHub.CreatedOn,
            UpdatedOn: eventHub.UpdatedOn,
            PartitionIds: eventHub.PartitionIds?.ToList());
    }

    public async Task<EventHubInfo> CreateOrUpdateEventHubAsync(
        string eventHubName,
        string namespaceName,
        string resourceGroup,
        string subscription,
        int? partitionCount = null,
        long? messageRetentionInHours = null,
        string? status = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);

        try
        {
            var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy);
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);

            if (resourceGroupResource?.Value == null)
            {
                throw new InvalidOperationException($"Resource group '{resourceGroup}' not found");
            }

            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);

            if (namespaceResource?.Value == null)
            {
                throw new KeyNotFoundException($"Event Hubs namespace '{namespaceName}' not found in resource group '{resourceGroup}'");
            }

            var eventHubData = new EventHubData();

            if (partitionCount.HasValue)
            {
                eventHubData.PartitionCount = partitionCount.Value;
            }

            if (messageRetentionInHours.HasValue && eventHubData.RetentionDescription != null)
            {
                eventHubData.RetentionDescription.RetentionTimeInHours = messageRetentionInHours.Value;
            }

            if (!string.IsNullOrEmpty(status) && eventHubData.Status.HasValue)
            {
                // Status is typically read-only, so we'll log a warning if attempted
                _logger.LogWarning("Status cannot be directly set on EventHub creation/update. Current status: {Status}", eventHubData.Status);
            }

            var operation = await namespaceResource.Value.GetEventHubs()
                .CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, eventHubData);

            if (operation?.Value == null)
            {
                throw new InvalidOperationException($"Failed to create or update event hub '{eventHubName}'");
            }

            return ConvertToEventHubInfo(operation.Value.Data, resourceGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating or updating event hub '{EventHubName}' in namespace '{NamespaceName}' for subscription '{Subscription}'",
                eventHubName, namespaceName, subscription);
            throw;
        }
    }

    public async Task<bool> DeleteEventHubAsync(
        string eventHubName,
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(subscription);

        try
        {
            var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy);
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);

            if (resourceGroupResource?.Value == null)
            {
                throw new InvalidOperationException($"Resource group '{resourceGroup}' not found");
            }

            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);

            if (namespaceResource?.Value == null)
            {
                throw new KeyNotFoundException($"Event Hubs namespace '{namespaceName}' not found in resource group '{resourceGroup}'");
            }

            var eventHubResource = await namespaceResource.Value.GetEventHubs().GetIfExistsAsync(eventHubName);

            if (eventHubResource?.Value == null)
            {
                _logger.LogInformation("Event hub '{EventHubName}' not found in namespace '{NamespaceName}', nothing to delete", eventHubName, namespaceName);
                return false;
            }

            await eventHubResource.Value.DeleteAsync(WaitUntil.Completed);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting event hub '{EventHubName}' in namespace '{NamespaceName}' for subscription '{Subscription}'",
                eventHubName, namespaceName, subscription);
            throw;
        }
    }

}
