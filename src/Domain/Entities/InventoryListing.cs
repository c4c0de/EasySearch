using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class InventoryListing
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;  // Part type (Headlight, Tail Light, etc.)

    // Optional per-part contact accounts; fall back to the dealer's default when null.
    public Guid? WhatsAppAccountId { get; set; }
    public SocialAccount? WhatsAppAccount { get; set; }
    public Guid? InstagramAccountId { get; set; }
    public SocialAccount? InstagramAccount { get; set; }

    public string? Title { get; set; }              // Optional part name, e.g. "Rear diffuser"
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Available;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
