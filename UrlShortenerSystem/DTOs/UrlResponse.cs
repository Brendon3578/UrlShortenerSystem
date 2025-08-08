namespace UrlShortenerSystem.DTOs
{

    /// <summary>
    /// DTO para resposta de URL encurtada
    /// </summary>
    public record UrlResponseDTO(
        string Id,
        string OriginalUrl,
        string ShortCode,
        string DeleteToken,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        int Clicks,
        string ShortUrl
    );
}
