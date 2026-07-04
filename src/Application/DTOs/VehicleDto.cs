namespace InventoryManagement.Application.DTOs;

public class VehicleDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public string Make { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Notes { get; set; }
    public int PartCount { get; set; }

    public string DisplayName => Year.HasValue ? $"{Make} {Year}" : Make;
}

public class CreateVehicleDto
{
    public Guid DealerId { get; set; }
    public string Make { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Notes { get; set; }
}
