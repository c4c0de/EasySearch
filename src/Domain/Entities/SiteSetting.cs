namespace InventoryManagement.Domain.Entities;

/// <summary>Key-value store for admin-editable site configuration (branding, contact info, theme, logo).</summary>
public class SiteSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
