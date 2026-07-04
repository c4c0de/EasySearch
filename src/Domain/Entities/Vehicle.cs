namespace InventoryManagement.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public string Make { get; set; } = string.Empty;  // e.g. "Maruti Swift", "Honda City"
    public int? Year { get; set; }                    // e.g. 2024 (nullable — year unknown)
    public string? Notes { get; set; }

    public ICollection<InventoryListing> Parts { get; set; } = new List<InventoryListing>();
}
