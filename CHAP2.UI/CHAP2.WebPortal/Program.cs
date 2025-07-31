using CHAP2.WebPortal.Interfaces;
using CHAP2.WebPortal.Services;
using CHAP2.Shared.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Infrastructure.Repositories;
using CHAP2.WebPortal.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Configure response buffering for streaming
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// Configure HttpClient for API communication using shared configuration
builder.Services.AddCHAP2ApiClient(builder.Configuration);

// Configure settings
builder.Services.Configure<QdrantSettings>(builder.Configuration.GetSection("Qdrant"));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<LangChainSettings>(builder.Configuration.GetSection("LangChainService"));

// Register settings as singleton
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<QdrantSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OllamaSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<LangChainSettings>>().Value);

// Register services
builder.Services.AddScoped<IChorusApiService, ChorusApiService>();
builder.Services.AddScoped<IChorusApplicationService, ChorusApplicationService>();
builder.Services.AddScoped<IChorusCommandService, ChorusCommandService>();
builder.Services.AddScoped<IChorusQueryService, ChorusQueryService>();
builder.Services.AddScoped<ISearchService, ChorusSearchService>();
builder.Services.AddScoped<IAiSearchService, AiSearchService>();
builder.Services.AddScoped<IOllamaRagService, OllamaRagService>();
builder.Services.AddScoped<ITraditionalSearchWithAiService, TraditionalSearchWithAiService>();
builder.Services.AddScoped<IIntelligentSearchService, IntelligentSearchService>();
builder.Services.AddScoped<IChorusRepository>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    // Point to the API's data directory instead of WebPortal's empty data directory
    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "CHAP2.Chorus.Api", "data", "chorus");
    return new DiskChorusRepository(folderPath, logger);
});
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Register AI services
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
{
    var ollamaSettings = builder.Configuration.GetSection("Ollama").Get<OllamaSettings>();
    var timeoutSeconds = ollamaSettings?.TimeoutSeconds ?? 300;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Register LangChain service
builder.Services.AddHttpClient<ILangChainSearchService, LangChainSearchService>(client =>
{
    var langChainSettings = builder.Configuration.GetSection("LangChainService").Get<LangChainSettings>();
    var timeoutSeconds = langChainSettings?.TimeoutSeconds ?? 600;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add middleware to disable response buffering for streaming endpoints
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Home/IntelligentSearchStream"))
    {
        context.Response.Headers.Add("X-Accel-Buffering", "no");
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Add("Pragma", "no-cache");
        context.Response.Headers.Add("Expires", "0");
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); 