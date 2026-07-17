using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class SiteContentRepository(AppDbContext context) : ISiteContentRepository
{
    // ---- Settings ----
    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken ct = default)
        => await context.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, ct);

    public async Task SaveSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default)
    {
        var existing = await context.SiteSettings.ToDictionaryAsync(s => s.Key, s => s, ct);
        foreach (var (key, value) in settings)
        {
            if (existing.TryGetValue(key, out var row)) row.Value = value;
            else context.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        }
        await context.SaveChangesAsync(ct);
    }

    // ---- Content pages ----
    private static ContentPageDto ToDto(ContentPage p) => new()
    {
        Id = p.Id, Slug = p.Slug, Title = p.Title, Markdown = p.Markdown, UpdatedAt = p.UpdatedAt
    };

    public async Task<ContentPageDto?> GetPageAsync(string slug, CancellationToken ct = default)
    {
        var page = await context.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug, ct);
        return page is null ? null : ToDto(page);
    }

    public async Task<List<ContentPageDto>> GetPagesAsync(CancellationToken ct = default)
        => (await context.ContentPages.AsNoTracking().OrderBy(p => p.Title).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task SavePageAsync(string slug, string title, string markdown, CancellationToken ct = default)
    {
        var page = await context.ContentPages.FirstOrDefaultAsync(p => p.Slug == slug, ct);
        if (page is null)
        {
            page = new ContentPage { Id = Guid.NewGuid(), Slug = slug };
            context.ContentPages.Add(page);
        }
        page.Title = title;
        page.Markdown = markdown;
        page.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    // ---- Branches ----
    private static BranchDto ToDto(Branch b) => new()
    {
        Id = b.Id, Name = b.Name, Address = b.Address, Phone = b.Phone, Email = b.Email,
        Hours = b.Hours, MapsUrl = b.MapsUrl, IsPrimary = b.IsPrimary, SortOrder = b.SortOrder
    };

    public async Task<List<BranchDto>> GetBranchesAsync(Guid dealerId, CancellationToken ct = default)
        // AsNoTracking: SetPrimary uses ExecuteUpdate (bypasses the change tracker), so reads must
        // hit the DB fresh — otherwise the circuit-scoped context serves stale cached entities.
        => (await context.Branches
            .AsNoTracking()
            .Where(b => b.DealerId == dealerId)
            .OrderByDescending(b => b.IsPrimary).ThenBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<Branch?> GetBranchByIdAsync(Guid id, CancellationToken ct = default)
        // Fresh, untracked read — FindAsync could return a stale cached instance after
        // ExecuteUpdate-based flag flips. Update() re-attaches the detached entity fine.
        => await context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task CreateBranchAsync(Branch branch, CancellationToken ct = default)
    {
        // First branch automatically becomes primary.
        var hasPrimary = await context.Branches.AsNoTracking()
            .AnyAsync(b => b.DealerId == branch.DealerId && b.IsPrimary, ct);
        if (!hasPrimary) branch.IsPrimary = true;
        if (branch.IsPrimary) await ClearPrimaryAsync(branch.DealerId, branch.Id, ct);

        context.Branches.Add(branch);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    public async Task UpdateBranchAsync(Branch branch, CancellationToken ct = default)
    {
        if (branch.IsPrimary) await ClearPrimaryAsync(branch.DealerId, branch.Id, ct);
        context.Branches.Update(branch);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    public async Task DeleteBranchAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        if (branch is null) return;

        // Never delete the last branch — footer/policy tokens need one.
        var count = await context.Branches.AsNoTracking().CountAsync(b => b.DealerId == branch.DealerId, ct);
        if (count <= 1) return;

        await context.Branches.Where(b => b.Id == id).ExecuteDeleteAsync(ct);

        // If the primary was deleted, promote the first remaining branch.
        if (branch.IsPrimary)
        {
            var nextId = await context.Branches.AsNoTracking()
                .Where(b => b.DealerId == branch.DealerId)
                .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(ct);
            if (nextId is not null)
                await context.Branches.Where(b => b.Id == nextId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsPrimary, true), ct);
        }

        context.ChangeTracker.Clear();
    }

    public async Task SetPrimaryBranchAsync(Guid id, CancellationToken ct = default)
    {
        // Pure DB operation — no tracked entities. The change tracker cannot be trusted here:
        // ExecuteUpdate bypasses it, so tracked instances go stale and later writes no-op or
        // resurrect old flag values. (Clear-then-set order: a crash between the two statements
        // leaves zero primaries, which SiteConfig.PrimaryBranch already falls back from.)
        var dealerId = await context.Branches.AsNoTracking()
            .Where(b => b.Id == id).Select(b => (Guid?)b.DealerId).FirstOrDefaultAsync(ct);
        if (dealerId is null) return;

        await context.Branches
            .Where(b => b.DealerId == dealerId && b.Id != id && b.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsPrimary, false), ct);
        await context.Branches
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsPrimary, true), ct);

        context.ChangeTracker.Clear();
    }

    private async Task ClearPrimaryAsync(Guid dealerId, Guid exceptId, CancellationToken ct)
    {
        await context.Branches
            .Where(b => b.DealerId == dealerId && b.IsPrimary && b.Id != exceptId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsPrimary, false), ct);
    }
}
