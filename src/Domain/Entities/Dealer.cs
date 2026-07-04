namespace InventoryManagement.Domain.Entities;

public class Dealer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DealerCode { get; set; } = string.Empty;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
