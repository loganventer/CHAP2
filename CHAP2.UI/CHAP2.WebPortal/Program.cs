using CHAP2.WebPortal.Auth;
using CHAP2.WebPortal.Interfaces;
using CHAP2.WebPortal.Services;
using CHAP2.WebPortal.Hubs;
using CHAP2.Shared.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Infrastructure.Repositories;
using CHAP2.WebPortal.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// API-forwarding auth: Portal authenticates a user by calling the API,
// stores the returned bearer + refresh tokens in an encrypted cookie,
// then attaches the bearer to every outgoing API call via a delegating
// handler. Cookie auth is the browser session; bearer is the API session.
builder.Services.Configure<ApiAuthSettings>(builder.Configuration.GetSection(ApiAuthSettings.SectionName));
var apiAuthSettings = builder.Configuration.GetSection(ApiAuthSettings.SectionName).Get<ApiAuthSettings>() ?? new ApiAuthSettings();
if (!string.IsNullOrWhiteSpace(apiAuthSettings.DataProtectionKeysPath))
{
    Directory.CreateDirectory(apiAuthSettings.DataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(apiAuthSettings.DataProtectionKeysPath))
        .SetApplicationName("CHAP2-Portal");
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenStore, CookieTokenStore>();
builder.Services.AddScoped<IApiAuthClient, ApiAuthClient>();
builder.Services.AddTransient<BearerTokenHandler>();

builder.Services.AddCHAP2ApiClient(builder.Configuration);

// Append bearer handler to the existing API client so every outbound
// API call carries the current user's access token automatically.
builder.Services.AddHttpClient("CHAP2API")
    .AddHttpMessageHandler<BearerTokenHandler>();

// Auth client uses its own un-authenticated HttpClient (it's how we get
// tokens in the first place).
builder.Services.AddHttpClient(ApiAuthClient.HttpClientName, client =>
{
    var apiBaseUrl = Environment.GetEnvironmentVariable("ApiService__BaseUrl")
        ?? builder.Configuration[ConfigSections.ApiBaseUrl]
        ?? SharedApiSettings.DefaultApiBaseUrl;
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "CHAP2-WebPortal-Auth/1.0");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = apiAuthSettings.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = apiAuthSettings.CookieLifetime;
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<QdrantSettings>(builder.Configuration.GetSection("Qdrant"));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<LangChainSettings>(builder.Configuration.GetSection("LangChainService"));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<QdrantSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OllamaSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<LangChainSettings>>().Value);

builder.Services.AddScoped<IChorusApiService, ChorusApiService>();
builder.Services.AddScoped<IBibleApiService, BibleApiService>();
builder.Services.AddScoped<ISyncApiService, SyncApiService>();
builder.Services.AddScoped<ISetlistApiService, SetlistApiService>();
builder.Services.AddScoped<IUserPreferencesApiService, UserPreferencesApiService>();
builder.Services.AddScoped<IUserAdminApiService, UserAdminApiService>();
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
    var configuration = provider.GetRequiredService<IConfiguration>();
    var chorusDataPath = configuration["ChorusDataPath"] ?? "data/chorus";
    var folderPath = Path.IsPathRooted(chorusDataPath)
        ? chorusDataPath
        : Path.Combine(Directory.GetCurrentDirectory(), chorusDataPath);
    var innerRepo = new DiskChorusRepository(folderPath, logger);
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var cacheLogger = provider.GetRequiredService<ILogger<CachedChorusRepository>>();
    return new CachedChorusRepository(innerRepo, cache, cacheLogger);
});
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
{
    var ollamaSettings = builder.Configuration.GetSection("Ollama").Get<OllamaSettings>();
    var timeoutSeconds = ollamaSettings?.TimeoutSeconds ?? 300;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

builder.Services.AddHttpClient<ILangChainSearchService, LangChainSearchService>(client =>
{
    var langChainSettings = builder.Configuration.GetSection("LangChainService").Get<LangChainSettings>();
    var timeoutSeconds = langChainSettings?.TimeoutSeconds ?? 600;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

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
                  .AllowCredentials();
        }
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Home/IntelligentSearchStream"))
    {
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<BearerTokenRefreshMiddleware>();

app.MapHub<ChorusHub>("/chorusHub").RequireAuthorization();

app.MapControllerRoute(
    name: "sync",
    pattern: "sync",
    defaults: new { controller = "Home", action = "MobileSync" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
