using System.Text.Json.Nodes;
using UpdateGenshinBeyondGachaInfo;

var builder = WebApplication.CreateBuilder(args);
#if !DEBUG
builder.WebHost.UseUrls("http://0.0.0.0:9000");
#endif

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/invoke", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    bool forceUpdate = false;
    try
    {
        JsonNode? node = await JsonNode.ParseAsync(request.Body);
        if (node?["payload"]?.ToString()?.Contains("forceupdate", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            forceUpdate = true;
        }
    }
    catch { }
    await DataUpdater.UpdateAsync(forceUpdate);
});

app.Run();


