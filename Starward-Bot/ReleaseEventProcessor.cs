using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Release;
using System.Text.RegularExpressions;

namespace Starward_Bot;

internal class ReleaseEventProcessor : WebhookEventProcessor
{

    private readonly ILogger<ReleaseEventProcessor> _logger;

    private readonly IMemoryCache _memory;

    private GitHubClient _ghClient;

    private AmazonS3Client _s3Client;

    private HttpClient _httpClient;

    private string _bucketName = Environment.GetEnvironmentVariable("S3_BUCKETNAME")!;


    public ReleaseEventProcessor(ILogger<ReleaseEventProcessor> logger, IMemoryCache memory)
    {
        _logger = logger;
        _memory = memory;
        string endpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT")!;
        string id = Environment.GetEnvironmentVariable("S3_ACCESSKEY_ID")!;
        string secret = Environment.GetEnvironmentVariable("S3_ACCESSKEY_SECRET")!;
        _s3Client = new AmazonS3Client(id, secret, new AmazonS3Config
        {
            ServiceURL = endpoint,
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
        });
    }



    private async Task EnsureAppClient()
    {
        if (_memory.TryGetValue("GithubClient", out GitHubClient? client))
        {
            _ghClient = client!;
            return;
        }
        if (_memory.TryGetValue("GithubHttpClient", out HttpClient? httpClient))
        {
            _httpClient = httpClient!;
            return;
        }
        (client, httpClient) = await GithubUtil.CreateGithubClient();
        _memory.Set("GithubClient", client, DateTimeOffset.Now + TimeSpan.FromMinutes(5));
        _memory.Set("GithubHttpClient", httpClient, DateTimeOffset.Now + TimeSpan.FromMinutes(5));
        _ghClient = client;
        _httpClient = httpClient;
    }



    protected override async ValueTask ProcessReleaseWebhookAsync(WebhookHeaders headers, ReleaseEvent releaseEvent, ReleaseAction action, CancellationToken cancellationToken = default)
    {
        try
        {
            if (releaseEvent.Repository is { FullName: "Scighost/Starward" })
            {
                if (action != ReleaseAction.Deleted && releaseEvent.Sender is { Login: "Scighost" })
                {
                    await EnsureAppClient();
                    string body = releaseEvent.Release.Body;
                    if (body.Contains("<!-- No Replace Attachments -->", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    var matches = Regex.Matches(body, @"https://github\.com/user-attachments/[a-zA-Z0-9/._\-]+");
                    if (matches.Count > 0)
                    {
                        var list = new List<string>();
                        foreach (Match? match in matches)
                        {
                            if (match is null)
                            {
                                continue;
                            }
                            string url = match.Value;
                            list.Add(url);
                            string replace = url.Replace("https://github.com/user-attachments/", "https://starward.scighost.com/github-attachments/");
                            body = body.Replace(url, replace);
                        }
                        if (list.Count > 0)
                        {
                            await Parallel.ForEachAsync(list, async (url, _) =>
                            {
                                using var resourceResponse = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                                resourceResponse.EnsureSuccessStatusCode();
                                using var hs = await resourceResponse.Content.ReadAsStreamAsync();
                                using var ms = new MemoryStream();
                                await hs.CopyToAsync(ms);
                                ms.Position = 0;
                                var request = new PutObjectRequest
                                {
                                    BucketName = _bucketName,
                                    Key = url.Replace("https://github.com/user-attachments/", "github-attachments/"),
                                    ContentType = resourceResponse.Content.Headers.ContentType?.MediaType,
                                    InputStream = ms,
                                };
                                var putResponse = await _s3Client.PutObjectAsync(request);
                                if (putResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                                {
                                    throw new Exception($"Failed to upload attachment {url} to S3: {putResponse.HttpStatusCode}");
                                }
                            });
                            var release = await _ghClient.Repository.Release.Get(releaseEvent.Repository.Id, releaseEvent.Release.Id);
                            var update = release.ToUpdate();
                            update.Body = body;
                            await _ghClient.Repository.Release.Edit(releaseEvent.Repository.Id, releaseEvent.Release.Id, update);
                        }
                    }

                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handle release event: " + ex.ToString());
            throw;
        }
    }



}