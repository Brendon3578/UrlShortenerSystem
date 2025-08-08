using System.ComponentModel.DataAnnotations;

namespace UrlShortenerSystem.DTOs
{
    /// <summary>
    /// DTO para deleção via corpo da requisição
    /// </summary>
    public record DeleteUrlRequestDTO
    {
        [Required(ErrorMessage = "DeleteToken é obrigatório")]
        public string DeleteToken { get; init; } = string.Empty;
    }
}
