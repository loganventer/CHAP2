using CHAP2.Common.Resources;
using CHAP2.Common.Interfaces;
using CHAP2.Common.Services;
using CHAP2API.Configuration;
using CHAP2.Common.Enum;
using CommonServices = CHAP2.Common.Services;
using ApiServices = CHAP2API.Services;
using ApiInterfaces = CHAP2API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register custom services
builder.Services.AddScoped<ApiInterfaces.IServices, ApiServices.Services>();
builder.Services.AddScoped<ISearchService, CommonServices.SearchService>();
builder.Services.AddScoped<IRegexHelperService, CommonServices.RegexHelperService>();

// Register generic configuration service
builder.Services.AddSingleton<IConfigurationService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new ConfigurationService(configuration);
});

// Configure settings
builder.Services.Configure<ChorusResourceOptions>(
    builder.Configuration.GetSection("ChorusResource"));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<SearchSettings>(
    builder.Configuration.GetSection("SearchSettings"));
builder.Services.Configure<SlideConversionSettings>(
    builder.Configuration.GetSection("SlideConversionSettings"));

builder.Services.AddSingleton<IChorusResource>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    return new DiskChorusResource(options);
});

// Register SlideToChorusService with values from SlideConversionSettings and IConfigurationService
builder.Services.AddSingleton<ISlideToChorusService>(provider =>
{
    var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SlideConversionSettings>>().Value;
    var configService = provider.GetRequiredService<IConfigurationService>();
    var defaultChorusType = Enum.TryParse<ChorusType>(settings.DefaultChorusType, true, out var ct) ? ct : ChorusType.NotSet;
    var defaultTimeSignature = Enum.TryParse<TimeSignature>(settings.DefaultTimeSignature, true, out var ts) ? ts : TimeSignature.NotSet;
    return new CommonServices.SlideToChorusService(defaultChorusType, defaultTimeSignature, configService);
});

// Register ChorusStandardizationService with IConfigurationService
builder.Services.AddScoped<CommonServices.ChorusStandardizationService>(provider =>
{
    var chorusResource = provider.GetRequiredService<IChorusResource>();
    var configService = provider.GetRequiredService<IConfigurationService>();
    return new CommonServices.ChorusStandardizationService(chorusResource, configService);
});

// Add controllers with a global route prefix
builder.Services.AddControllers(options =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
    options.Conventions.Insert(0, new GlobalRoutePrefixConvention(apiSettings.GlobalRoutePrefix));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
