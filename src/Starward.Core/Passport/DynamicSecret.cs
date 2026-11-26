using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Starward.Core.Passport;

public static class DynamicSecret
{

    public static string LifeCycleId { get; } = Guid.NewGuid().ToString();


    private const string AppSalt1_CN = "24cf11fb14a84f0683e7cadf7c788ba2";
    private const string AppSalt2_CN = "t0qEgfub6cvueAPgR5m9aQWWVciEer7v";
    private const string WebSalt1_CN = "8fb73233f73e200aa1f818b5705d4442";
    private const string WebSalt2_CN = "xV8v4Qu54lUKrEYFZkJhB8cuOh9Asafs";
    private const string PassportSalt_CN = "JwYDpKvLj6MrMqqYU6jTKF17KNO2PXoS";

    private const string AppSalt1_OS = "599uqkwc0dlqu3h6epzjzfhgyyrd44ae";
    private const string AppSalt2_OS = "okr4obncj8bw5a65hbnn5oo6ixjc3l9w";
    private const string WebSalt1_OS = "rk4xg2hakoi26nljpr099fv9fck1ah10";
    private const string WebSalt2_OS = "h4c1d6ywfq5bsbnbhm1bzq7bxzzv6srt";
    private const string PassportSalt_OS = "IZPgfb0dRPtBeLuFkdDznSZ6f4wWt6y2";



    private static string GetDS1RandomString()
    {
        return RandomNumberGenerator.GetString("0123456789abcdefghijklmnop", 6);
    }

    private static int GetDS2RandomNumber()
    {
        return Random.Shared.Next(10_0000, 20_0000 + 1);
    }

    private static string GetPassportDSRandomString()
    {
        return RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890", 6);
    }

    private static string RegionNotSupported(ServerRegion region)
    {
        throw new NotSupportedException($"Region {region} is not supported.");
    }


    private static string CreateDS1(string salt)
    {
        long t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string r = GetDS1RandomString();
        byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes($"salt={salt}&t={t}&r={r}"));
        return $"{t},{r},{Convert.ToHexStringLower(md5)}";
    }


    private static string CreateDS2(string salt, string url)
    {
        long t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int r = GetDS2RandomNumber();
        string b = "";
        string q = "";
        int index = url.IndexOf('?');
        if (index >= 0 && url.Length > index + 1)
        {
            string query = url.Substring(index + 1);
            q = string.Join("&", query.Split('&').Order());
        }
        byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes($"salt={salt}&t={t}&r={r}&b={b}&q={q}"));
        return $"{t},{r},{Convert.ToHexStringLower(md5)}";
    }


    public static string CreateAppDS1(ServerRegion region)
    {
        string salt = region switch
        {
            ServerRegion.HoYoverse => AppSalt1_OS,
            ServerRegion.miHoYo => AppSalt1_CN,
            _ => RegionNotSupported(region),
        };
        return CreateDS1(salt);
    }


    public static string CreateAppDS2(ServerRegion region, string url)
    {
        string salt = region switch
        {
            ServerRegion.HoYoverse => AppSalt2_OS,
            ServerRegion.miHoYo => AppSalt2_CN,
            _ => RegionNotSupported(region),
        };
        return CreateDS2(salt, url);
    }


    public static string CreateWebDS1(ServerRegion region)
    {
        string salt = region switch
        {
            ServerRegion.HoYoverse => WebSalt1_OS,
            ServerRegion.miHoYo => WebSalt1_CN,
            _ => RegionNotSupported(region),
        };
        return CreateDS1(salt);
    }


    public static string CreateWebDS2(ServerRegion region, string url)
    {
        string salt = region switch
        {
            ServerRegion.HoYoverse => WebSalt2_OS,
            ServerRegion.miHoYo => WebSalt2_CN,
            _ => RegionNotSupported(region),
        };
        return CreateDS2(salt, url);
    }


    public static string CreatePassportDS<T>(ServerRegion region, T body)
    {
        string salt = region switch
        {
            ServerRegion.HoYoverse => PassportSalt_OS,
            ServerRegion.miHoYo => PassportSalt_CN,
            _ => RegionNotSupported(region),
        };

        long t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string r = GetPassportDSRandomString();
        string b = JsonSerializer.Serialize(body, typeof(T), PassportJsonContext.Default);
        string q = "";
        byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes($"salt={salt}&t={t}&r={r}&b={b}&q={q}"));
        return $"{t},{r},{Convert.ToHexStringLower(md5)}";
    }


}