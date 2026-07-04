namespace InventoryManagement.Domain.Entities;

/// <summary>A physical branch/location shown as a card on the public Contact page.</summary>
public class Branch
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public string Name { get; set; } = string.Empty;     // e.g. "Thrissur Main"
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string? MapsUrl { get; set; }

    /// <summary>Primary branch feeds the footer + policy-page tokens.</summary>
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
