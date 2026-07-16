using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class InventoryRepository(AppDbContext context) : IInventoryRepository
{
    public async Task<PagedList<InventoryListingDto>> GetFilteredListingsAsync(InventoryFilterQuery query, CancellationToken ct = default)
    {
        var q = context.InventoryListings
            .Include(l => l.Vehicle)
            .Include(l => l.Category)
            .Include(l => l.WhatsAppAccount)
            .Include(l => l.InstagramAccount)
            .Where(l => l.DealerId == query.DealerId)
            .AsQueryable();

        // Part type (category) filter — includes sub-categories
        if (query.CategoryId.HasValue)
        {
            var subIds = await GetCategoryAndDescendantIds(query.CategoryId.Value, ct);
            q = q.Where(l => subIds.Contains(l.CategoryId));
        }

        // Car make search (contains, case-insensitive)
        if (!string.IsNullOrWhiteSpace(query.VehicleMake))
        {
            var make = query.VehicleMake.ToLower();
            q = q.Where(l => l.Vehicle != null && l.Vehicle.Make.ToLower().Contains(make));
        }

        // Year exact match
        if (query.Year.HasValue)
            q = q.Where(l => l.Vehicle != null && l.Vehicle.Year == query.Year.Value);

        // General search: make + category name + description
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(l => (l.Vehicle != null && l.Vehicle.Make.ToLower().Contains(term))
                           || l.Category.Name.ToLower().Contains(term)
                           || (l.Description != null && l.Description.ToLower().Contains(term)));
        }

        if (query.MinPrice.HasValue) q = q.Where(l => l.Price >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue) q = q.Where(l => l.Price <= query.MaxPrice.Value);
        if (query.Status.HasValue) q = q.Where(l => l.Status == query.Status.Value);

        q = (query.SortBy, query.IsDescending) switch
        {
            ("Price", false) => q.OrderBy(l => l.Price),
            ("Price", true) => q.OrderByDescending(l => l.Price),
            ("Make", false) => q.OrderBy(l => l.Vehicle!.Make),
            ("Make", true) => q.OrderByDescending(l => l.Vehicle!.Make),
            (_, false) => q.OrderBy(l => l.CreatedAt),
            (_, true) => q.OrderByDescending(l => l.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var listings = await q
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Default accounts used when a part has no explicit pick.
        var defaultWa = await context.SocialAccounts.AsNoTracking().FirstOrDefaultAsync(
            s => s.DealerId == query.DealerId && s.Type == SocialAccountType.WhatsApp && s.IsDefault, ct);
        var defaultIg = await context.SocialAccounts.AsNoTracking().FirstOrDefaultAsync(
            s => s.DealerId == query.DealerId && s.Type == SocialAccountType.Instagram && s.IsDefault, ct);

        var items = listings.Select(l => new InventoryListingDto
        {
            Id = l.Id,
            VehicleId = l.VehicleId,
            VehicleMake = l.Vehicle?.Make ?? string.Empty,
            VehicleYear = l.Vehicle?.Year,
            CategoryId = l.CategoryId,
            CategoryName = l.Category.Name,
            Title = l.Title,
            ImageUrl = l.ImageUrl,
            Description = l.Description,
            Price = l.Price,
            Status = l.Status,
            CreatedAt = l.CreatedAt,
            WhatsAppAccountId = l.WhatsAppAccountId,
            InstagramAccountId = l.InstagramAccountId,
            WhatsAppNumber = l.WhatsAppAccount?.Value ?? defaultWa?.Value,
            InstagramUsername = l.InstagramAccount?.Value ?? defaultIg?.Value
        }).ToList();

        return new PagedList<InventoryListingDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    // Returns a category id plus all descendant ids (for sub-category filtering)
    private async Task<List<Guid>> GetCategoryAndDescendantIds(Guid categoryId, CancellationToken ct)
    {
        var all = await context.Categories.ToListAsync(ct);
        var result = new List<Guid>();
        Collect(categoryId, all, result);
        return result;

        static void Collect(Guid id, List<Category> all, List<Guid> result)
        {
            result.Add(id);
            foreach (var child in all.Where(c => c.ParentCategoryId == id))
                Collect(child.Id, all, result);
        }
    }

    public async Task<InventoryListing?> GetListingByIdAsync(Guid id, CancellationToken ct = default)
        => await context.InventoryListings
            .Include(l => l.Vehicle)
            .Include(l => l.Category)
            .Include(l => l.WhatsAppAccount)
            .Include(l => l.InstagramAccount)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<InventoryListing> CreateListingAsync(InventoryListing listing, CancellationToken ct = default)
    {
        context.InventoryListings.Add(listing);
        await context.SaveChangesAsync(ct);
        return listing;
    }

    public async Task UpdateListingAsync(InventoryListing listing, CancellationToken ct = default)
    {
        context.InventoryListings.Update(listing);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteListingAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await context.InventoryListings.FindAsync([id], ct);
        if (listing is not null)
        {
            context.InventoryListings.Remove(listing);
            await context.SaveChangesAsync(ct);
        }
    }

    // Vehicles
    public async Task<List<VehicleDto>> GetVehiclesAsync(Guid dealerId, CancellationToken ct = default)
        => await context.Vehicles
            .Where(v => v.DealerId == dealerId)
            .OrderBy(v => v.Make).ThenBy(v => v.Year)
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                DealerId = v.DealerId,
                Make = v.Make,
                Year = v.Year,
                Notes = v.Notes,
                PartCount = v.Parts.Count
            })
            .ToListAsync(ct);

    public async Task<Vehicle?> GetVehicleByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Vehicles.FindAsync([id], ct);

    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        context.Vehicles.Update(vehicle);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteVehicleAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await context.Vehicles.FindAsync([id], ct);
        if (vehicle is null) return;

        // Keep the parts — just remove their link to this vehicle.
        await context.InventoryListings
            .Where(l => l.VehicleId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.VehicleId, (Guid?)null), ct);

        context.Vehicles.Remove(vehicle);
        await context.SaveChangesAsync(ct);
    }

    // Find-or-create tags — matches case-insensitively, inserts if missing.
    public async Task<Vehicle> GetOrCreateVehicleAsync(Guid dealerId, string make, int? year, CancellationToken ct = default)
    {
        var trimmed = make.Trim();
        var existing = await context.Vehicles.FirstOrDefaultAsync(
            v => v.DealerId == dealerId && v.Make.ToLower() == trimmed.ToLower() && v.Year == year, ct);
        if (existing is not null) return existing;

        var vehicle = new Vehicle { Id = Guid.NewGuid(), DealerId = dealerId, Make = trimmed, Year = year };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task<Category> GetOrCreateCategoryAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == trimmed.ToLower(), ct);
        if (existing is not null) return existing;

        var slug = trimmed.ToLower().Replace(' ', '-');
        var category = new Category { Id = Guid.NewGuid(), Name = trimmed, Slug = slug };
        context.Categories.Add(category);
        await context.SaveChangesAsync(ct);
        return category;
    }

    public async Task<List<string>> GetDistinctMakesAsync(Guid dealerId, CancellationToken ct = default)
        => await context.Vehicles
            .Where(v => v.DealerId == dealerId)
            .Select(v => v.Make)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(ct);

    // ---- Social contact accounts ----
    private static SocialAccountDto ToDto(SocialAccount s) => new()
    {
        Id = s.Id, Type = s.Type, Label = s.Label, Value = s.Value, IsDefault = s.IsDefault
    };

    // AsNoTracking on reads: SetDefault uses ExecuteUpdate (bypasses the change tracker),
    // so tracked entities would show stale IsDefault values on reload.
    public async Task<List<SocialAccountDto>> GetSocialAccountsAsync(Guid dealerId, CancellationToken ct = default)
        => (await context.SocialAccounts
            .AsNoTracking()
            .Where(s => s.DealerId == dealerId)
            .OrderByDescending(s => s.IsDefault).ThenBy(s => s.Label)
            .ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<List<SocialAccountDto>> GetSocialAccountsByTypeAsync(Guid dealerId, SocialAccountType type, CancellationToken ct = default)
        => (await context.SocialAccounts
            .AsNoTracking()
            .Where(s => s.DealerId == dealerId && s.Type == type)
            .OrderByDescending(s => s.IsDefault).ThenBy(s => s.Label)
            .ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<SocialAccount?> GetSocialAccountByIdAsync(Guid id, CancellationToken ct = default)
        => await context.SocialAccounts.FindAsync([id], ct);

    public async Task<SocialAccount> CreateSocialAccountAsync(SocialAccount account, CancellationToken ct = default)
    {
        // First account of a type becomes the default automatically.
        var hasDefault = await context.SocialAccounts.AnyAsync(
            s => s.DealerId == account.DealerId && s.Type == account.Type && s.IsDefault, ct);
        if (!hasDefault) account.IsDefault = true;

        if (account.IsDefault) await ClearDefaultsAsync(account.DealerId, account.Type, account.Id, ct);

        context.SocialAccounts.Add(account);
        await context.SaveChangesAsync(ct);
        return account;
    }

    public async Task UpdateSocialAccountAsync(SocialAccount account, CancellationToken ct = default)
    {
        if (account.IsDefault) await ClearDefaultsAsync(account.DealerId, account.Type, account.Id, ct);
        context.SocialAccounts.Update(account);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteSocialAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await context.SocialAccounts.FindAsync([id], ct);
        if (account is null) return;

        // Unassign from any parts first (FKs are NO ACTION) so they fall back to the default.
        await context.InventoryListings.Where(l => l.WhatsAppAccountId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.WhatsAppAccountId, (Guid?)null), ct);
        await context.InventoryListings.Where(l => l.InstagramAccountId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.InstagramAccountId, (Guid?)null), ct);

        context.SocialAccounts.Remove(account);
        await context.SaveChangesAsync(ct);
    }

    public async Task SetDefaultSocialAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await context.SocialAccounts.FindAsync([id], ct);
        if (account is null) return;
        await ClearDefaultsAsync(account.DealerId, account.Type, id, ct);
        account.IsDefault = true;
        await context.SaveChangesAsync(ct);
    }

    // Clears IsDefault on all other accounts of the same dealer + type.
    private async Task ClearDefaultsAsync(Guid dealerId, SocialAccountType type, Guid exceptId, CancellationToken ct)
    {
        await context.SocialAccounts
            .Where(s => s.DealerId == dealerId && s.Type == type && s.IsDefault && s.Id != exceptId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);
    }
}
