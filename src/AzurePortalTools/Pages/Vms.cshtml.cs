using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

public class VmsModel : PageModel
{
    private readonly AzureService _azure;
    private readonly List<TenantConfig> _tenants;

    public TenantConfig? SelectedTenant { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string TenantId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ResourceGroup { get; set; } = string.Empty;

    public List<string> ResourceGroups { get; set; } = new();
    public List<VmInfo> Vms { get; set; } = new();
    public string? Message { get; set; }
    public string? MessageClass { get; set; }

    public VmsModel(AzureService azure, IOptions<List<TenantConfig>> tenants)
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
            Vms = await _azure.GetVmsInResourceGroupAsync(SelectedTenant, ResourceGroup);

        return Page();
    }

    public async Task<IActionResult> OnPostStartAsync(string vmName)
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        var result = await _azure.StartVmAsync(SelectedTenant, ResourceGroup, vmName);
        SetMessage(result, vmName, "avviata");
        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);
        Vms = await _azure.GetVmsInResourceGroupAsync(SelectedTenant, ResourceGroup);
        return Page();
    }

    public async Task<IActionResult> OnPostStopAsync(string vmName)
    {
        SelectedTenant = _tenants.FirstOrDefault(t => t.TenantId == TenantId);
        if (SelectedTenant == null) return RedirectToPage("/Index");

        var result = await _azure.StopVmAsync(SelectedTenant, ResourceGroup, vmName);
        SetMessage(result, vmName, "fermata");
        ResourceGroups = await _azure.GetResourceGroupsAsync(SelectedTenant);
        Vms = await _azure.GetVmsInResourceGroupAsync(SelectedTenant, ResourceGroup);
        return Page();
    }

    private void SetMessage(string result, string vmName, string action)
    {
        if (result.StartsWith("error"))
        {
            Message = $"Errore su {vmName}: {result}";
            MessageClass = "alert-danger";
        }
        else
        {
            Message = $"VM {vmName} {action} con successo.";
            MessageClass = "alert-success";
        }
    }
}
