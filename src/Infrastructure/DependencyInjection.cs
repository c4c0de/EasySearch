using InventoryManagement.Application.Interfaces;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var isSqlite = connectionString.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase)
                    || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<AppDbContext>(options =>
        {
            if (isSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null));
        });

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IDealerRepository, DealerRepository>();
        services.AddScoped<ISiteContentRepository, SiteContentRepository>();

        var cloudinaryCloudName = config["Cloudinary:CloudName"];
        var cloudinaryPreset = config["Cloudinary:UploadPreset"];

        if (!string.IsNullOrWhiteSpace(cloudinaryCloudName) && !string.IsNullOrWhiteSpace(cloudinaryPreset))
        {
            services.AddHttpClient<CloudinaryStorageService>();
            services.AddScoped<IStorageService, CloudinaryStorageService>();
        }
        else if (!string.IsNullOrEmpty(config["AWS:BucketName"]))
        {
            services.AddDefaultAWSOptions(config.GetAWSOptions());
            services.AddAWSService<Amazon.S3.IAmazonS3>();
            services.AddScoped<IStorageService, S3StorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }

        return services;
    }
}
