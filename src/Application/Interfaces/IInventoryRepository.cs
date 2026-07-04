using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Interfaces;

public interface IInventoryRepository
{
    // Parts (listings)
    Task<PagedList<InventoryListingDto>> GetFilteredListingsAsync(InventoryFilterQuery query, CancellationToken ct = default);
    Task<InventoryListing?> GetListingByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryListing> CreateListingAsync(InventoryListing listing, CancellationToken ct = default);
    Task UpdateListingAsync(InventoryListing listing, CancellationToken ct = default);
    Task DeleteListingAsync(Guid id, CancellationToken ct = default);

    // Vehicles
    Task<List<VehicleDto>> GetVehiclesAsync(Guid dealerId, CancellationToken ct = default);
    Task<Vehicle?> GetVehicleByIdAsync(Guid id, CancellationToken ct = default);
    Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, CancellationToken ct = default);
    Task UpdateVehicleAsync(Vehicle vehicle, CancellationToken ct = default);
    Task DeleteVehicleAsync(Guid id, CancellationToken ct = default);

    // Find-or-create tags (no pre-creation required)
    Task<Vehicle> GetOrCreateVehicleAsync(Guid dealerId, string make, int? year, CancellationToken ct = default);
    Task<Category> GetOrCreateCategoryAsync(string name, CancellationToken ct = default);
    Task<List<string>> GetDistinctMakesAsync(Guid dealerId, CancellationToken ct = default);

    // Social contact accounts (WhatsApp / Instagram)
    Task<List<SocialAccountDto>> GetSocialAccountsAsync(Guid dealerId, CancellationToken ct = default);
    Task<List<SocialAccountDto>> GetSocialAccountsByTypeAsync(Guid dealerId, SocialAccountType type, CancellationToken ct = default);
    Task<SocialAccount> CreateSocialAccountAsync(SocialAccount account, CancellationToken ct = default);
    Task UpdateSocialAccountAsync(SocialAccount account, CancellationToken ct = default);
    Task DeleteSocialAccountAsync(Guid id, CancellationToken ct = default);
    Task SetDefaultSocialAccountAsync(Guid id, CancellationToken ct = default);
    Task<SocialAccount?> GetSocialAccountByIdAsync(Guid id, CancellationToken ct = default);
}
