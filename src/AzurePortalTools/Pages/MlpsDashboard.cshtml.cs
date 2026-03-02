using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

public class MlpsDashboardModel : PageModel
{
    private readonly AzureService _azure;
    private readonly List<TenantConfig> _tenants;
    private readonly List<AppUserConfig> _users;

    // Fixed config loaded from appsettings
    public AppUserConfig? UserConfig { get; private set; }
    public TenantConfig? Tenant { get; private set; }

    // VM state
    public VmInfo? Vm { get; set; }

    // NSG preset statuses
    public List<NsgRulePresetStatus> PresetStatuses { get; set; } = new();
    public List<NsgRuleInfo> NsgRules { get; set; } = new();

    [BindProperty]
    public string SourceIp { get; set; } = string.Empty;

    [BindProperty]
    public List<string> Protocols { get; set; } = new();

    public string? Message { get; set; }
    public string? MessageClass { get; set; }

    [TempData]
    public string? ToastMessage { get; set; }
    [TempData]
    public string? ToastClass { get; set; }

    public MlpsDashboardModel(AzureService azure, IOptions<List<TenantConfig>> tenants, IOptions<List<AppUserConfig>> users)
    {
        _azure = azure;
        _tenants = tenants.Value;
        _users = users.Value;
    }

    private bool LoadConfig()
    {
        var username = User.Identity?.Name;
        UserConfig = _users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.Role == "Operator");
        if (UserConfig == null) return false;

        Tenant = _tenants.FirstOrDefault(t =>
            string.Equals(t.DisplayName, UserConfig.TenantDisplayName, StringComparison.OrdinalIgnoreCase));
        return Tenant != null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!LoadConfig()) return RedirectToPage("/Index");
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostStartVmAsync()
    {
        if (!LoadConfig()) return RedirectToPage("/Index");

        var result = await _azure.StartVmAsync(Tenant!, UserConfig!.ResourceGroup!, UserConfig.VmName!);
        if (result == "started")
        {
            Message = $"VM {UserConfig.VmName} avviata con successo.";
            MessageClass = "alert-success";
        }
        else
        {
            Message = $"Errore avvio: {result}";
            MessageClass = "alert-danger";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostStopVmAsync()
    {
        if (!LoadConfig()) return RedirectToPage("/Index");

        var result = await _azure.StopVmAsync(Tenant!, UserConfig!.ResourceGroup!, UserConfig.VmName!);
        if (result == "deallocated")
        {
            Message = $"VM {UserConfig.VmName} fermata (deallocated).";
            MessageClass = "alert-success";
        }
        else
        {
            Message = $"Errore stop: {result}";
            MessageClass = "alert-danger";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApplyRulesAsync()
    {
        if (!LoadConfig()) return RedirectToPage("/Index");

        if (Protocols == null || !Protocols.Any())
        {
            TempData["ToastMessage"] = "Seleziona almeno un servizio.";
            TempData["ToastClass"] = "bg-warning text-dark";
        }
        else
        {
            var request = new AddNsgRuleRequest
            {
                ResourceGroup = UserConfig!.ResourceGroup!,
                NsgName = UserConfig.NsgName!,
                SourceIp = SourceIp,
                Protocols = Protocols
            };

            var result = await _azure.AddOrUpdateNsgRuleAsync(Tenant!, request);
            if (result.StartsWith("ok|"))
            {
                TempData["ToastMessage"] = $"✅ Regole applicate con IP {SourceIp}";
                TempData["ToastClass"] = "bg-success";
            }
            else
            {
                TempData["ToastMessage"] = $"❌ Errore: {result}";
                TempData["ToastClass"] = "bg-danger";
            }
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        if (Tenant == null || UserConfig == null) return;

        // Load VM status
        try
        {
            var vms = await _azure.GetVmsInResourceGroupAsync(Tenant, UserConfig.ResourceGroup!);
            Vm = vms.FirstOrDefault(v =>
                string.Equals(v.Name, UserConfig.VmName, StringComparison.OrdinalIgnoreCase));
        }
        catch { /* VM not found */ }
    }
}
