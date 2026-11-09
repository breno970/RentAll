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
    private readonly ILogger<SupabaseImageStorage> _logger;
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public SupabaseImageStorage(HttpClient client, IConfiguration config, ILogger<SupabaseImageStorage> logger)
    {
        _client = client;
        _logger = logger;
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
        await using (var probe = image.OpenReadStream())
        {
            if (!await ImageSignature.MatchesAsync(image.ContentType, probe, ct))
                throw new InvalidDataException("O ficheiro não é uma imagem JPG, PNG ou WebP válida.");
        }

        var key = $"{userId}/{Guid.NewGuid():N}{ext}";
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/storage/v1/object/{Bucket}/{key}");
        request.Headers.Add("apikey", _secretKey);
        // Modern sb_secret_* keys are opaque API keys, not JWTs. Never send them as Bearer tokens.
        // Keep legacy service_role JWT compatibility for installations that still use it.
        if (!_secretKey.StartsWith("sb_secret_", StringComparison.Ordinal))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        request.Content = new StreamContent(image.OpenReadStream());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        using var response = await _client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase image upload returned HTTP {StatusCode}: {Details}", (int)response.StatusCode, responseBody);
            var userMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "O serviço de imagens recusou a autorização. Verifica a configuração do Supabase.",
                System.Net.HttpStatusCode.NotFound => "O bucket de fotografias não existe. Verifica o bucket rentall-images no Supabase.",
                System.Net.HttpStatusCode.RequestEntityTooLarge => "A fotografia ultrapassa o limite do armazenamento. Tenta uma imagem menor.",
                _ => $"O armazenamento de fotografias respondeu com erro HTTP {(int)response.StatusCode}. Tenta novamente."
            };
            throw new ImageStorageException(userMessage);
        }
        return key;
    }

    public async Task DeleteAsync(string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        CheckConfiguration();
        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_baseUrl}/storage/v1/object/{Bucket}/{key}");
        request.Headers.Add("apikey", _secretKey);
        // Modern sb_secret_* keys are opaque API keys, not JWTs. Never send them as Bearer tokens.
        // Keep legacy service_role JWT compatibility for installations that still use it.
        if (!_secretKey.StartsWith("sb_secret_", StringComparison.Ordinal))
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

public sealed class ImageStorageException : IOException
{
    public string UserMessage { get; }
    public ImageStorageException(string userMessage) : base(userMessage) => UserMessage = userMessage;
}
