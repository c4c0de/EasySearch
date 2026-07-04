using InventoryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace InventoryManagement.Infrastructure.Storage;

/// <summary>
/// Dev-only fallback: saves uploads to wwwroot/uploads and serves them statically.
/// </summary>
public class LocalStorageService(IWebHostEnvironment env) : IStorageService
{
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var uniqueFileName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        await using var fs = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fs, ct);

        return $"/uploads/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (fileUrl.StartsWith("/uploads/"))
        {
            var filePath = Path.Combine(env.WebRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
