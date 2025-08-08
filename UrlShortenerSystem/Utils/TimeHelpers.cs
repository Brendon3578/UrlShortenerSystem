namespace UrlShortenerSystem.Utils
{
    public static class TimeHelpers
    {
        public static readonly long MaxExpirationMilliseconds = 2L * 365 * 24 * 60 * 60 * 1000; // 2 anos em ms

        /// <summary>
        /// Converte milissegundos para TimeSpan
        /// </summary>
        /// <param name="milliseconds">Quantidade em milissegundos</param>
        /// <returns>TimeSpan equivalente</returns>
        public static TimeSpan MillisecondsToTimeSpan(long milliseconds)
        {
            if (milliseconds <= 0)
                throw new ArgumentException("Milissegundos deve ser um valor positivo", nameof(milliseconds));

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// Calcula data de expiração baseada em milissegundos
        /// </summary>
        /// <param name="milliseconds">Tempo de vida em milissegundos</param>
        /// <returns>Data de expiração calculada</returns>
        public static DateTime CalculateExpirationDate(long milliseconds)
        {
            var timespan = MillisecondsToTimeSpan(milliseconds);

            return DateTime.UtcNow.Add(timespan);
        }

        /// <summary>
        /// Valida se o tempo de expiração é válido
        /// </summary>
        /// <param name="milliseconds">Milissegundos para validar</param>
        /// <returns>True se válido</returns>
        public static bool IsValidExpirationTime(long? milliseconds)
        {
            if (!milliseconds.HasValue)
                return true; // Null é válido (sem expiração)

            // Mínimo de 1 segundo, máximo de MaxExpirationMilliseconds
            const long oneSecond = 1000;

            return milliseconds.Value >= oneSecond && milliseconds.Value <= MaxExpirationMilliseconds;
        }

        /// <summary>
        /// Formata TimeSpan para string legível
        /// </summary>
        /// <param name="timeSpan">TimeSpan para formatar</param>
        /// <returns>String formatada (ex: "2h 30m 15s")</returns>
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            var parts = new List<string>();

            //ano
            if (timeSpan.Days >= 365)
            {
                var years = timeSpan.Days / 365;
                parts.Add($"{years}y");
                timeSpan = timeSpan.Subtract(TimeSpan.FromDays(years * 365));
            }
            if (timeSpan.Days >= 30)
            {
                var months = timeSpan.Days / 30;
                parts.Add($"{months}mo");
                timeSpan = timeSpan.Subtract(TimeSpan.FromDays(months * 30));
            }
            if (timeSpan.Days > 0)
                parts.Add($"{timeSpan.Days}d");
            if (timeSpan.Hours > 0)
                parts.Add($"{timeSpan.Hours}h");
            if (timeSpan.Minutes > 0)
                parts.Add($"{timeSpan.Minutes}m");
            if (timeSpan.Seconds > 0 || parts.Count == 0)
                parts.Add($"{timeSpan.Seconds}s");

            return string.Join(" ", parts);
        }
    }
}
