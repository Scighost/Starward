using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

namespace Starward.GameManagement.Service;

internal class HttpService
{

    private const int MAX_ATTEMPT = 2;

    private readonly ILogger<HttpService> _logger;

    private readonly IHttpClientFactory _httpClientFactor;

    private readonly SlidingWindowRateLimiter _rateLimiter;


    public HttpService(ILogger<HttpService> logger, IHttpClientFactory httpClientFactor)
    {
        _logger = logger;
        _httpClientFactor = httpClientFactor;
        _rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 10,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            SegmentsPerWindow = 1,
            Window = TimeSpan.FromMinutes(1),
        });
    }



    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellation)
    {
        return await GetAsync(url, null, cancellation);
    }


    public async Task<HttpResponseMessage> GetAsync(string url, RangeHeaderValue? range, CancellationToken cancellation)
    {
        using HttpClient httpClient = _httpClientFactor.CreateClient();
        HttpResponseMessage result = null!;
        for (int i = 0; i < MAX_ATTEMPT; i++)
        {
            HttpRequestMessage request = new(HttpMethod.Get, url) { VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
            request.Headers.Range = range;
            HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                response.EnsureSuccessStatusCode();
            }
            result?.Dispose();
            result = response;
            if (response.RequestMessage?.RequestUri?.IsDefaultPort ?? true)
            {
                break;
            }
            using RateLimitLease lease = _rateLimiter.AttemptAcquire();
            if (lease.IsAcquired)
            {
                await Task.Delay(300, cancellation);
                continue;
            }
            break;
        }
        return result;
    }


}