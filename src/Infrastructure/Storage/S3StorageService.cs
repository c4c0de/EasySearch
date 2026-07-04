using Amazon.S3;
using Amazon.S3.Transfer;
using InventoryManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace InventoryManagement.Infrastructure.Storage;

public class S3StorageService(IAmazonS3 s3Client, IConfiguration config) : IStorageService
{
    private readonly string _bucket = config["AWS:BucketName"] ?? throw new InvalidOperationException("AWS:BucketName not configured");
    private readonly string _region = config["AWS:Region"] ?? "us-east-1";

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"inventory/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";

        using var utility = new TransferUtility(s3Client);
        await utility.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        }, ct);

        return $"https://{_bucket}.s3.{_region}.amazonaws.com/{key}";
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/');
        await s3Client.DeleteObjectAsync(_bucket, key, ct);
    }
}
