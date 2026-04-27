using CHAP2.Chorus.Api.Configuration;
using CHAP2.Chorus.Api.HostedServices;
using CHAP2.Chorus.Api.Identity;
using CHAP2.Chorus.Api.RateLimiting;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Application.EventHandlers;
using CHAP2.Domain.Events;
using CHAP2.Infrastructure.DependencyInjection;
using CHAP2.Infrastructure.GitHub;
using CHAP2.Infrastructure.Identity;
using CHAP2.Infrastructure.Repositories;
using CHAP2.Infrastructure.Repositories.Bible;
using CHAP2.Shared.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

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

builder.Services.AddSingleton<DiskChorusRepository>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<DiskChorusRepository>>();
    return new DiskChorusRepository(options.FolderPath, logger);
});

builder.Services.AddSingleton<IChorusRepository>(provider =>
{
    var innerRepository = provider.GetRequiredService<DiskChorusRepository>();
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<CachedChorusRepository>>();
    return new CachedChorusRepository(innerRepository, cache, logger);
});

// Forward the segregated chorus interfaces to the same composite
// instance so consumers can depend on the narrowest interface they need
// (mirrors the bible repository wiring below).
builder.Services.AddSingleton<IChorusReadRepository>(p => p.GetRequiredService<IChorusRepository>());
builder.Services.AddSingleton<IChorusWriteRepository>(p => p.GetRequiredService<IChorusRepository>());
builder.Services.AddSingleton<IChorusSearchRepository>(p => p.GetRequiredService<IChorusRepository>());

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
builder.Services.AddScoped<IDomainEventHandler<ChorusCreatedEvent>, ChorusCreatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusUpdatedEvent>, ChorusUpdatedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusDeletedEvent>, ChorusDeletedEventHandler>();

// Per-edit GitHub push handlers: chorus CRUD lands on the edits branch
// immediately, no waiting for the daily mirror. Composed alongside the
// log-only handlers above (multiple handlers per event by design).
builder.Services.AddSingleton<IChorusFileGateway>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    return new DiskChorusFileGateway(options.FolderPath);
});
builder.Services.AddScoped<IDomainEventHandler<ChorusCreatedEvent>, ChorusCreatedGitPushHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusUpdatedEvent>, ChorusUpdatedGitPushHandler>();
builder.Services.AddScoped<IDomainEventHandler<ChorusDeletedEvent>, ChorusDeletedGitPushHandler>();

builder.Services.AddScoped<ISetlistOwnershipPolicy, SetlistOwnershipPolicy>();
builder.Services.AddScoped<ISetlistQueryService, SetlistQueryService>();
builder.Services.AddScoped<ISetlistCommandService, SetlistCommandService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();

builder.Services.AddCHAP2Persistence(builder.Configuration);

var identitySettings = builder.Configuration.GetSection(IdentitySettings.SectionName).Get<IdentitySettings>() ?? new IdentitySettings();
if (!string.IsNullOrWhiteSpace(identitySettings.DataProtectionKeysPath))
{
    Directory.CreateDirectory(identitySettings.DataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(identitySettings.DataProtectionKeysPath))
        .SetApplicationName("CHAP2");
}

builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = identitySettings.BearerTokenExpiration;
        options.RefreshTokenExpiration = identitySettings.RefreshTokenExpiration;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole(RoleNames.Admin))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddChap2RateLimiting(builder.Configuration);

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
        autoCreateFrom: opts.Value.MainBranch,
        remotePathPrefix: opts.Value.RemotePathPrefix,
        authorName: opts.Value.AuthorName,
        authorEmail: opts.Value.AuthorEmail,
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
    return new ChorusGitSyncOrchestrator(sync, opts.DataDirectory, opts.Branch, opts.MainBranch, logger);
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
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<IChorusDiskBootstrapper>()
        .EnsureReadyAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseResponseCompression();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapChap2Identity();

app.MapControllers();
app.Run();

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var v in values) if (!string.IsNullOrWhiteSpace(v)) return v;
    return null;
}
