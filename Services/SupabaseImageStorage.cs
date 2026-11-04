using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace aluguel.Services;

/// <summary>
/// Server-side Supabase Storage client: the secret key must NEVER be sent to browsers.
/// Store only generated object keys (not original filenames) in the Items table.
/// </summary>
public sealed class SupabaseImageStorage : IImageStorage
{
    private const string Bucket = "rentall-images";
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly string _secretKey;
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public SupabaseImageStorage(HttpClient client, IConfiguration config)
    {
        _client = client;
        _baseUrl = (config["Supabase:Url"] ?? "").TrimEnd('/');
        _secretKey = config["Supabase:ServiceKey"] ?? "";
    }

    private void CheckConfiguration()
    {
        if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_secretKey))
            throw new InvalidOperationException("Configure Supabase__Url and Supabase__ServiceKey in Render environment.");
    }

    public async Task<string> UploadAsync(IFormFile image, string userId, CancellationToken ct = default)
    {
        CheckConfiguration();
        if (image.Length <= 0 || image.Length > MaxBytes)
            throw new InvalidDataException("A imagem deve ter até 5 MB.");
        if (!Allowed.TryGetValue(image.ContentType, out var ext))
            throw new InvalidDataException("Apenas imagens JPG, PNG ou WebP são aceites.");

        var key = $"{userId}/{Guid.NewGuid():N}{ext}";
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/storage/v1/object/{Bucket}/{key}");
        request.Headers.Add("apikey", _secretKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        request.Content = new StreamContent(image.OpenReadStream());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        using var response = await _client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new IOException($"Falha no armazenamento de imagem (HTTP {(int)response.StatusCode}).");
        return key;
    }

    public async Task DeleteAsync(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        CheckConfiguration();
        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_baseUrl}/storage/v1/object/{Bucket}/{key}");
        request.Headers.Add("apikey", _secretKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        using var response = await _client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw new IOException($"Falha ao eliminar imagem (HTTP {(int)response.StatusCode}).");
    }

    public string PublicUrl(string key)
    {
        if (string.IsNullOrEmpty(_baseUrl)) throw new InvalidOperationException("Supabase__Url não configurada.");
        // Only generated keys are stored; the path is exactly owner-UUID/filename-UUID.ext.
        return $"{_baseUrl}/storage/v1/object/public/{Bucket}/{key}";
    }
}
