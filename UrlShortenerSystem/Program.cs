using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UrlShortenerSystem.BackgroundServices;
using UrlShortenerSystem.Data;
using UrlShortenerSystem.DTOs;
using UrlShortenerSystem.Models;
using UrlShortenerSystem.Utils;
using static UrlShortenerSystem.Utils.Generators;

var builder = WebApplication.CreateBuilder(args);

var sqliteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";

builder.Services.AddDbContext<UrlShortenerContext>(options =>
   options.UseSqlite(sqliteConnectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URL Shortener API",
        Version = "v1",
        Description = "API para encurtar URLs usando .NET 8 Minimal API"
    });

    c.AddSecurityDefinition("DeleteToken", new OpenApiSecurityScheme
    {
        Name = "X-Delete-Token",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Token de exclusão para autorizar remoção de URLs",
    });
});

// Register background services
builder.Services.AddHostedService<CleanupExpiredUrlService>();

// add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();
    context.Database.EnsureCreated();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database inicializado: {ConnectionString}", sqliteConnectionString);
}

app.UseHttpsRedirection();

app.UseRouting();


// ---------------- Endpoints ----------------

app.MapPost("/urls", async (CreateURLRequestDTO request, UrlShortenerContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.OriginalUrl) || !Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest("URL inválida.");
    }

    // Validação do tempo de expiração
    if (request.ExpireIn.HasValue && !TimeHelpers.IsValidExpirationTime(request.ExpireIn.Value))
    {
        var maxExpiration = TimeHelpers.FormatTimeSpan(TimeSpan.FromMilliseconds(TimeHelpers.MaxExpirationMilliseconds));
        return Results.BadRequest(new { error = $"Tempo de expiração inválido. Deve estar entre 1 segundo e {maxExpiration}" });
    }

    string shortCode;
    do
    {
        shortCode = GenerateShortCode();

    } while (await context.ShortUrls.AnyAsync(u => u.ShortCode == shortCode));

    // Calcular data de expiração
    DateTime? expiresAt = null;
    if (request.ExpireIn.HasValue)
    {
        expiresAt = TimeHelpers.CalculateExpirationDate(request.ExpireIn.Value);
    }

    var shortUrl = new ShortUrl
    {
        Id = Guid.NewGuid(),
        OriginalUrl = request.OriginalUrl,
        ShortCode = shortCode,
        DeleteToken = Generators.GenerateDeleteToken(),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = expiresAt,
        Clicks = 0
    };

    context.ShortUrls.Add(shortUrl);
    await context.SaveChangesAsync();

    var baseUrl = GenerateBaseUrl(httpContext.Request);

    logger.LogInformation(
        "URL encurtada criada: {ShortCode} -> {OriginalUrl} (Expira: {ExpiresAt})",
        shortUrl.ShortCode,
        shortUrl.OriginalUrl,
        shortUrl.ExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Nunca"
    );


    var response = new UrlResponseDTO(
        shortUrl.Id.ToString(),
        shortUrl.OriginalUrl,
        shortUrl.ShortCode,
        shortUrl.DeleteToken,
        shortUrl.CreatedAt,
        shortUrl.ExpiresAt,
        shortUrl.Clicks,
        $"{baseUrl}/{shortUrl.ShortCode}"
    );

    return Results.Created($"/urls/{shortUrl.ShortCode}", response);

})
    .WithName("CreateUrl")
    .WithSummary("Create a short URL")
    .WithDescription("Create a new short URL with 6 random chars")
    .WithTags("URLs");


app.MapGet("/{code}", async (string code, UrlShortenerContext context, ILogger<Program> logger) =>
{
    var shortUrl = await context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);

    if (shortUrl == null)
    {
        logger.LogWarning("Tentativa de acesso a URL inexistente: {ShortCode}", code);
        return Results.NotFound(new { error = "URL not found." });
    }

    // se estiver expirada não redirecioanr
    if (shortUrl.IsExpired)
    {
        logger.LogWarning("Tentativa de acesso a URL expirata: {ShortCode}", code);
        return Results.BadRequest("Expired URL.");

    }

    shortUrl.Clicks++;

    await context.SaveChangesAsync();

    logger.LogInformation("Redirecionamento: {ShortCode} -> {OriginalUrl} (Click #{Clicks})",
                        shortUrl.ShortCode, shortUrl.OriginalUrl, shortUrl.Clicks);


    return Results.Redirect(shortUrl.OriginalUrl);
})
.WithName("RedirectUrl")
.WithSummary("Redirecionar para URL original")
.WithDescription("Redireciona para a URL original e incrementa o contador de clicks")
.WithTags("URLs");


