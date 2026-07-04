using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class DealerRepository(AppDbContext context) : IDealerRepository
{
    public async Task<List<Dealer>> GetAllAsync(CancellationToken ct = default)
        => await context.Dealers.OrderBy(d => d.Name).ToListAsync(ct);

    public async Task<Dealer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Dealers.FindAsync([id], ct);

    public async Task<Dealer> CreateAsync(Dealer dealer, CancellationToken ct = default)
    {
        context.Dealers.Add(dealer);
        await context.SaveChangesAsync(ct);
        return dealer;
    }
}
