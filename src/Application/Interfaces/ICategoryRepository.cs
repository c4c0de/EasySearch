using InventoryManagement.Application.DTOs;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<CategoryDto>> GetCategoryTreeAsync(CancellationToken ct = default);
    Task<List<Category>> GetAllFlatAsync(CancellationToken ct = default);
    Task<Category> CreateAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
