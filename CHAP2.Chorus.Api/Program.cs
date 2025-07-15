using CHAP2.Chorus.Api.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Infrastructure.Repositories;
using CHAP2.Chorus.Api.Interfaces;
using CHAP2.Chorus.Api.Services;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add memory cache for performance
builder.Services.AddMemoryCache();

// Add HTTP client factory with retry policies
builder.Services.AddHttpClient();

// Configure settings with validation
builder.Services.Configure<ChorusResourceOptions>(
    builder.Configuration.GetSection("ChorusResource"));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<SearchSettings>(
    builder.Configuration.GetSection("SearchSettings"));
builder.Services.Configure<SlideConversionSettings>(
    builder.Configuration.GetSection("SlideConversionSettings"));

// Register Infrastructure services (Data Access Layer)
builder.Services.AddSingleton<IChorusRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    return new DiskChorusRepository(options.FolderPath, logger);
});
builder.Services.AddScoped<IChorusRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    return new DiskChorusRepository(options.FolderPath, logger);
});

// Register Application services (Business Logic Layer)
builder.Services.AddScoped<ISearchService, ChorusSearchService>();
builder.Services.AddScoped<IChorusQueryService, ChorusQueryService>();
builder.Services.AddScoped<IChorusCommandService, ChorusCommandService>();
builder.Services.AddScoped<ISlideToChorusService, SlideToChorusService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Register API services (Presentation Layer)
builder.Services.AddScoped<IServices, Services>();

// Add controllers with a global route prefix
builder.Services.AddControllers(options =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
    options.Conventions.Insert(0, new GlobalRoutePrefixConvention(apiSettings.GlobalRoutePrefix));
});

// Add response compression for better performance
builder.Services.AddResponseCompression();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use response compression
app.UseResponseCompression();

app.MapControllers();

app.Run();
