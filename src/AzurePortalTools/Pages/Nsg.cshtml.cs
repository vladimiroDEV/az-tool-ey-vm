using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

public class NsgModel : PageModel
{
    private readonly AzureService _azure;
    private readonly List<TenantConfig> _tenants;

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
    public List<string> Protocols { get; set; } = new();

    public List<string> ResourceGroups { get; set; } = new();
    public List<string> Nsgs { get; set; } = new();
    public List<NsgRuleInfo> Rules { get; set; } = new();
    public List<NsgRulePresetStatus> PresetStatuses { get; set; } = new();
    public string? Message { get; set; }
    public string? MessageClass { get; set; }

    public NsgModel(AzureService azure, IOptions<List<TenantConfig>> tenants)
    {
        _azure = azure;
        _tenants = tenants.Value;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);

        if (!string.IsNullOrEmpty(ResourceGroup))
            Nsgs = await _azure.GetNsgsInResourceGroupAsync(SelectedTenant, ResourceGroup);

        if (!string.IsNullOrEmpty(ResourceGroup) && !string.IsNullOrEmpty(NsgName))
        {
            Rules = await _azure.GetNsgRulesAsync(SelectedTenant, ResourceGroup, NsgName);
            PresetStatuses = await _azure.GetPresetStatusAsync(SelectedTenant, ResourceGroup, NsgName, string.Empty);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddRuleAsync()
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        if (Protocols == null || !Protocols.Any())
        {
            Message = "Seleziona almeno un servizio (RDP, MS SQL, ...).";
            MessageClass = "alert-warning";
        }
        else
        {
            var request = new AddNsgRuleRequest
            {
                ResourceGroup = ResourceGroup,
                NsgName = NsgName,
                SourceIp = SourceIp,
                Protocols = Protocols
            };

            var result = await _azure.AddOrUpdateNsgRuleAsync(SelectedTenant, request);
            if (result.StartsWith("ok|"))
            {
                var details = result.Substring(3);
                Message = $"Regole applicate con IP {SourceIp}: {details}";
                MessageClass = "alert-success";
            }
            else
            {
                Message = $"Errore: {result}";
                MessageClass = "alert-danger";
            }
        }

        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);
        Nsgs = await _azure.GetNsgsInResourceGroupAsync(SelectedTenant, ResourceGroup);
        Rules = await _azure.GetNsgRulesAsync(SelectedTenant, ResourceGroup, NsgName);
        PresetStatuses = await _azure.GetPresetStatusAsync(SelectedTenant, ResourceGroup, NsgName, SourceIp);
        return Page();
    }

}
