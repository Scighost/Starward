using Octokit;
using System.Net;

namespace Starward_Bot;

internal static class GithubUtil
{

    public static async Task<(GitHubClient, HttpClient)> CreateGithubClient()
    {
        var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(Environment.GetEnvironmentVariable("GITHUB_PEM")),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = 374100,
                    ExpirationSeconds = 360
                });
        var jwtToken = generator.CreateEncodedJwtToken();
        var appClient = new GitHubClient(new ProductHeaderValue("Starward-Bot"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        var installation = await appClient!.GitHubApps.GetRepositoryInstallationForCurrent("Scighost", "Starward");
        var response = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

        var installationClient = new GitHubClient(new ProductHeaderValue("Starward-Bot"))
        {
            Credentials = new Credentials(response.Token)
        };
        var httpClient = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Starward-Bot");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", response.Token);
        return (installationClient, httpClient);
    }

}
