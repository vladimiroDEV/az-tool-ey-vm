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

            var results = new List<string>();

            foreach (var protocolKey in request.Protocols)
            {
                var preset = NsgRulePreset.All.FirstOrDefault(p => p.Key == protocolKey);
                if (preset == null)
                {
                    results.Add($"{protocolKey}: preset sconosciuto");
                    continue;
                }

                try
                {
                    await ApplySingleRuleAsync(nsgResource.Value, preset, request.SourceIp);
                    results.Add($"{preset.DisplayName}: ✅");
                }
                catch (Exception ex)
                {
                    results.Add($"{preset.DisplayName}: ❌ {ex.Message}");
                }
            }

            var hasError = results.Any(r => r.Contains("❌"));
            return hasError ? string.Join(" | ", results) : "ok|" + string.Join(" | ", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding NSG rules");
            return $"error: {ex.Message}";
        }
    }

    private async Task ApplySingleRuleAsync(NetworkSecurityGroupResource nsgResource, NsgRulePreset preset, string sourceIp)
    {
        // Check if rule already exists
        SecurityRuleData? existingRule = null;
        try
        {
            var existingRuleResource = await nsgResource.GetSecurityRules().GetAsync(preset.RuleName);
            existingRule = existingRuleResource.Value.Data;
        }
        catch { /* rule does not exist */ }

        if (existingRule != null)
        {
            // Merge IPs
            var existing = existingRule.SourceAddressPrefixes.ToList();
            if (!string.IsNullOrEmpty(existingRule.SourceAddressPrefix))
            {
                if (!existing.Contains(existingRule.SourceAddressPrefix))
                    existing.Add(existingRule.SourceAddressPrefix);
            }

            if (!existing.Contains(sourceIp))
                existing.Add(sourceIp);

            existingRule.SourceAddressPrefix = null;
            existingRule.SourceAddressPrefixes.Clear();
            foreach (var ip in existing)
                existingRule.SourceAddressPrefixes.Add(ip);

            await nsgResource.GetSecurityRules().CreateOrUpdateAsync(
                WaitUntil.Completed, preset.RuleName, existingRule);
        }
        else
        {
            var ruleData = new SecurityRuleData
            {
                Name = preset.RuleName,
                Priority = preset.Priority,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = sourceIp,
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = preset.Port,
                Description = preset.Description
            };

            await nsgResource.GetSecurityRules().CreateOrUpdateAsync(
                WaitUntil.Completed, preset.RuleName, ruleData);
        }
    }

    /// <summary>
    /// Returns the status of each preset rule in the NSG (exists, IPs, etc.).
    /// </summary>
    public async Task<List<NsgRulePresetStatus>> GetPresetStatusAsync(TenantConfig tenant, string resourceGroup, string nsgName, string currentIp)
    {
        var rules = await GetNsgRulesAsync(tenant, resourceGroup, nsgName);
        var statuses = new List<NsgRulePresetStatus>();

        foreach (var preset in NsgRulePreset.All)
        {
            var matchingRule = rules.FirstOrDefault(r =>
                string.Equals(r.Name, preset.RuleName, StringComparison.OrdinalIgnoreCase));

            var currentIps = new List<string>();
            if (matchingRule != null)
            {
                currentIps = matchingRule.SourceAddressPrefixes
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim())
                    .ToList();
            }

            statuses.Add(new NsgRulePresetStatus
            {
                Preset = preset,
                Exists = matchingRule != null,
                IpAlreadyPresent = currentIps.Contains(currentIp),
                CurrentIps = currentIps
            });
        }

        return statuses;
    }
}
