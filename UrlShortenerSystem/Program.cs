using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UrlShortenerSystem.Data;
using UrlShortenerSystem.DTOs;
using UrlShortenerSystem.Models;
using static UrlShortenerSystem.Utils.Generators;

var builder = WebApplication.CreateBuilder(args);




var sqlLiteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";

builder.Services.AddDbContext<UrlShortenerContext>(options =>
   options.UseSqlite(sqlLiteConnectionString));

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
});



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
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapPost("/urls", async (CreateURLRequestDTO request, UrlShortenerContext context, HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.OriginalUrl) || !Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest("URL inválida.");
    }

    string shortCode;
    do
    {
        shortCode = GenerateShortCode();
    } while (await context.ShortUrls.AnyAsync(u => u.ShortCode == shortCode));

    var shortUrl = new ShortUrl
    {
        Id = Guid.NewGuid(),
        OriginalUrl = request.OriginalUrl,
        ShortCode = shortCode,
        CreatedAt = DateTime.UtcNow,
        Clicks = 0
    };

    context.ShortUrls.Add(shortUrl);
    await context.SaveChangesAsync();

    var baseUrl = GenerateBaseUrl(httpContext.Request);

    var shortUrlResponse = new UrlResponseDTO(
        shortUrl.Id.ToString(),
        shortUrl.OriginalUrl,
        shortUrl.ShortCode,
        shortUrl.CreatedAt,
        shortUrl.Clicks,
        $"{baseUrl}/{shortUrl.ShortCode}"
    );

    return Results.Created($"/urls/{shortUrl.ShortCode}", shortUrlResponse);

})
    .WithName("CreateUrl")
    .WithSummary("Create a short URL")
    .WithDescription("Create a new short URL with 6 random chars")
    .WithTags("URLs");


app.MapGet("/{code}", async (string code, UrlShortenerContext context) =>
{
    var shortUrl = await context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == code);

    if (shortUrl == null)
        return Results.NotFound(new { message = "URL not found." });


    shortUrl.Clicks++;

    await context.SaveChangesAsync();

    return Results.Redirect(shortUrl.OriginalUrl);
})
.WithName("RedirectUrl")
.WithSummary("Redirecionar para URL original")
.WithDescription("Redireciona para a URL original e incrementa o contador de clicks")
.WithTags("URLs");

app.MapGet("/urls", async (UrlShortenerContext context, HttpContext httpContext) =>
{
    var urls = await context.ShortUrls
        .OrderByDescending(u => u.CreatedAt)
        .ToListAsync();

    var baseUrl = GenerateBaseUrl(httpContext.Request);

    var urlResponses = urls.Select(u => new UrlResponseDTO(
        u.Id.ToString(),
        u.OriginalUrl,
        u.ShortCode,
        u.CreatedAt,
        u.Clicks,
        $"{baseUrl}/{u.ShortCode}"
    ));

    return Results.Ok(urlResponses);
})
.WithName("GetAllUrls")
.WithSummary("List all URLs")
.WithDescription("Returns list with all short URLs registered")
.WithTags("URLs");

app.MapDelete("/urls/{shortCode}", async (string shortCode, UrlShortenerContext context) =>
{
    var shortUrl = await context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    if (shortUrl == null)
        return Results.NotFound(new { message = "URL not found." });

    context.ShortUrls.Remove(shortUrl);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteUrl")
.WithSummary("Deleter a short URL ")
.WithDescription("Remove an short URL")
.WithTags("URLs");

app.MapGet("/heath", () => Results.Ok(new { status = "health", timestamp = DateTime.UtcNow }))
.WithName("HealthCheck")
.WithSummary("Verificar saúde da API")
.WithTags("System");


app.Run();

