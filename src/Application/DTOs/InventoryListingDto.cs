using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.DTOs;

public class InventoryListingDto
{
    public Guid Id { get; set; }

    public Guid? VehicleId { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public int? VehicleYear { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }

    public ListingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Chosen per-part contact accounts (for edit prefill) + resolved values (chosen, else default).
    public Guid? WhatsAppAccountId { get; set; }
    public Guid? InstagramAccountId { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? InstagramUsername { get; set; }

    public string VehicleDisplay => string.IsNullOrEmpty(VehicleMake)
        ? "No vehicle"
        : (VehicleYear.HasValue ? $"{VehicleMake} {VehicleYear}" : VehicleMake);
    public string Heading => string.IsNullOrWhiteSpace(Title) ? CategoryName : Title!;
}

// Used for both creating a new part and inline-editing an existing one.
// Vehicle + Part type are entered as free-text tags (find-or-create on save).
public class CreateListingDto
{
    public Guid? Id { get; set; }  // null = create, set = edit existing

    public string? Title { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public int? VehicleYear { get; set; }
    public string PartType { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Available;

    public Guid? WhatsAppAccountId { get; set; }
    public Guid? InstagramAccountId { get; set; }
}
