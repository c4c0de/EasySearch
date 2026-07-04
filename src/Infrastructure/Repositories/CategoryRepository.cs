using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<List<CategoryDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        var all = await context.Categories.ToListAsync(ct);
        return BuildTree(all, null);
    }

    private static List<CategoryDto> BuildTree(List<Category> all, Guid? parentId)
        => all.Where(c => c.ParentCategoryId == parentId)
              .Select(c => new CategoryDto
              {
                  Id = c.Id,
                  Name = c.Name,
                  Slug = c.Slug,
                  ParentCategoryId = c.ParentCategoryId,
                  SubCategories = BuildTree(all, c.Id)
              })
              .ToList();

    public async Task<List<Category>> GetAllFlatAsync(CancellationToken ct = default)
        => await context.Categories.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await context.Categories.FindAsync([id], ct);
        if (category is not null)
        {
            context.Categories.Remove(category);
            await context.SaveChangesAsync(ct);
        }
    }
}
