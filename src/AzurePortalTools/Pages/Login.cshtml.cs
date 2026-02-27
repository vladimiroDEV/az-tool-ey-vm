using System.Security.Claims;
using AzurePortalTools.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AzurePortalTools.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly List<AppUserConfig> _users;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public LoginModel(IOptions<List<AppUserConfig>> users)
    {
        _users = users.Value;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Username, Username, StringComparison.OrdinalIgnoreCase)
            && u.Password == Password);

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Redirect based on role
            if (!string.IsNullOrEmpty(user.RedirectPage))
                return Redirect(user.RedirectPage);

            return RedirectToPage("/Index");
        }

        ErrorMessage = "Credenziali non valide.";
        return Page();
    }
}
