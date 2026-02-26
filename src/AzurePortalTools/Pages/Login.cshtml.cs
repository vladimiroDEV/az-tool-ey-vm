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
    private readonly AppAuthConfig _auth;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public LoginModel(IOptions<AppAuthConfig> auth)
    {
        _auth = auth.Value;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Username == _auth.Username && Password == _auth.Password)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Username),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            return RedirectToPage("/Index");
        }

        ErrorMessage = "Credenziali non valide.";
        return Page();
    }
}
