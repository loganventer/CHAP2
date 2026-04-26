using CHAP2.Chorus.Api.Configuration;
using CHAP2.Chorus.Api.HostedServices;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Application.EventHandlers;
using CHAP2.Domain.Events;
using CHAP2.Infrastructure.GitHub;
using CHAP2.Infrastructure.Repositories;
using CHAP2.Infrastructure.Repositories.Bible;
using CHAP2.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Note: No authentication configured - this API is designed for internal/local network use only
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
builder.Services.Configure<BibleResourceOptions>(
    builder.Configuration.GetSection("BibleResource"));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<SearchSettings>(
    builder.Configuration.GetSection("SearchSettings"));
builder.Services.Configure<SlideConversionSettings>(
    builder.Configuration.GetSection("SlideConversionSettings"));
builder.Services.Configure<GitSyncOptions>(
    builder.Configuration.GetSection("GitSync"));

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

// Bible repositories — disk reader wrapped by an in-memory caching decorator.
builder.Services.AddSingleton<DiskBibleRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BibleResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskBibleRepository>>();
    return new DiskBibleRepository(options.FolderPath, logger);
});
builder.Services.AddSingleton<IBibleRepository>(provider =>
{
    var inner = provider.GetRequiredService<DiskBibleRepository>();
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<CachedBibleRepository>>();
    return new CachedBibleRepository(inner, cache, logger);
});
// Forward the segregated interfaces to the same composite instance so
// consumers (e.g. BibleReferenceParser) can depend on the narrowest
// interface they need without DI failing to resolve them.
builder.Services.AddSingleton<IBibleBookRepository>(p => p.GetRequiredService<IBibleRepository>());
builder.Services.AddSingleton<IBibleChapterRepository>(p => p.GetRequiredService<IBibleRepository>());
builder.Services.AddSingleton<IBibleVerseSearchRepository>(p => p.GetRequiredService<IBibleRepository>());

builder.Services.AddScoped<IBibleReferenceParser, BibleReferenceParser>();
builder.Services.AddScoped<IBibleQueryService, BibleQueryService>();

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

// Chorus sync over the GitHub Git Data API. Mirrors /var/data/{id}.json
// to {RemotePathPrefix}/{id}.json on the configured branch. One commit
// per sync regardless of file count. No git binary, no .git on disk.
//
// The token accessor re-reads the PAT from options on every request so
// a future PAT-rotation flow takes effect immediately.
builder.Services.AddHttpClient(nameof(GitHubChorusSync), client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddSingleton<IChorusGitHubSync>(provider =>
{
    var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GitHubChorusSync));
    var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GitSyncOptions>>();
    var logger = provider.GetRequiredService<ILogger<GitHubChorusSync>>();
    return new GitHubChorusSync(
        httpClient: http,
        owner: opts.Value.Owner,
        repo: opts.Value.Repo,
        branch: opts.Value.Branch,
        remotePathPrefix: opts.Value.RemotePathPrefix,
        authorName: opts.Value.AuthorName,
        authorEmail: opts.Value.AuthorEmail,
        // Late-bound token: tries the structured config binding first,
        // then falls back to whichever common GitHub-PAT env var Render
        // has set. Means the existing secret on the dashboard works
        // regardless of the exact env-var name.
        tokenAccessor: () =>
            FirstNonEmpty(
                opts.Value.GitHubToken,
                Environment.GetEnvironmentVariable("GitSync__GitHubToken"),
                Environment.GetEnvironmentVariable("GITHUB_TOKEN"),
                Environment.GetEnvironmentVariable("GITHUB_PAT"),
                Environment.GetEnvironmentVariable("GH_TOKEN"),
                Environment.GetEnvironmentVariable("GH_PAT")),
        logger: logger);
});

builder.Services.AddSingleton<IChorusGitSyncOrchestrator>(provider =>
{
    var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GitSyncOptions>>().Value;
    var sync = provider.GetRequiredService<IChorusGitHubSync>();
    var logger = provider.GetRequiredService<ILogger<ChorusGitSyncOrchestrator>>();
    return new ChorusGitSyncOrchestrator(sync, opts.DataDirectory, logger);
});
builder.Services.AddSingleton<IChorusDiskBootstrapper>(provider =>
{
    var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GitSyncOptions>>().Value;
    var sync = provider.GetRequiredService<IChorusGitHubSync>();
    var logger = provider.GetRequiredService<ILogger<ChorusDiskBootstrapper>>();
    return new ChorusDiskBootstrapper(opts.Enabled, opts.DataDirectory, opts.ImageSeedDirectory, sync, logger);
});
builder.Services.AddHostedService<ChorusGitSyncBackgroundService>();

builder.Services.AddControllers(options =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
    options.Conventions.Insert(0, new GlobalRoutePrefixConvention(apiSettings.GlobalRoutePrefix));
});

builder.Services.AddResponseCompression();

// Render terminates HTTPS at its edge proxy and forwards traffic to
// the container as plain HTTP, with the original scheme in the
// X-Forwarded-Proto header. UseForwardedHeaders teaches Kestrel to
// trust that header so HttpsRedirection (and Request.Scheme generally)
// reflect the real client scheme instead of treating every request as
// HTTP and redirecting endlessly.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Render's proxy is on the same Docker network; we cannot enumerate
    // its addresses up-front, so trust any proxy.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Bootstrap the chorus disk before serving traffic so the very first
// request finds a populated working tree (clones the remote when git
// sync is enabled, seeds from the image otherwise). Idempotent.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<IChorusDiskBootstrapper>()
        .EnsureReadyAsync();
}

// Forwarded headers must be the FIRST middleware so subsequent ones
// see the corrected scheme and remote IP.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Skip HTTPS redirection in production: Render handles HTTPS at the
// edge and the container only listens on HTTP, so an in-app redirect
// would loop or break health checks.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseResponseCompression();
app.MapControllers();
app.Run();

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var v in values) if (!string.IsNullOrWhiteSpace(v)) return v;
    return null;
}
