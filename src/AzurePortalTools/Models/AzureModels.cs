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
    public string Protocol { get; set; } = "RDP"; // RDP | MSSQL
}
