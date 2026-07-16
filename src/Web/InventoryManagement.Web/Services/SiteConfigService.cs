using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Web.Options;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Web.Services;

/// <summary>Canonical setting keys (stored in the SiteSettings table).</summary>
public static class SettingKeys
{
    public const string BrandName = "BrandName";
    public const string LegalName = "LegalName";
    public const string SupportEmail = "SupportEmail";
    public const string SupportPhone = "SupportPhone";
    public const string BusinessHours = "BusinessHours";
    public const string GrievanceOfficerName = "GrievanceOfficerName";
    public const string RefundWindowDays = "RefundWindowDays";
    public const string ProcessingTimeText = "ProcessingTimeText";
    public const string Jurisdiction = "Jurisdiction";
    public const string UdyamRegistrationNumber = "UdyamRegistrationNumber";
    public const string WebsiteUrl = "WebsiteUrl";
    public const string LastUpdated = "LastUpdated";
    public const string LogoUrl = "LogoUrl";
    public const string Theme = "Theme";
    public const string Template = "Template";

    public static readonly string[] All =
    [
        BrandName, LegalName, SupportEmail, SupportPhone, BusinessHours, GrievanceOfficerName,
        RefundWindowDays, ProcessingTimeText, Jurisdiction, UdyamRegistrationNumber,
        WebsiteUrl, LastUpdated, LogoUrl, Theme, Template
    ];
}

/// <summary>Immutable snapshot of the site's admin-editable configuration.</summary>
public class SiteConfig
{
    public required Dictionary<string, string> Settings { get; init; }
    public required List<BranchDto> Branches { get; init; }

    public BranchDto? PrimaryBranch => Branches.FirstOrDefault(b => b.IsPrimary) ?? Branches.FirstOrDefault();

    public string Get(string key, string fallback = "") =>
        Settings.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    public string BrandName => Get(SettingKeys.BrandName, "AutoNex");
    public string LegalName => Get(SettingKeys.LegalName, BrandName);
    public string SupportEmail => Get(SettingKeys.SupportEmail);
    public string SupportPhone => Get(SettingKeys.SupportPhone);
    public string BusinessHours => Get(SettingKeys.BusinessHours);
    public string GrievanceOfficerName => Get(SettingKeys.GrievanceOfficerName);
    public string LogoUrl => Get(SettingKeys.LogoUrl);
    public string Theme => Get(SettingKeys.Theme, Theming.ThemeCatalog.DefaultKey);
    public string Template => Get(SettingKeys.Template, Theming.TemplateCatalog.DefaultKey);
    public string LastUpdated => Get(SettingKeys.LastUpdated);
    /// <summary>Primary branch address, falling back to any stored address.</summary>
    public string Address => PrimaryBranch?.Address ?? Get("Address");
}

/// <summary>
/// Cached facade over ISiteContentRepository. All public-facing components read through this,
/// so DB hits are bounded; every admin save calls Invalidate().
/// </summary>
public class SiteConfigService(ISiteContentRepository repo, IMemoryCache cache, IOptions<BusinessInfo> fallback)
{
    private const string CacheKey = "site-config";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()   // raw HTML in markdown renders as text — no script injection
        .Build();

    public async Task<SiteConfig> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteConfig? cached) && cached is not null)
            return cached;

        // Serialize DB fetches on cache miss: multiple components render concurrently during
        // static SSR and all share the same scoped DbContext. Without this gate, they all call
        // GetAllSettingsAsync simultaneously and hit EF's "second operation on same context" error.
        await _lock.WaitAsync(ct);
        try
        {
            // Re-check after acquiring — another caller may have populated it while we waited.
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var settings = await repo.GetAllSettingsAsync(ct);
            // Credential keys must never reach public-facing snapshots or {{token}} replacement.
            foreach (var key in settings.Keys.Where(k => k.StartsWith(AuthKeys.Prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                settings.Remove(key);
            ApplyFallbacks(settings, fallback.Value);
            var branches = await repo.GetBranchesAsync(AppConstants.DefaultDealerId, ct);

            var config = new SiteConfig { Settings = settings, Branches = branches };
            cache.Set(CacheKey, config, Ttl);
            return config;
        }
        finally
        {
            _lock.Release();
        }
    }


    public void Invalidate() => cache.Remove(CacheKey);

    /// <summary>Replace {{Token}} placeholders with live settings, then render markdown to HTML.</summary>
    public string RenderMarkdown(string markdown, SiteConfig config)
        => Markdown.ToHtml(ReplaceTokens(markdown, config), Pipeline);

    public static string ReplaceTokens(string text, SiteConfig config)
    {
        foreach (var (key, value) in config.Settings)
            text = text.Replace("{{" + key + "}}", value, StringComparison.OrdinalIgnoreCase);
        // Address comes from the primary branch, not a setting.
        text = text.Replace("{{Address}}", config.Address, StringComparison.OrdinalIgnoreCase);
        return text;
    }

    /// <summary>Any missing/blank setting falls back to appsettings BusinessInfo — a partially
    /// seeded DB can never blank the site.</summary>
    private static void ApplyFallbacks(Dictionary<string, string> s, BusinessInfo b)
    {
        void Fb(string key, string value)
        {
            if (!s.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) s[key] = value;
        }
        Fb(SettingKeys.BrandName, b.BrandName);
        Fb(SettingKeys.LegalName, b.LegalName);
        Fb(SettingKeys.SupportEmail, b.SupportEmail);
        Fb(SettingKeys.SupportPhone, b.SupportPhone);
        Fb(SettingKeys.BusinessHours, b.BusinessHours);
        Fb(SettingKeys.GrievanceOfficerName, b.GrievanceOfficerName);
        Fb(SettingKeys.RefundWindowDays, b.RefundWindowDays.ToString());
        Fb(SettingKeys.ProcessingTimeText, b.ProcessingTimeText);
        Fb(SettingKeys.Jurisdiction, b.Jurisdiction);
        Fb(SettingKeys.UdyamRegistrationNumber, b.UdyamRegistrationNumber);
        Fb(SettingKeys.WebsiteUrl, b.WebsiteUrl);
        Fb(SettingKeys.LastUpdated, b.LastUpdated);
        Fb("Address", b.Address);   // fallback address when no branch exists yet
    }
}
