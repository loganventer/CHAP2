using CHAP2.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Infrastructure.Identity;

public class IdentitySeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentitySettings _settings;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<IdentitySettings> settings,
        ILogger<IdentitySeeder> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, RoleNames.Admin);
        await EnsureRoleAsync(roleManager, RoleNames.User);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureSeedAdminAsync(userManager);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName)) return;
        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create role '{roleName}': {Describe(result)}");
        _logger.LogInformation("Created role {Role}", roleName);
    }

    private async Task EnsureSeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        var existing = await userManager.FindByNameAsync(_settings.SeedAdminUserName);
        if (existing is not null)
        {
            if (_settings.ForceReseedAdmin)
            {
                await ForceResetAdminAsync(userManager, existing);
            }
            else
            {
                _logger.LogDebug("Seed admin {UserName} already exists", _settings.SeedAdminUserName);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.SeedAdminInitialPassword))
            throw new InvalidOperationException(
                $"{IdentitySettings.SectionName}:{nameof(IdentitySettings.SeedAdminInitialPassword)} is not configured.");

        var admin = new ApplicationUser
        {
            UserName = _settings.SeedAdminUserName,
            Email = _settings.SeedAdminEmail,
            EmailConfirmed = true,
            MustChangePassword = true,
        };

        var create = await userManager.CreateAsync(admin, _settings.SeedAdminInitialPassword);
        if (!create.Succeeded)
            throw new InvalidOperationException($"Failed to create seed admin: {Describe(create)}");

        var addRole = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!addRole.Succeeded)
            throw new InvalidOperationException($"Failed to grant Admin role to seed admin: {Describe(addRole)}");

        _logger.LogInformation("Seeded admin user {UserName}", _settings.SeedAdminUserName);
    }

    private async Task ForceResetAdminAsync(UserManager<ApplicationUser> userManager, ApplicationUser admin)
    {
        if (string.IsNullOrWhiteSpace(_settings.SeedAdminInitialPassword))
            throw new InvalidOperationException(
                $"ForceReseedAdmin is true but {IdentitySettings.SectionName}:{nameof(IdentitySettings.SeedAdminInitialPassword)} is not configured.");

        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var reset = await userManager.ResetPasswordAsync(admin, token, _settings.SeedAdminInitialPassword);
        if (!reset.Succeeded)
            throw new InvalidOperationException($"Failed to reset seed admin password: {Describe(reset)}");

        if (await userManager.IsLockedOutAsync(admin))
            await userManager.SetLockoutEndDateAsync(admin, null);
        await userManager.ResetAccessFailedCountAsync(admin);

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            var grant = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            if (!grant.Succeeded)
                throw new InvalidOperationException($"Failed to re-grant Admin role: {Describe(grant)}");
        }

        admin.MustChangePassword = true;
        await userManager.UpdateAsync(admin);

        _logger.LogWarning(
            "ForceReseedAdmin reset password and lockout for {UserName}. Set Identity:ForceReseedAdmin=false after recovery.",
            _settings.SeedAdminUserName);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
}
