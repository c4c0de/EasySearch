using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Interfaces;

public interface IDealerRepository
{
    Task<List<Dealer>> GetAllAsync(CancellationToken ct = default);
    Task<Dealer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Dealer> CreateAsync(Dealer dealer, CancellationToken ct = default);
}
