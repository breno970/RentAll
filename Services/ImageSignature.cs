namespace aluguel.Services;

/// <summary>
/// Verifies that uploaded bytes really are the declared image format. The browser-supplied
/// Content-Type alone is attacker-controlled and must not decide what is stored publicly.
/// </summary>
public static class ImageSignature
{
    public static bool Matches(string contentType, ReadOnlySpan<byte> head)
    {
        switch (contentType.ToLowerInvariant())
        {
            case "image/jpeg":
                return head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
            case "image/png":
                return head.Length >= 8 && head[..8].SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            case "image/webp":
                return head.Length >= 12 &&
                    head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8);
            default:
                return false;
        }
    }

    public static async Task<bool> MatchesAsync(string contentType, Stream stream, CancellationToken ct)
    {
        var buffer = new byte[12];
        int read = 0;
        while (read < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(read), ct);
            if (n == 0) break;
            read += n;
        }
        return Matches(contentType, buffer.AsSpan(0, read));
    }
}
