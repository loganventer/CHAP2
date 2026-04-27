using System.Threading.RateLimiting;
using CHAP2.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CHAP2.Chorus.Api.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddChap2RateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));

        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
        var permitLimit = Math.Max(1, settings.AuthRequestsPerMinute);
        var queueLimit = Math.Max(0, settings.AuthQueueLimit);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                ctx.HttpContext.Response.ContentType = "application/problem+json";
                await ctx.HttpContext.Response.WriteAsync(
                    "{\"type\":\"https://tools.ietf.org/html/rfc6585#section-4\",\"title\":\"Too Many Requests\",\"status\":429}",
                    ct);
            };

            options.AddPolicy(RateLimitPolicyNames.AuthAnonymous, httpContext =>
            {
                var key = ResolvePartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true,
                });
            });
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
}
