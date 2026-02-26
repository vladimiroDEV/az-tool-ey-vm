namespace AzurePortalTools.Models;

public class VmInfo
{
    public string Name { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string PowerState { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class NsgRuleInfo
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string SourceAddressPrefixes { get; set; } = string.Empty;
    public string DestinationPortRange { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AddNsgRuleRequest
{
    public string ResourceGroup { get; set; } = string.Empty;
    public string NsgName { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
    public List<string> Protocols { get; set; } = new(); // RDP, MSSQL, etc.
}

/// <summary>
/// Defines a preset NSG rule template (name, port, priority).
/// </summary>
public class NsgRulePreset
{
    public string Key { get; set; } = string.Empty;       // e.g. "RDP"
    public string DisplayName { get; set; } = string.Empty; // e.g. "RDP (3389)"
    public string RuleName { get; set; } = string.Empty;   // e.g. "AllowRDPInbound"
    public string Port { get; set; } = string.Empty;       // e.g. "3389"
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;       // emoji

    /// <summary>All available presets.</summary>
    public static List<NsgRulePreset> All => new()
    {
        new() { Key = "RDP",   DisplayName = "RDP (3389)",     RuleName = "AllowRDPInbound",   Port = "3389", Priority = 234, Description = "Allow RDP from specific IPs",   Icon = "🖥️" },
        new() { Key = "MSSQL", DisplayName = "MS SQL (1433)",  RuleName = "AllowMSSQLInbound", Port = "1433", Priority = 110, Description = "Allow MSSQL from specific IPs", Icon = "🗄️" },
    };
}

/// <summary>
/// Status of a preset rule in the current NSG (exists? which IPs?).
/// </summary>
public class NsgRulePresetStatus
{
    public NsgRulePreset Preset { get; set; } = new();
    public bool Exists { get; set; }
    public bool IpAlreadyPresent { get; set; }
    public List<string> CurrentIps { get; set; } = new();
}
