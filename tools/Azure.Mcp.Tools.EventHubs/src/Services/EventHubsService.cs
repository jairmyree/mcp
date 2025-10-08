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

    public async Task<ConsumerGroup> UpdateConsumerGroupAsync(
        string consumerGroupName,
        string eventHubName,
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? userMetadata = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(consumerGroupName, eventHubName, namespaceName, resourceGroup, subscription);

        try
        {
            var armClient = await CreateArmClientAsync(tenant, retryPolicy);
            var subscriptionResource = armClient.GetSubscriptionResource(ResourceManager.Resources.SubscriptionResource.CreateResourceIdentifier(subscription));
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);
            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);
            var eventHubResource = await namespaceResource.Value.GetEventHubs().GetAsync(eventHubName);

            var consumerGroupData = new EventHubsConsumerGroupData();
            if (!string.IsNullOrEmpty(userMetadata))
            {
                consumerGroupData.UserMetadata = userMetadata;
            }

            var operation = await eventHubResource.Value.GetEventHubsConsumerGroups().CreateOrUpdateAsync(
                WaitUntil.Completed,
                consumerGroupName,
                consumerGroupData);

            var consumerGroupResource = operation.Value;
            if (string.IsNullOrEmpty(consumerGroupResource.Id))
            {
                throw new InvalidOperationException("Consumer group resource ID is missing");
            }

            var resourceId = new ResourceIdentifier(consumerGroupResource.Id!);

            return new ConsumerGroup(
                Name: consumerGroupResource.Data.Name,
                Id: consumerGroupResource.Id!,
                ResourceGroup: resourceId.ResourceGroupName ?? resourceGroup,
                Namespace: namespaceName,
                EventHub: eventHubName,
                Location: consumerGroupResource.Data.Location?.ToString(),
                UserMetadata: consumerGroupResource.Data.UserMetadata,
                CreationTime: consumerGroupResource.Data.CreatedOn,
                UpdatedTime: consumerGroupResource.Data.UpdatedOn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating/updating consumer group '{ConsumerGroupName}' in Event Hub '{EventHubName}' of namespace '{NamespaceName}'",
                consumerGroupName, eventHubName, namespaceName);
            throw;
        }
    }

    public async Task<bool> DeleteConsumerGroupAsync(
        string consumerGroupName,
        string eventHubName,
        string namespaceName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null)
    {
        ValidateRequiredParameters(consumerGroupName, eventHubName, namespaceName, resourceGroup, subscription);

        try
        {
            var armClient = await CreateArmClientAsync(tenant, retryPolicy);
            var subscriptionResource = armClient.GetSubscriptionResource(ResourceManager.Resources.SubscriptionResource.CreateResourceIdentifier(subscription));
            var resourceGroupResource = await subscriptionResource.GetResourceGroupAsync(resourceGroup);
            var namespaceResource = await resourceGroupResource.Value.GetEventHubsNamespaces().GetAsync(namespaceName);
            var eventHubResource = await namespaceResource.Value.GetEventHubs().GetAsync(eventHubName);

            var consumerGroupResource = await eventHubResource.Value.GetEventHubsConsumerGroups().GetAsync(consumerGroupName);

            await consumerGroupResource.Value.DeleteAsync(WaitUntil.Completed);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting consumer group '{ConsumerGroupName}' from Event Hub '{EventHubName}' of namespace '{NamespaceName}'",
                consumerGroupName, eventHubName, namespaceName);
            throw;
        }
    }

}
