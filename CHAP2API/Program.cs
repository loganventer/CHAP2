using CHAP2.Common.Resources;
using CHAP2.Common.Interfaces;
using CommonServices = CHAP2.Common.Services;
using ApiServices = CHAP2API.Services;
using ApiInterfaces = CHAP2API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register custom services
builder.Services.AddScoped<ApiInterfaces.IServices, ApiServices.Services>();
builder.Services.AddSingleton<ISlideToChorusService, CommonServices.SlideToChorusService>();

// Configure chorus resources
builder.Services.Configure<ChorusResourceOptions>(
    builder.Configuration.GetSection("ChorusResource"));
builder.Services.AddSingleton<IChorusResource>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChorusResourceOptions>>().Value;
    return new DiskChorusResource(options);
});

// Add controllers with a global route prefix
builder.Services.AddControllers(options =>
{
    options.Conventions.Insert(0, new GlobalRoutePrefixConvention("api"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
