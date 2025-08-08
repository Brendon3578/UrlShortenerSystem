
using Microsoft.EntityFrameworkCore;
using UrlShortenerSystem.Data;

namespace UrlShortenerSystem.BackgroundServices
{
    /// <summary>
    /// Serviço de background para limpeza automática de URLs expiradas
    /// </summary>
    public class CleanupExpiredUrlService : BackgroundService
    {

        private const int DefaultCleanupMinutes = 1;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupExpiredUrlService> _logger;
        private readonly IConfiguration _configuration;

        // Intervalo de limpeza configurado (default: 1 hora)
        private readonly TimeSpan _cleanupInterval;

        public CleanupExpiredUrlService(IServiceProvider serviceProvider, ILogger<CleanupExpiredUrlService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            var intervalMinutes = _configuration.GetValue<int>("CleanupService:IntervalMinutes", DefaultCleanupMinutes);
            if (intervalMinutes <= 0)
            {
                _logger.LogWarning("Intervalo de limpeza inválido, usando valor padrão de {DefaultCleanupMinutes} minutos.", DefaultCleanupMinutes);
                intervalMinutes = DefaultCleanupMinutes;
            }
            _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);

            _logger.LogInformation("CleanupExpiredUrlsService inicializado. Intervalo: {Interval}", _cleanupInterval);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CleanupExpiredUrlsService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredUrls();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante a limpeza de URLs expiradas");
                }

                // Aguarda o próximo intervalo
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("CleanupExpiredUrlsService finalizado");
        }

        // <summary>
        /// Remove URLs expiradas do banco de dados
        /// </summary>
        private async Task CleanupExpiredUrls()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();

            try
            {
                var now = DateTime.UtcNow;

                // Busca URLs expiradas
                var expiredUrls = await context.ShortUrls
                    .Where(u => u.ExpiresAt.HasValue && u.ExpiresAt.Value <= now)
                    .ToListAsync();

                if (expiredUrls.Count == 0)
                {
                    _logger.LogDebug("Nenhuma URL expirada encontrada em {DateTime}", now);
                    return;
                }

                // Remove URLs expiradas
                context.ShortUrls.RemoveRange(expiredUrls);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Limpeza concluída: {Count} URLs expiradas removidas em {DateTime}. Códigos removidos: [{Codes}]",
                    expiredUrls.Count,
                    now,
                    string.Join(", ", expiredUrls.Select(u => u.ShortCode))
                );

                // Log detalhado para cada URL removida
                foreach (var url in expiredUrls)
                {
                    _logger.LogDebug(
                        "URL removida: {ShortCode} (Criada: {CreatedAt}, Expirou: {ExpiresAt}, Clicks: {Clicks})",
                        url.ShortCode,
                        url.CreatedAt,
                        url.ExpiresAt,
                        url.Clicks
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar limpeza de URLs expiradas");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Parando CleanupExpiredUrlsService...");
            await base.StopAsync(stoppingToken);
        }
    }

}
