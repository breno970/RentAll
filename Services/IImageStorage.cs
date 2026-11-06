using Microsoft.AspNetCore.Http;

namespace aluguel.Services;

public interface IImageStorage
{
    Task<string> UploadAsync(IFormFile image, string userId, CancellationToken ct = default);
    Task DeleteAsync(string? key, CancellationToken ct = default);
    string PublicUrl(string key);
}
