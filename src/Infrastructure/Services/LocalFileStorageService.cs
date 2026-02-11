using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

public sealed class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const string UploadDir = "uploads/cards";

    public async Task<string> SaveAsync(Stream stream, string fileName, CancellationToken ct)
    {
        var dir = Path.Combine(env.ContentRootPath, UploadDir);
        Directory.CreateDirectory(dir);

        var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(dir, safeName);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fs, ct);

        return $"{UploadDir}/{safeName}";
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(env.ContentRootPath, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
