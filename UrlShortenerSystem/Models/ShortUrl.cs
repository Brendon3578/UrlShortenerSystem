using System.ComponentModel.DataAnnotations;

namespace UrlShortenerSystem.Models
{
    public class ShortUrl
    {

        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ShortCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int Clicks { get; set; }
    }
}
