using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

public class IndexModel : PageModel
{
    public List<TenantConfig> Tenants { get; set; } = new();

    public IndexModel(IOptions<List<TenantConfig>> tenants)
    {
        Tenants = tenants.Value;
    }

    public void OnGet() { }
}
