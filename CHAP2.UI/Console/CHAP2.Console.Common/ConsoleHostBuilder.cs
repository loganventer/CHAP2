using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CHAP2.Console.Common.Configuration;
using CHAP2.Shared.Configuration;

namespace CHAP2.Console.Common;

public static class ConsoleHostBuilder
{
    public static IHost Build(
        Action<IServiceCollection>? configureServices = null,
        Action<IConfigurationBuilder>? configureConfig = null)
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddCommandLine(Environment.GetCommandLineArgs());
        configureConfig?.Invoke(configBuilder);
        var configuration = configBuilder.Build();

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddCHAP2ApiClient(configuration);
                services.Configure<ConsoleSettings>(configuration.GetSection(ConfigSections.ConsoleSettings));
                services.Configure<ConsoleApiSettings>(configuration.GetSection(ConfigSections.ConsoleApiSettings));
                configureServices?.Invoke(services);
            });

        return hostBuilder.Build();
    }
} 