namespace UrlShortenerSystem.Utils
{
    public static class Generators
    {
        public static string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            var shortCode = new char[6];

            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateBaseUrl(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        /// <summary>
        /// Gera um token de deleção único
        /// </summary>
        /// <returns>Token único para deleção</returns>
        public static string GenerateDeleteToken()
        {
            return Guid.NewGuid().ToString("N"); // Remove hífens para token mais limpo
        }
    }
}
