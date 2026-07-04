namespace InventoryManagement.Web.Theming;

/// <summary>
/// Preset themes for the PUBLIC site (catalog, legal pages, login). The admin panel keeps its
/// own dark styling from app.css. Each preset overrides the CSS variables that app.css scopes
/// to .public-page/.login-page/.legal-page.
/// </summary>
public record ThemePreset(string Key, string Name, string Description, Dictionary<string, string> Variables)
{
    /// <summary>Representative swatch colors for the admin theme picker.</summary>
    public string SwatchBg => Variables.GetValueOrDefault("--color-bg", "#f8fafc");
    public string SwatchPrimary => Variables.GetValueOrDefault("--color-primary", "#4f46e5");
    public string SwatchSurface => Variables.GetValueOrDefault("--color-surface", "#ffffff");
}

public static class ThemeCatalog
{
    public const string DefaultKey = "clean-light";

    public static readonly IReadOnlyList<ThemePreset> Presets =
    [
        // Matches the current app.css public defaults — selecting it changes nothing.
        new("clean-light", "Clean Light", "Bright, minimal indigo — the current look.", new()
        {
            ["--color-bg"] = "#f8fafc",
            ["--color-surface"] = "#ffffff",
            ["--color-surface-hover"] = "#f1f5f9",
            ["--color-border"] = "#e2e8f0",
            ["--color-primary"] = "#4f46e5",
            ["--color-primary-hover"] = "#4338ca",
            ["--color-primary-glow"] = "rgba(79, 70, 229, 0.1)",
            ["--color-text"] = "#0f172a",
            ["--color-muted"] = "#64748b",
        }),
        new("obsidian", "Obsidian", "Dark, high-contrast storefront.", new()
        {
            ["--color-bg"] = "#07090e",
            ["--color-surface"] = "#0f131a",
            ["--color-surface-hover"] = "#161c27",
            ["--color-border"] = "#1d2433",
            ["--color-primary"] = "#6366f1",
            ["--color-primary-hover"] = "#818cf8",
            ["--color-primary-glow"] = "rgba(99, 102, 241, 0.15)",
            ["--color-text"] = "#f3f4f6",
            ["--color-muted"] = "#9ca3af",
        }),
        new("emerald", "Emerald", "Fresh green — workshop energy.", new()
        {
            ["--color-bg"] = "#f6faf7",
            ["--color-surface"] = "#ffffff",
            ["--color-surface-hover"] = "#eef7f0",
            ["--color-border"] = "#d7e8dc",
            ["--color-primary"] = "#059669",
            ["--color-primary-hover"] = "#047857",
            ["--color-primary-glow"] = "rgba(5, 150, 105, 0.12)",
            ["--color-text"] = "#122117",
            ["--color-muted"] = "#5b7365",
        }),
        new("royal", "Royal", "Deep navy & gold accents.", new()
        {
            ["--color-bg"] = "#0d1526",
            ["--color-surface"] = "#152036",
            ["--color-surface-hover"] = "#1c2a47",
            ["--color-border"] = "#243657",
            ["--color-primary"] = "#f59e0b",
            ["--color-primary-hover"] = "#d97706",
            ["--color-primary-glow"] = "rgba(245, 158, 11, 0.15)",
            ["--color-text"] = "#eef2ff",
            ["--color-muted"] = "#93a3c4",
        }),
        new("sunset", "Sunset", "Warm orange-red, bold and friendly.", new()
        {
            ["--color-bg"] = "#fff8f4",
            ["--color-surface"] = "#ffffff",
            ["--color-surface-hover"] = "#fff1e9",
            ["--color-border"] = "#f4ddd0",
            ["--color-primary"] = "#ea580c",
            ["--color-primary-hover"] = "#c2410c",
            ["--color-primary-glow"] = "rgba(234, 88, 12, 0.12)",
            ["--color-text"] = "#27150c",
            ["--color-muted"] = "#8a6f60",
        }),
    ];

    public static ThemePreset Get(string? key)
        => Presets.FirstOrDefault(p => p.Key == key) ?? Presets[0];

    /// <summary>CSS rule that overrides the public-scope variables for the chosen theme.</summary>
    public static string ToCss(ThemePreset preset)
    {
        var vars = string.Join(" ", preset.Variables.Select(kv => $"{kv.Key}: {kv.Value};"));
        return $".public-page, .login-page, .legal-page {{ {vars} }}";
    }
}
