using InventoryManagement.Application.Common;
using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Web.Services;
using Microsoft.AspNetCore.Components;

namespace InventoryManagement.Web.Components.Public;

public abstract class CatalogBase : ComponentBase, IDisposable
{
    [Inject] protected IInventoryRepository InventoryRepo { get; set; } = null!;
    [Inject] protected ICategoryRepository CategoryRepo { get; set; } = null!;

    protected static Guid DefaultDealerId => AppConstants.DefaultDealerId;

    protected InventoryFilterQuery filter = new() { DealerId = DefaultDealerId };
    protected PagedList<InventoryListingDto>? result;
    protected List<CategoryDto> categories = new();
    protected List<string> makes = new();
    protected bool loading = true;
    private System.Threading.Timer? debounceTimer;

    protected string StatusFilter
    {
        get => filter.Status?.ToString() ?? "";
        set => filter.Status = Enum.TryParse<ListingStatus>(value, out var s) ? s : null;
    }

    protected override async Task OnInitializedAsync()
    {
        // Public visitors see Available parts first; the Availability filter can switch to All/Sold.
        filter.Status = ListingStatus.Available;
        categories = await CategoryRepo.GetCategoryTreeAsync();
        makes = await InventoryRepo.GetDistinctMakesAsync(DefaultDealerId);
        await LoadListings();
    }

    public void Dispose() => debounceTimer?.Dispose();

    protected void Debounce(Action action)
    {
        debounceTimer?.Dispose();
        debounceTimer = new System.Threading.Timer(_ =>
            InvokeAsync(async () => { action(); filter.PageNumber = 1; await LoadListings(); StateHasChanged(); }), null, 300, Timeout.Infinite);
    }

    protected void OnSearchInput(ChangeEventArgs e) => Debounce(() => filter.SearchTerm = e.Value?.ToString());
    protected void OnMakeInput(ChangeEventArgs e) => Debounce(() => filter.VehicleMake = e.Value?.ToString());

    protected async Task OnYearChange(ChangeEventArgs e)
    {
        filter.Year = int.TryParse(e.Value?.ToString(), out var y) ? y : null;
        filter.PageNumber = 1;
        await LoadListings();
    }

    protected async Task OnCategorySelected(Guid? id)
    {
        filter.CategoryId = id;
        filter.PageNumber = 1;
        await LoadListings();
    }

    protected async Task LoadListings()
    {
        loading = true;
        result = await InventoryRepo.GetFilteredListingsAsync(filter);
        loading = false;
    }

    protected async Task PrevPage() { filter.PageNumber--; await LoadListings(); }
    protected async Task NextPage() { filter.PageNumber++; await LoadListings(); }
}
