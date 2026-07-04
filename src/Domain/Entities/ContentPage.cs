namespace InventoryManagement.Domain.Entities;

/// <summary>Admin-editable markdown page (policy pages, contact intro).</summary>
public class ContentPage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;    // e.g. "terms", "privacy", "contact-intro"
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
