using BuildTool;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;


var url = "https://starward-static.scighost.com/release/manifest/manifest_0.16.0-preview.1_x64_portable_diff_0.15.5.json";


var client = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
{
    DefaultRequestVersion = HttpVersion.Version11,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
};


var manifest = await client.GetFromJsonAsync<ReleaseManifest>(url);
var list = manifest.Files.Where(x => x.Patch?.Id is not null).ToList();


var client1 = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
{
    DefaultRequestVersion = HttpVersion.Version11,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
};
var client2 = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
{
    DefaultRequestVersion = HttpVersion.Version11,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
};



{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 1; i++)
    {
        await Parallel.ForEachAsync(list, async (item, _) =>
        {
            //using var a = await client2.GetAsync($"https://speed.cloudflare.com/__down?during=download&bytes={item.Patch.PatchSize}");
            using var a = await client1.GetAsync(manifest.UrlPrefix + item.Patch.Id);
        });
        //Console.WriteLine(a.Version);
    }
    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.Elapsed.TotalSeconds} s");
}


{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 1; i++)
    {
        //using var a = await client1.GetAsync($"https://speed.cloudflare.com/__down?during=download&bytes={manifest.DiffSize}");
        //using var a = await client2.GetAsync(manifest.UrlPrefix + "6036ca47fabeed8b_39923cfdad272169c217406a60214ffe9bd6c1b3bd396a3eff94b781bd8d3376");
        using var a = await client2.GetAsync(manifest.UrlPrefix + "0a302c963d8454cf_a404fc640c4e73562e387233af70f74fb9e526841bf6f6f6c9d95089acbbcae7");
        using var b = await client2.GetAsync(manifest.UrlPrefix + "0a302c963d8454cf_a404fc640c4e73562e387233af70f74fb9e526841bf6f6f6c9d95089acbbcae7");
        Console.WriteLine(a.Version);
    }
    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.Elapsed.TotalSeconds} s");
}






























return;

//ILookup<string, string> aaa = await ZeroconfResolver.BrowseDomainsAsync();
//foreach (var item in aaa)
//{
//    Console.WriteLine($"{item.Key}:");
//    foreach (var subitem in item)
//    {
//        Console.WriteLine($"-- {subitem}");
//    }


//    IReadOnlyList<IZeroconfHost> response = await ZeroconfResolver.ResolveAsync(item.Key);
//    foreach (var host in response)
//    {
//        Console.WriteLine("id: " + host.Id);
//        Console.WriteLine("display name: " + host.DisplayName);
//        Console.WriteLine("ip: " + host.IPAddress);
//        Console.WriteLine("service: ");
//        foreach (var service in host.Services)
//        {
//            Console.WriteLine($"-- {service.Key}:");
//            Console.WriteLine($"---- Name {service.Value.Name}");
//            Console.WriteLine($"---- ServiceName {service.Value.ServiceName}");
//            Console.WriteLine($"---- Port {service.Value.Port}");
//            Console.WriteLine($"---- Ttl {service.Value.Ttl}");
//            Console.WriteLine($"---- Properties {service.Value.Properties}");
//        }
//    }


//    Console.WriteLine("-----------");

//}