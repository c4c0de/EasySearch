using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.DTOs;

public class InventoryFilterQuery
{
    public Guid DealerId { get; set; }
    public Guid? CategoryId { get; set; }       // Part type filter
    public string? VehicleMake { get; set; }    // Car name search (contains)
    public int? Year { get; set; }              // Year exact match
    public string? SearchTerm { get; set; }    // General: searches make + category + description
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ListingStatus? Status { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public string SortBy { get; set; } = "CreatedAt";
    public bool IsDescending { get; set; } = true;
}
