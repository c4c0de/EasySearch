using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Storage;

public class CloudinaryStorageService(HttpClient httpClient, IConfiguration config, ILogger<CloudinaryStorageService> logger) : IStorageService
{
    private readonly string _cloudName = config["Cloudinary:CloudName"] ?? "";
    private readonly string _uploadPreset = config["Cloudinary:UploadPreset"] ?? "";
    private readonly string? _apiKey = config["Cloudinary:ApiKey"];
    private readonly string? _apiSecret = config["Cloudinary:ApiSecret"];

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cloudName) || string.IsNullOrWhiteSpace(_uploadPreset))
        {
            throw new InvalidOperationException("Cloudinary CloudName and UploadPreset must be configured.");
        }

        var url = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(streamContent, "file", Path.GetFileName(fileName));
        content.Add(new StringContent(_uploadPreset), "upload_preset");

        // If fileName contains folder pathing e.g. "headlight/photo.jpg"
        var folder = Path.GetDirectoryName(fileName)?.Replace('\\', '/').Trim('/');
        if (!string.IsNullOrWhiteSpace(folder))
        {
            content.Add(new StringContent(folder), "folder");
        }

        var response = await httpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("secure_url", out var secureUrl))
        {
            return secureUrl.GetString() ?? throw new InvalidOperationException("Cloudinary response missing secure_url.");
        }
        if (doc.RootElement.TryGetProperty("url", out var plainUrl))
        {
            return plainUrl.GetString() ?? throw new InvalidOperationException("Cloudinary response missing url.");
        }

        throw new InvalidOperationException("Cloudinary response did not contain an image URL.");
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret) || string.IsNullOrWhiteSpace(_cloudName))
        {
            logger.LogWarning("Cloudinary ApiKey or ApiSecret is not configured. Skipping image destruction for {Url}.", fileUrl);
            return;
        }

        var publicId = ExtractPublicId(fileUrl);
        if (string.IsNullOrWhiteSpace(publicId))
        {
            logger.LogWarning("Could not extract public_id from Cloudinary URL: {Url}", fileUrl);
            return;
        }

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var stringToSign = $"public_id={publicId}&timestamp={timestamp}{_apiSecret}";
            var signature = ComputeSha1(stringToSign);

            var destroyUrl = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/destroy";
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("public_id", publicId),
                new KeyValuePair<string, string>("timestamp", timestamp),
                new KeyValuePair<string, string>("api_key", _apiKey),
                new KeyValuePair<string, string>("signature", signature)
            });

            var response = await httpClient.PostAsync(destroyUrl, content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete Cloudinary image {PublicId}", publicId);
        }
    }

    private static string? ExtractPublicId(string url)
    {
        // Example: https://res.cloudinary.com/demo/image/upload/v1570925700/folder/sample.jpg -> folder/sample
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath; // /demo/image/upload/v1570925700/folder/sample.jpg
            var uploadIndex = path.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
            if (uploadIndex < 0) return null;

            var afterUpload = path[(uploadIndex + "/upload/".Length)..]; // v1570925700/folder/sample.jpg or folder/sample.jpg
            var parts = afterUpload.Split('/');
            var startIndex = (parts.Length > 0 && parts[0].StartsWith('v') && long.TryParse(parts[0][1..], out _)) ? 1 : 0;

            var publicIdWithExt = string.Join('/', parts.Skip(startIndex));
            var lastDot = publicIdWithExt.LastIndexOf('.');
            return lastDot > 0 ? publicIdWithExt[..lastDot] : publicIdWithExt;
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeSha1(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
