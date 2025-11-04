using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.Release;
using Octokit.Webhooks.Models;
using System.Text.RegularExpressions;

namespace Starward_Bot;

internal class MyProcessor : WebhookEventProcessor
{

    private readonly ILogger<MyProcessor> _logger;

    private readonly IMemoryCache _memory;

    private GitHubClient _ghClient;

    private AmazonS3Client _s3Client;

    private HttpClient _httpClient;

    private string _bucketName = Environment.GetEnvironmentVariable("S3_BUCKETNAME")!;


    public MyProcessor(ILogger<MyProcessor> logger, IMemoryCache memory)
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



    protected override async ValueTask ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action, CancellationToken cancellationToken = default)
    {
        try
        {
            if (issuesEvent.Repository is { FullName: "Scighost/Starward" })
            {
                _logger.LogInformation("Process issue event: {Action} for issue #{Number} in {RepositoryFullName}", action, issuesEvent.Issue.Number, issuesEvent.Repository.FullName);
                await EnsureAppClient();

                if (action == IssuesAction.Opened || action == IssuesAction.Reopened)
                {
                    await OnIssueOpenedAsync(headers, issuesEvent, action);
                }

                if (action == IssuesAction.Closed)
                {
                    await OnIssueClosedAsync(headers, issuesEvent, action);
                }

                if (action == IssuesAction.Labeled)
                {
                    await OnIssueLabeledAsync(headers, issuesEvent, action);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handle issue event: " + ex.ToString());
            throw;
        }
    }




    private async Task OnIssueOpenedAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        bool close = false;
        string title = issuesEvent.Issue.Title;
        string? body = issuesEvent.Issue.Body;

        if (issuesEvent.Issue.Labels.Any(x => x.Name == "keep open"))
        {
            return;
        }

        if (!close && string.IsNullOrWhiteSpace(title))
        {
            _logger.LogInformation("Issue #{Number} has no title, closing it.", issuesEvent.Issue.Number);
            close = true;
        }
        if (!close && string.IsNullOrWhiteSpace(body))
        {
            _logger.LogInformation("Issue #{Number} has no body, closing it.", issuesEvent.Issue.Number);
            close = true;
        }
        if (!close && string.IsNullOrWhiteSpace(Regex.Match(title, @"(\[[^\]]*\])?\s*(.*)").Groups[2].Value))
        {
            _logger.LogInformation("Issue #{Number} has no meaningful title, closing it.", issuesEvent.Issue.Number);
            close = true;
        }

        if (close)
        {
            var issueUpdate = new IssueUpdate { Title = title };
            issueUpdate.AddLabel("invalid");
            issueUpdate.State = ItemState.Closed;
            issueUpdate.StateReason = ItemStateReason.NotPlanned;
            await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "This issue will be closed for no title or content.");
            await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
            _logger.LogInformation("Issue #{Number} has been closed due to invalid content.", issuesEvent.Issue.Number);
        }
    }



    private async Task OnIssueClosedAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        if (issuesEvent.Issue.Labels.Any(x => x.Name == "triage"))
        {
            var issue = await _ghClient.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
            var issueUpdate = issue.ToUpdate();
            issueUpdate.RemoveLabel("triage");
            await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
            _logger.LogInformation("Issue #{Number} has been updated to remove 'triage' label.", issuesEvent.Issue.Number);
        }
    }



    private async Task OnIssueLabeledAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        if (issuesEvent.Sender is { Login: "Scighost" })
        {
            if (issuesEvent.Issue.State?.Value is IssueState.Closed)
            {
                return;
            }
            else if (issuesEvent.Issue.Labels.Any(x => x.Name is "invalid"))
            {
                _logger.LogInformation("Issue #{Number} has been labeled as 'invalid', closing it.", issuesEvent.Issue.Number);
                var issue = await _ghClient.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "This issue will be closed for something invalid.\n由于存在无效内容，该 issue 将被关闭。");
                await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
                _logger.LogInformation("Issue #{Number} has been closed due to invalid content.", issuesEvent.Issue.Number);
            }
            else if (issuesEvent.Issue.Labels.Any(x => x.Name is "duplicate"))
            {
                _logger.LogInformation("Issue #{Number} has been labeled as 'duplicate', closing it.", issuesEvent.Issue.Number);
                var issue = await _ghClient.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "This issue will be closed for duplicate.\n重复的 issue 将被关闭。");
                await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
                _logger.LogInformation("Issue #{Number} has been closed due to duplicate content.", issuesEvent.Issue.Number);
            }
            else if (issuesEvent.Issue.Labels.Any(x => x.Name is "need more info"))
            {
                _logger.LogInformation("Issue #{Number} has been labeled as 'need more info', closing it.", issuesEvent.Issue.Number);
                var issue = await _ghClient.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "Sorry, based on the information you provided, the developer is unable to resolve this issue.\n很抱歉，根据您提供的信息，开发者无法解决此问题。");
                await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
                _logger.LogInformation("Issue #{Number} has been closed due to insufficient information.", issuesEvent.Issue.Number);
            }
            else if (issuesEvent.Issue.Labels.Any(x => x.Name is "do not understand"))
            {
                _logger.LogInformation("Issue #{Number} has been labeled as 'do not understand', closing it.", issuesEvent.Issue.Number);
                var issue = await _ghClient.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "Thank you for your feedback or suggestions, but we're sorry that the developer couldn't understand what you posted.\n感谢您的反馈或建议，但是很抱歉，开发者无法理解您发布的内容。");
                await _ghClient.Issue.Update("Scighost", "Starward", (int)issuesEvent.Issue.Number, issueUpdate);
                _logger.LogInformation("Issue #{Number} has been closed due to misunderstanding.", issuesEvent.Issue.Number);
            }
        }
    }




    protected override async ValueTask ProcessIssueCommentWebhookAsync(WebhookHeaders headers, IssueCommentEvent issueCommentEvent, IssueCommentAction action, CancellationToken cancellationToken = default)
    {
        return;
        await EnsureAppClient();
        if (action == IssueCommentAction.Deleted && issueCommentEvent.Repository is { FullName: "Scighost/Starward" })
        {
            if (issueCommentEvent.Sender?.Login is "Scighost")
            {
                return;
            }
            string body = $"""
                > @{issueCommentEvent.Comment.User.Login} deleted the following comment
                
                {issueCommentEvent.Comment.Body}
                """;
            await _ghClient.Issue.Comment.Create("Scighost", "Starward", (int)issueCommentEvent.Issue.Number, body);
        }
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
                            string replace = url.Replace("https://github.com/user-attachments/", "https://starward-static.scighost.com/github-attachments/");
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
