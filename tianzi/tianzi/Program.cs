using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tianzi.Data;
using tianzi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "tianzi");

    Directory.CreateDirectory(dbDir);

    var dbPath = Path.Combine(dbDir, "tianzi.db");
    options.UseSqlite($"Data Source={dbPath}");
});
// Cookie auth for admin area
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("role", "admin"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Files/NotFound");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ensure DB exists + apply migrations + seed admin user
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Applies migrations automatically at startup.
    // If you want manual-only migrations, remove this and run `dotnet ef database update`.
    await db.Database.MigrateAsync();

    var adminUsername = app.Configuration["Admin:Username"] ?? "admin";
    var adminPassword = app.Configuration["Admin:Password"] ?? "admin123";

    var exists = await db.AdminUsers.AnyAsync(a => a.Username == adminUsername);
    if (!exists)
    {
        var admin = new AdminUser { Username = adminUsername };
        var hasher = new PasswordHasher<AdminUser>();
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Files}/{action=Index}/{id?}");

app.Run();
