using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.DeviceFp;

public class AndroidDeviceInfo
{

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; }

    [JsonPropertyName("seed_id")]
    public string SeedId { get; set; }

    [JsonPropertyName("seed_time")]
    public string SeedTime { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "2";

    [JsonPropertyName("device_fp")]
    public string DeviceFp { get; set; }

    [JsonPropertyName("app_name")]
    public string AppName { get; set; }

    [JsonPropertyName("ext_fields")]
    public AndroidDeviceExtFields ExtFields { get; set; } = new();

    [JsonPropertyName("bbs_device_id")]
    public string BbsDeviceId { get; set; }

    [JsonPropertyName("hoyolab_device_id")]
    public string HoyolabDeviceId { get; set; }

    [JsonPropertyName("bbs_device_fp")]
    public string BbsDeviceFp { get; set; }

    [JsonPropertyName("hoyolab_device_fp")]
    public string HoyolabDeviceFp { get; set; }



    public DeviceFpRequest ToRequest()
    {
        return new DeviceFpRequest
        {
            DeviceId = DeviceId,
            SeedId = SeedId,
            SeedTime = SeedTime,
            Platform = Platform,
            DeviceFp = DeviceFp,
            AppName = AppName,
            BbsDeviceId = BbsDeviceId,
            HoyolabDeviceId = HoyolabDeviceId,
            ExtFields = JsonSerializer.Serialize(ExtFields, DeviceFpJsonContext.Default.AndroidDeviceExtFields),
        };
    }


    public static AndroidDeviceInfo CreateRandom()
    {
        var ext = DeviceCatalog.RandomOne();
        AndroidDeviceInfo info = new()
        {
            DeviceId = ext.AndroidId,
            SeedId = Guid.NewGuid().ToString(),
            SeedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(),
            DeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString(),
            BbsDeviceId = GuidVersion3(ext.AndroidId),
            BbsDeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString(),
            HoyolabDeviceId = GuidVersion3(ext.AndroidId),
            HoyolabDeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString(),
            ExtFields = ext,
        };
        info.Update();
        return info;
    }



    public void Update()
    {
        if (ExtFields is null)
        {
            ExtFields = DeviceCatalog.RandomOne();
            DeviceId = ExtFields.AndroidId;
            SeedId = Guid.NewGuid().ToString();
            SeedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            DeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString();
            BbsDeviceId = GuidVersion3(ExtFields.AndroidId);
            BbsDeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString();
            HoyolabDeviceId = GuidVersion3(ExtFields.AndroidId);
            HoyolabDeviceFp = Random.Shared.NextInt64(10000_00000, 99999_99999).ToString();
        }

        ExtFields.NetworkType = Random.Shared.NextDouble() < 0.15 ? "MOBILE" : "WiFi";
        ExtFields.SdRemain = (int)(ExtFields.SdCapacity * RandomDouble(0.3, 0.6));
        ExtFields.RamRemain = ExtFields.SdRemain.ToString();
        ExtFields.RomRemain = Random.Shared.Next(96, 352).ToString();

        ExtFields.BatteryStatus = Random.Shared.Next(20, 100);
        ExtFields.ChargeStatus = ExtFields.BatteryStatus < 50 ? 1 : 0;
        ExtFields.RingMode = 2;
        ExtFields.IsAirMode = 0;
        ExtFields.Accelerometer = NextAccelerometer();
        ExtFields.Magnetometer = NextMagnetometer();
        ExtFields.Gyroscope = NextGyroscope();

    }


    private static string GuidVersion3(string value)
    {
        byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes(value));
        md5[6] &= 0x0f;
        md5[6] |= 0x30;
        md5[8] &= 0x3f;
        md5[8] |= 0x80;
        return new Guid(md5).ToString();
    }


    private static string NextAccelerometer()
    {
        double x = RandomDouble(-1, 1);
        double y = RandomDouble(-1, 1);
        double z = RandomDouble(-1, 1);

        double length = Math.Sqrt(x * x + y * y + z * z);
        double magnitude = RandomDouble(9.6, 9.9);
        x = x / length * magnitude + RandomDouble(-0.01, 0.01);
        y = y / length * magnitude + RandomDouble(-0.01, 0.01);
        z = z / length * magnitude + RandomDouble(-0.01, 0.01);

        return $"{x:F9}x{y:F9}x{z:F9}";
    }


    private static string NextMagnetometer()
    {
        double x = RandomDouble(-1, 1);
        double y = RandomDouble(-1, 1);
        double z = RandomDouble(-1, 1);

        double length = Math.Sqrt(x * x + y * y + z * z);
        double magnitude = RandomDouble(40, 60);
        x = x / length * magnitude;
        y = y / length * magnitude;
        z = z / length * magnitude;

        return $"{x:F4}x{y:F4}x{z:F4}";
    }


    private static string NextGyroscope()
    {
        static string random() => RandomDouble(-0.02, 0.02).ToString("F6");
        return $"{random()}x{random()}x{random()}";
    }



    private static double RandomDouble(double min, double max)
    {
        return min + (Random.Shared.NextDouble() * (max - min));
    }


    internal AndroidDeviceInfo Clone()
    {
        var obj = (AndroidDeviceInfo)MemberwiseClone();
        obj.ExtFields = ExtFields.Clone();
        return obj;
    }


}
