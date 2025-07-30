using CHAP2.Chorus.Api.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Infrastructure.Repositories;
using CHAP2.Chorus.Api.Interfaces;
using CHAP2.Chorus.Api.Services;
using CHAP2.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition"); // For file downloads
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

builder.Services.AddSingleton<IChorusRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    return new DiskChorusRepository(options.FolderPath, logger);
});

builder.Services.AddScoped<IAiSearchService, AiSearchService>();
builder.Services.AddScoped<ISearchService, ChorusSearchService>();
builder.Services.AddScoped<IChorusQueryService, ChorusQueryService>();
builder.Services.AddScoped<IChorusCommandService, ChorusCommandService>();
builder.Services.AddScoped<ISlideToChorusService, SlideToChorusService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IServices, Services>();

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

app.UseCors();
app.UseResponseCompression();
app.MapControllers();
app.Run();
