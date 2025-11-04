using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Starward_Bot;

public class AutoCloseIssue
{

    private readonly ILogger _logger;

    public AutoCloseIssue(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AutoCloseIssue>();
    }


    [Function("AutoCloseIssue")]
    public async Task RunAsync([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        (var client, _) = await GithubUtil.CreateGithubClient();
        var req = new RepositoryIssueRequest
        {
            SortProperty = IssueSort.Updated,
            SortDirection = SortDirection.Ascending,
            State = ItemStateFilter.Open,
        };
        var issues = await client.Issue.GetAllForRepository("Scighost", "Starward", req);

        foreach (var issue in issues)
        {
            if (issue.PullRequest != null || issue.Labels.Any(x => x.Name is "triage" or "bug" or "enhancement" or "documentation" or "keep open"))
            {
                _logger.LogInformation("Skip issue {issue.Number}.", issue.Number);
                continue;
            }
            if (issue.UpdatedAt != null && issue.UpdatedAt < DateTimeOffset.Now - TimeSpan.FromDays(14))
            {
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                if (issue.Labels.Any(x => x.Name is "wait for reply"))
                {
                    issueUpdate.RemoveLabel("wait for reply");
                    issueUpdate.AddLabel("long time no reply");
                    issueUpdate.StateReason = ItemStateReason.NotPlanned;
                    _logger.LogInformation($"Close issue {issue.Number} as not planned.");
                }
                else if (issue.Title.Contains("[Feature]", StringComparison.OrdinalIgnoreCase))
                {
                    issueUpdate.StateReason = ItemStateReason.NotPlanned;
                    _logger.LogInformation($"Close issue {issue.Number} as not planned.");
                }
                else
                {
                    issueUpdate.StateReason = ItemStateReason.Completed;
                    _logger.LogInformation($"Close issue {issue.Number} as completed.");
                }
                await client.Issue.Comment.Create("Scighost", "Starward", issue.Number, "This issue will be closed due to no reply for more than 14 days.");
                await client.Issue.Update("Scighost", "Starward", issue.Number, issueUpdate);
            }
            else
            {
                _logger.LogInformation($"Skip issue {issue.Number}.");
            }
        }

    }
}
