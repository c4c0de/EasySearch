namespace InventoryManagement.Web.Theming;

public record TemplatePreset(string Key, string Name, string Description);

public static class TemplateCatalog
{
    public const string DefaultKey = "classic";

    public static readonly IReadOnlyList<TemplatePreset> Presets =
    [
        new("classic", "Classic", "Traditional sidebar filter with grid cards. Compact, informative, and familiar."),
        new("showroom", "Showroom", "Modern hero layout with horizontal filter chips and image-first cards.")
    ];

    public static TemplatePreset Get(string? key)
        => Presets.FirstOrDefault(p => p.Key == key) ?? Presets[0];
}
