using CHAP2.WebPortal.Interfaces;
using CHAP2.WebPortal.Services;
using CHAP2.Shared.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Infrastructure.Repositories;
using CHAP2.WebPortal.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Configure HttpClient for API communication using shared configuration
builder.Services.AddCHAP2ApiClient(builder.Configuration);

// Configure settings
builder.Services.Configure<QdrantSettings>(builder.Configuration.GetSection("Qdrant"));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));

// Register settings as singleton
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<QdrantSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OllamaSettings>>().Value);

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
    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "chorus");
    return new DiskChorusRepository(folderPath, logger);
});
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Register AI services
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddHttpClient<IOllamaService, OllamaService>();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); 