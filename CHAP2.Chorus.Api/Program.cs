using CHAP2.Chorus.Api.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Application.EventHandlers;
using CHAP2.Domain.Events;
using CHAP2.Infrastructure.Repositories;
using CHAP2.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Add CORS support
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:5000", "http://localhost:5001", "http://localhost:5173" }
            : Array.Empty<string>());

    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition");
        }
    });
});

builder.Services.Configure<ChorusResourceOptions>(
    builder.Configuration.GetSection("ChorusResource"));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<SearchSettings>(
    builder.Configuration.GetSection("SearchSettings"));
builder.Services.Configure<SlideConversionSettings>(
    builder.Configuration.GetSection("SlideConversionSettings"));

// Register the base repository
builder.Services.AddSingleton<DiskChorusRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    return new DiskChorusRepository(options.FolderPath, logger);
});

// Register the cached repository decorator
builder.Services.AddSingleton<IChorusRepository>(provider =>
{
    var innerRepository = provider.GetRequiredService<DiskChorusRepository>();
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<CachedChorusRepository>>();
    return new CachedChorusRepository(innerRepository, cache, logger);
});

builder.Services.AddScoped<IAiSearchService, AiSearchService>();
builder.Services.AddScoped<ISearchService, ChorusSearchService>();
builder.Services.AddScoped<IChorusQueryService, ChorusQueryService>();
builder.Services.AddScoped<IChorusCommandService, ChorusCommandService>();
builder.Services.AddScoped<ISlideToChorusService, SlideToChorusService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
// Register domain event handlers
builder.Services.AddScoped<IDomainEventHandler<ChorusCreatedEvent>, ChorusCreatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusUpdatedEvent>, ChorusUpdatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusDeletedEvent>, ChorusDeletedEventHandler>();

builder.Services.AddControllers(options =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
    options.Conventions.Insert(0, new GlobalRoutePrefixConvention(apiSettings.GlobalRoutePrefix));
});

builder.Services.AddResponseCompression();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseResponseCompression();
app.MapControllers();
app.Run();
