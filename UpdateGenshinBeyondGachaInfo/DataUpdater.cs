using SixLabors.ImageSharp;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace UpdateGenshinBeyondGachaInfo;

public class DataUpdater
{

#if DEBUG
    static string localFolder = AppContext.BaseDirectory;
#else
    static string localFolder = "/mnt/oss/game-assets/genshin/";
#endif

    const string ApiUrl = "https://api.hakush.in/gi/data/zh/beyond/item.json";


    public static async Task UpdateAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.Combine(localFolder, "beyond"));
        var client = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        var dic = await client.GetFromJsonAsync<Dictionary<int, GenshinBeyondGachaInfo>>(ApiUrl, cancellationToken);

        Lock writeLock = new();
        await Parallel.ForEachAsync(dic.Values, cancellationToken, async (item, cancellation) =>
        {
            string name = item.Icon + ".webp";
            string path = Path.Combine(localFolder, "beyond", name);
            if (forceUpdate || !File.Exists(path))
            {
                string url = $"https://api.hakush.in/gi/UI/{item.Icon}.webp";
                try
                {
                    var bytes = await client.GetByteArrayAsync(url, cancellation);
                    lock (writeLock)
                    {
                        File.WriteAllBytes(path, bytes);
                    }
                    Console.WriteLine($"Downloaded: {url}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download {url}: {ex.Message}");
                }
            }
        });

        foreach (var item in dic)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string webp = Path.Combine(localFolder, "beyond", item.Value.Icon + ".webp");
            string png = Path.Combine(localFolder, "beyond", item.Value.Icon + ".png");
            if (forceUpdate || !File.Exists(png))
            {
                if (File.Exists(webp))
                {
                    using var image = Image.Load(webp);
                    image.SaveAsPng(png);
                    Console.WriteLine($"Converted: {item.Value.Icon}.webp to {item.Value.Icon}.png");
                }
                else
                {
                    Console.WriteLine($"WebP file not found: {webp}");
                }
            }
            item.Value.Id = item.Key;
            item.Value.Icon = $"https://starward-static.scighost.com/game-assets/genshin/beyond/{item.Value.Icon}.png";
        }

        string outputJson = Path.Combine(localFolder, "GenshinBeyondGachaInfo.json");
        string jsonString = JsonSerializer.Serialize(dic.Values.OrderBy(x => x.Id).ToList(), new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        await File.WriteAllTextAsync(outputJson, jsonString);

        Console.WriteLine("GenshinBeyondGachaInfo.json has been updated.");
    }







    class GenshinBeyondGachaInfo
    {

        public int Id { get; set; }

        public string Name { get; set; }

        public int Rank { get; set; }

        public string Icon { get; set; }

    }


}
