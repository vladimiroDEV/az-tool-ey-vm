using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using AzurePortalTools.Models;

namespace AzurePortalTools.Services;

public class AzureService
{
    private readonly ILogger<AzureService> _logger;

    public AzureService(ILogger<AzureService> logger)
    {
        _logger = logger;
    }

    private ArmClient CreateClient(TenantConfig tenant)
    {
        var credential = new ClientSecretCredential(
            tenant.TenantId,
            tenant.ClientId,
            tenant.ClientSecret);
        return new ArmClient(credential, tenant.SubscriptionId);
    }

    // ── Resource Groups ────────────────────────────────────────────────────
    public async Task<List<string>> GetResourceGroupsAsync(TenantConfig tenant)
    {
        var client = CreateClient(tenant);
        var subscription = await client.GetDefaultSubscriptionAsync();
        var groups = new List<string>();
        await foreach (var rg in subscription.GetResourceGroups().GetAllAsync())
        {
            groups.Add(rg.Data.Name);
        }
        return groups;
    }

    // ── Virtual Machines ───────────────────────────────────────────────────
    public async Task<List<VmInfo>> GetVmsInResourceGroupAsync(TenantConfig tenant, string resourceGroup)
    {
        var client = CreateClient(tenant);
        var subscription = await client.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().GetAsync(resourceGroup);
        var vms = new List<VmInfo>();

        await foreach (var vm in rg.Value.GetVirtualMachines().GetAllAsync())
        {
            // Retrieve instance view for power state
            var instanceView = await vm.InstanceViewAsync();
            var powerState = "unknown";
            if (instanceView?.Value?.Statuses != null)
            {
                var statusEntry = instanceView.Value.Statuses
                    .FirstOrDefault(s => s.Code != null && s.Code.StartsWith("PowerState/"));
                if (statusEntry != null)
                    powerState = statusEntry.Code!.Replace("PowerState/", "");
            }

            vms.Add(new VmInfo
            {
                Name = vm.Data.Name,
                ResourceGroup = resourceGroup,
                PowerState = powerState,
                Location = vm.Data.Location
            });
        }
        return vms;
    }

    public async Task<string> StartVmAsync(TenantConfig tenant, string resourceGroup, string vmName)
    {
        try
        {
            var client = CreateClient(tenant);
            var subscription = await client.GetDefaultSubscriptionAsync();
            var rg = await subscription.GetResourceGroups().GetAsync(resourceGroup);
            var vm = await rg.Value.GetVirtualMachines().GetAsync(vmName);
            await vm.Value.PowerOnAsync(WaitUntil.Completed);
            return "started";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting VM {vm}", vmName);
            return $"error: {ex.Message}";
        }
    }

    public async Task<string> StopVmAsync(TenantConfig tenant, string resourceGroup, string vmName)
    {
        try
        {
            var client = CreateClient(tenant);
            var subscription = await client.GetDefaultSubscriptionAsync();
            var rg = await subscription.GetResourceGroups().GetAsync(resourceGroup);
            var vm = await rg.Value.GetVirtualMachines().GetAsync(vmName);
            await vm.Value.DeallocateAsync(WaitUntil.Completed);
            return "deallocated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping VM {vm}", vmName);
            return $"error: {ex.Message}";
        }
    }

    // ── NSG ────────────────────────────────────────────────────────────────
    public async Task<List<string>> GetNsgsInResourceGroupAsync(TenantConfig tenant, string resourceGroup)
    {
        var client = CreateClient(tenant);
        var subscription = await client.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().GetAsync(resourceGroup);
        var nsgs = new List<string>();
        await foreach (var nsg in rg.Value.GetNetworkSecurityGroups().GetAllAsync())
        {
            nsgs.Add(nsg.Data.Name);
        }
        return nsgs;
    }

    public async Task<List<NsgRuleInfo>> GetNsgRulesAsync(TenantConfig tenant, string resourceGroup, string nsgName)
    {
        var client = CreateClient(tenant);
        var subscription = await client.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().GetAsync(resourceGroup);
        var nsg = await rg.Value.GetNetworkSecurityGroups().GetAsync(nsgName);
        var rules = new List<NsgRuleInfo>();

        foreach (var rule in nsg.Value.Data.SecurityRules)
        {
            rules.Add(new NsgRuleInfo
            {
                Name = rule.Name ?? "",
                Priority = rule.Priority ?? 0,
                Direction = rule.Direction?.ToString() ?? "",
                Access = rule.Access?.ToString() ?? "",
                Protocol = rule.Protocol?.ToString() ?? "",
                DestinationPortRange = rule.DestinationPortRange ?? string.Join(",", rule.DestinationPortRanges),
                SourceAddressPrefixes = rule.SourceAddressPrefix ?? string.Join(", ", rule.SourceAddressPrefixes),
                Description = rule.Description ?? ""
            });
        }

        return rules.OrderBy(r => r.Priority).ToList();
    }

    public async Task<string> AddOrUpdateNsgRuleAsync(TenantConfig tenant, AddNsgRuleRequest request)
    {
        try
        {
            var client = CreateClient(tenant);
            var subscription = await client.GetDefaultSubscriptionAsync();
            var rg = await subscription.GetResourceGroups().GetAsync(request.ResourceGroup);
            var nsgResource = await rg.Value.GetNetworkSecurityGroups().GetAsync(request.NsgName);

            // Determine rule parameters by protocol
            string ruleName, port, description;
            int priority;
            if (request.Protocol == "MSSQL")
            {
                ruleName = "AllowMSSQLInbound";
                port = "1433";
                description = "Allow MSSQL from multiple IPs";
                priority = 110;
            }
            else // RDP default
            {
                ruleName = "AllowRDPInbound";
                port = "3389";
                description = "Allow RDP from multiple IPs";
                priority = 234;
            }

            // Check if rule already exists
            SecurityRuleData? existingRule = null;
            try
            {
                var existingRuleResource = await nsgResource.Value.GetSecurityRules().GetAsync(ruleName);
                existingRule = existingRuleResource.Value.Data;
            }
            catch { /* rule does not exist */ }

            var newIpList = new List<string> { request.SourceIp };

            if (existingRule != null)
            {
                // Merge IPs
                var existing = existingRule.SourceAddressPrefixes.ToList();
                if (!existing.Contains(request.SourceIp))
                    existing.Add(request.SourceIp);
                newIpList = existing;

                existingRule.SourceAddressPrefix = null;
                existingRule.SourceAddressPrefixes.Clear();
                foreach (var ip in newIpList)
                    existingRule.SourceAddressPrefixes.Add(ip);

                await nsgResource.Value.GetSecurityRules().CreateOrUpdateAsync(
                    WaitUntil.Completed, ruleName, existingRule);
            }
            else
            {
                var ruleData = new SecurityRuleData
                {
                    Name = ruleName,
                    Priority = priority,
                    Direction = SecurityRuleDirection.Inbound,
                    Access = SecurityRuleAccess.Allow,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = request.SourceIp,
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = port,
                    Description = description
                };

                await nsgResource.Value.GetSecurityRules().CreateOrUpdateAsync(
                    WaitUntil.Completed, ruleName, ruleData);
            }

            return "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding NSG rule");
            return $"error: {ex.Message}";
        }
    }
}
