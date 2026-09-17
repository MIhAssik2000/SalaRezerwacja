using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalaRezerwacja.Models;
using SalaRezerwacja.Services;

namespace SalaRezerwacja.Controllers;

/// <summary>
/// Logowanie kontem uczelnianym (wymaganie 1).
/// Po pomyślnej weryfikacji w systemie uczelni konto jest zakładane lokalnie
/// (bez hasła własnego) i użytkownik dostaje sesję Identity – reszta aplikacji
/// nie musi wiedzieć, skąd przyszło logowanie.
/// </summary>
public class UniversityAuthController : Controller
{
    private readonly IUniversityAuthProvider _provider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UniversityAuthController(
        IUniversityAuthProvider provider,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _provider = provider;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.ProviderName = _provider.ProviderName;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string login, string password, string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.ProviderName = _provider.ProviderName;

        var info = await _provider.AuthenticateAsync(login, password);
        if (info == null)
        {
            ModelState.AddModelError(string.Empty, "Nieprawidłowe dane logowania do systemu uczelnianego.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(info.Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = info.Email,
                Email = info.Email,
                EmailConfirmed = true,
                FirstName = info.FirstName,
                LastName = info.LastName,
                UniversityId = info.UniversityId,
                IsUniversityAccount = true
            };

            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                foreach (var error in created.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View();
            }

            await _userManager.AddToRoleAsync(user, info.Role ?? Roles.Student);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }
}
