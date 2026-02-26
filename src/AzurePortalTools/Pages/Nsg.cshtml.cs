using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace AzurePortalTools.Pages;

public class NsgModel : PageModel
{
    private readonly AzureService _azure;
    private readonly List<TenantConfig> _tenants;
    private readonly IHttpClientFactory _httpClientFactory;

    public TenantConfig? SelectedTenant { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string TenantId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ResourceGroup { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string NsgName { get; set; } = string.Empty;

    [BindProperty]
    public string SourceIp { get; set; } = string.Empty;

    [BindProperty]
    public string Protocol { get; set; } = "RDP";

    public List<string> ResourceGroups { get; set; } = new();
    public List<string> Nsgs { get; set; } = new();
    public List<NsgRuleInfo> Rules { get; set; } = new();
    public string PublicIp { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? MessageClass { get; set; }

    public NsgModel(AzureService azure, IOptions<List<TenantConfig>> tenants, IHttpClientFactory httpClientFactory)
    {
        _azure = azure;
        _tenants = tenants.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);

        if (!string.IsNullOrEmpty(ResourceGroup))
            Nsgs = await _azure.GetNsgsInResourceGroupAsync(SelectedTenant, ResourceGroup);

        if (!string.IsNullOrEmpty(ResourceGroup) && !string.IsNullOrEmpty(NsgName))
            Rules = await _azure.GetNsgRulesAsync(SelectedTenant, ResourceGroup, NsgName);

        PublicIp = await GetPublicIpAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddRuleAsync()
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        var request = new AddNsgRuleRequest
        {
            ResourceGroup = ResourceGroup,
            NsgName = NsgName,
            SourceIp = SourceIp,
            Protocol = Protocol
        };

        var result = await _azure.AddOrUpdateNsgRuleAsync(SelectedTenant, request);
        if (result == "ok")
        {
            Message = $"Regola {Protocol} aggiunta/aggiornata con IP {SourceIp}.";
            MessageClass = "alert-success";
        }
        else
        {
            Message = $"Errore: {result}";
            MessageClass = "alert-danger";
        }

        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);
        Nsgs = await _azure.GetNsgsInResourceGroupAsync(SelectedTenant, ResourceGroup);
        Rules = await _azure.GetNsgRulesAsync(SelectedTenant, ResourceGroup, NsgName);
        PublicIp = await GetPublicIpAsync();
        return Page();
    }

    private async Task<string> GetPublicIpAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            return (await client.GetStringAsync("https://ifconfig.me")).Trim();
        }
        catch
        {
            return "N/A";
        }
    }
}
