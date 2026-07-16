using InventoryManagement.Infrastructure;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Web.Components;
using InventoryManagement.Web.Options;
using InventoryManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Keep disconnected-circuit memory bounded on the 256 MB host (public catalog is InteractiveServer).
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DisconnectedCircuitMaxRetained = 20;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(1);
});

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Business/legal details for the public policy pages + social contact buttons.
builder.Services.Configure<BusinessInfo>(builder.Configuration.GetSection(BusinessInfo.SectionName));
builder.Services.Configure<DealerAuth>(builder.Configuration.GetSection(DealerAuth.SectionName));

// Simple cookie auth (single dealer) — no ASP.NET Identity.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Admin-editable site content (settings, pages, branches) with a shared in-memory cache.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<SiteConfigService>();

var app = builder.Build();

// Create the schema + seed data on startup.
// Migrations are SQL Server-specific, so:
//   - SQL Server (production): apply the real migrations.
//   - SQLite (local dev): build the schema straight from the model (no migrations).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    // Seed admin-editable content + the admin credential from appsettings the first time only
    // (never overwrites edits — after first boot the config credential is inert).
    var biz = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<BusinessInfo>>().Value;
    var auth = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DealerAuth>>().Value;
    await SeedContent.EnsureSeededAsync(db, biz, auth);

    // Warn if the stored credential is still a well-known placeholder.
    var storedHash = (await db.SiteSettings.FindAsync(AuthKeys.PasswordHash))?.Value;
    if (storedHash is not null &&
        (PasswordHasher.Verify("change-me", storedHash) || PasswordHasher.Verify("CHANGE-THIS-STRONG-PASSWORD", storedHash)))
        app.Logger.LogWarning("The admin password is still the default placeholder. Change it at /account/change-password.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Basic security headers on every response.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Uploaded part photos are immutable (GUID filenames) — cache aggressively.
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"
});
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Health check for uptime monitors (also keeps the app pool warm).
app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok("healthy") : Results.StatusCode(503));

// Logout endpoint — clears the auth cookie and returns to the public catalog.
app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
