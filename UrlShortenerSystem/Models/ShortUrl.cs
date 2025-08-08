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

        public DateTime? ExpiresAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeleteToken { get; set; } = string.Empty;

        /// <summary>
        /// Verifica se a URL está expirada
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }
}
