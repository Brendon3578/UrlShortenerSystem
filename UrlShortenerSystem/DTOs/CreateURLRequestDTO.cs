using System.ComponentModel.DataAnnotations;

namespace UrlShortenerSystem.DTOs
{
    public record CreateURLRequestDTO([Required] string OriginalUrl);
}
