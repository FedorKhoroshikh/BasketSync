namespace Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, CancellationToken ct);
    void Delete(string relativePath);
}
