using AzurePortalTools.Models;
using AzurePortalTools.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

public class IndexModel : PageModel
{
    public List<TenantConfig> Tenants { get; set; } = new();
    private readonly List<AppUserConfig> _users;

    public IndexModel(IOptions<List<TenantConfig>> tenants, IOptions<List<AppUserConfig>> users)
    {
        Tenants = tenants.Value;
        _users = users.Value;
    }

    public IActionResult OnGet()
    {
        // If Operator user, redirect to their dedicated dashboard
        if (User.IsInRole("Operator"))
        {
            var username = User.Identity?.Name;
            var userConfig = _users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (userConfig?.RedirectPage != null)
                return Redirect(userConfig.RedirectPage);
        }

        return Page();
    }
}
