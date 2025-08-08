using System.ComponentModel.DataAnnotations;

namespace UrlShortenerSystem.DTOs
{
    /// <summary>
    /// DTO para criação de URL encurtada
    /// </summary>
    public record CreateURLRequestDTO
    {
        [Required(ErrorMessage = "OriginalUrl é obrigatória")]
        [Url(ErrorMessage = "OriginalUrl deve ser uma URL válida")]
        public string OriginalUrl { get; init; } = string.Empty;

        /// <summary>
        /// Tempo de expiração em milissegundos (opcional)
        /// Exemplo: 3600000 = 1 hora
        /// </summary>
        [Range(1000, long.MaxValue, ErrorMessage = "ExpireIn deve ser maior que 1000ms (1 segundo)")]
        public long? ExpireIn { get; init; }
    }
}