app.MapGet("/urls", async (UrlShortenerContext context, HttpContext httpContext) =>
{
    var now = DateTime.UtcNow;

    var urls = await context.ShortUrls
        .Where(u => !u.ExpiresAt.HasValue || u.ExpiresAt.Value > now) // Apenas URLs não expiradas
        .OrderByDescending(u => u.CreatedAt)
        .ToListAsync();

    var baseUrl = GenerateBaseUrl(httpContext.Request);

    var responses = urls.Select(u => new UrlResponseDTO(
        u.Id.ToString(),
        u.OriginalUrl,
        u.ShortCode,
        u.DeleteToken,
        u.CreatedAt,
        u.ExpiresAt,
        u.Clicks,
        $"{baseUrl}/{u.ShortCode}"
    ));

    return Results.Ok(responses);
})
.WithName("GetAllUrls")
.WithSummary("List all URLs")
.WithDescription("Returns list with all short URLs registered")
.WithTags("URLs");

app.MapDelete("/urls/{shortCode}", async (
    string shortCode,
    UrlShortenerContext context,
    HttpContext httpContext,
    ILogger<Program> logger,
    [FromBody] DeleteUrlRequestDTO? requestBody = null) =>
{
    string? deleteToken = httpContext.Request.Headers["X-Delete-Token"].FirstOrDefault();

    if (string.IsNullOrEmpty(deleteToken) && requestBody != null)
    {
        deleteToken = requestBody.DeleteToken;
    }

    if (string.IsNullOrEmpty(deleteToken))
    {
        return Results.BadRequest(new { error = "Token de deleção é obrigatório. Envie via header 'X-Delete-Token' ou no corpo da requisição." });
    }

    var shortUrl = await context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    if (shortUrl == null)
    {
        logger.LogWarning("Tentativa de deleção de URL inexistente: {ShortCode}", shortCode);
        return Results.NotFound(new { error = "URL não encontrada" });
    }

    // Verificar token de deleção
    if (shortUrl.DeleteToken != deleteToken)
    {
        logger.LogWarning("Tentativa de deleção com token inválido: {ShortCode}", shortCode);
        return Results.StatusCode(403); // Forbidden
    }


    context.ShortUrls.Remove(shortUrl);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
    .WithName("DeleteUrl")
    .WithSummary("Deleter a short URL ")
    .WithDescription("Remove an short URL")
    .WithTags("URLs");

app.MapGet("/urls/{code}/info", async (string code, UrlShortenerContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var shortUrl = await context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);

    if (shortUrl == null)
    {
        return Results.NotFound(new { error = "URL não encontrada" });
    }

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

    var response = new UrlResponseDTO(
        shortUrl.Id.ToString(),
        shortUrl.OriginalUrl,
        shortUrl.ShortCode,
        "***HIDDEN***", // Não expor o delete token na consulta
        shortUrl.CreatedAt,
        shortUrl.ExpiresAt,
        shortUrl.Clicks,
        $"{baseUrl}/{shortUrl.ShortCode}"
    );

    return Results.Ok(new
    {
        url = response,
        status = new
        {
            isExpired = shortUrl.IsExpired,
            timeUntilExpiration = shortUrl.ExpiresAt.HasValue && !shortUrl.IsExpired
                ? TimeHelpers.FormatTimeSpan(shortUrl.ExpiresAt.Value - DateTime.UtcNow)
                : null
        }
    });
})
.WithName("GetUrlInfo")
.WithSummary("Obter informações da URL")
.WithDescription("Retorna informações detalhadas sobre a URL sem realizar redirecionamento")
.WithTags("URLs");

app.MapGet("/heath", () => Results.Ok(new { status = "health", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithSummary("Verificar saúde da API")
    .WithTags("System");


app.MapGet("/stats", async (UrlShortenerContext context) =>
{
    var now = DateTime.UtcNow;

    var totalUrls = await context.ShortUrls.CountAsync();
    var activeUrls = await context.ShortUrls
        .CountAsync(u => !u.ExpiresAt.HasValue || u.ExpiresAt.Value > now);
    var expiredUrls = totalUrls - activeUrls;
    var totalClicks = await context.ShortUrls.SumAsync(u => u.Clicks);

    return Results.Ok(new
    {
        totalUrls,
        activeUrls,
        expiredUrls,
        totalClicks,
        timestamp = now
    });
})
.WithName("GetStats")
.WithSummary("Estatísticas da aplicação")
.WithDescription("Retorna estatísticas gerais sobre as URLs cadastradas")
.WithTags("System");


app.Run();

