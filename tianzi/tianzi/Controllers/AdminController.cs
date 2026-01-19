using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tianzi.Data;
using tianzi.Models;

namespace tianzi.Controllers;
[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("/admin/login")]
    public IActionResult Login() => View();

    [AllowAnonymous]
    [HttpPost("/admin/login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        username = (username ?? "").Trim();

        var admin = await _db.AdminUsers.SingleOrDefaultAsync(a => a.Username == username);
        if (admin == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        var hasher = new PasswordHasher<AdminUser>();
        var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username),
            new("role", "admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet("/admin")]
    public async Task<IActionResult> Dashboard()
    {
        var files = await _db.SharedFiles
            .AsNoTracking()
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync();

        return View(files);
    }

    [HttpPost("/admin/delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var f = await _db.SharedFiles.FindAsync(id);
        if (f == null) return NotFound();

        _db.SharedFiles.Remove(f);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost("/admin/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/admin/login");
    }
}
