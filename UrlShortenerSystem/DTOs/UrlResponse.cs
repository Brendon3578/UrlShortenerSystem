namespace UrlShortenerSystem.DTOs
{
    public record UrlResponseDTO(string Id, string OriginalUrl, string ShortCode,
        DateTime CreatedAt, int Clicks, string ShortUrl);
}
