using InventoryManagement.Application.DTOs;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Interfaces;

public interface ISiteContentRepository
{
    // Settings (key-value)
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default);

    // Content pages (markdown)
    Task<ContentPageDto?> GetPageAsync(string slug, CancellationToken ct = default);
    Task<List<ContentPageDto>> GetPagesAsync(CancellationToken ct = default);
    Task SavePageAsync(string slug, string title, string markdown, CancellationToken ct = default);

    // Branches
    Task<List<BranchDto>> GetBranchesAsync(Guid dealerId, CancellationToken ct = default);
    Task<Branch?> GetBranchByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateBranchAsync(Branch branch, CancellationToken ct = default);
    Task UpdateBranchAsync(Branch branch, CancellationToken ct = default);
    Task DeleteBranchAsync(Guid id, CancellationToken ct = default);
    Task SetPrimaryBranchAsync(Guid id, CancellationToken ct = default);
}
