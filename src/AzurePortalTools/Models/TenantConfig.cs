namespace AzurePortalTools.Models;

public class TenantConfig
{
    public string DisplayName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class AppAuthConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VmConfig
{
    public string VmName { get; set; } = string.Empty;
    public string NsgName { get; set; } = string.Empty;
    public List<string> AllowedProtocols { get; set; } = new();
}

public class AppUserConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin"; // Admin | Operator
    public string? RedirectPage { get; set; }    // e.g. "/MlpsDashboard"

    // Fixed resources for Operator users
    public string? TenantDisplayName { get; set; }
    public string? ResourceGroup { get; set; }

    // New: list of VMs (preferred format)
    public List<VmConfig> Vms { get; set; } = new();

    // Legacy single-VM fields (kept for backward compatibility)
    public string? VmName { get; set; }
    public string? NsgName { get; set; }
    public List<string> AllowedProtocols { get; set; } = new();

    /// <summary>Returns the effective list of VM configs, supporting both old and new format.</summary>
    public List<VmConfig> GetVmConfigs()
    {
        if (Vms.Any()) return Vms;
        if (!string.IsNullOrEmpty(VmName))
            return new() { new() { VmName = VmName, NsgName = NsgName ?? "", AllowedProtocols = AllowedProtocols } };
        return new();
    }
}
