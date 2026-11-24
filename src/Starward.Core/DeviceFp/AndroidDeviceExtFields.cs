using System.Text.Json.Serialization;

namespace Starward.Core.DeviceFp;

public class AndroidDeviceExtFields
{
    // ---- 固定画像：来自真实机型 ----

    [JsonPropertyName("androidId")]
    public string AndroidId { get; set; }

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; } = "unknown";

    [JsonPropertyName("board")]
    public string Board { get; set; }

    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    [JsonPropertyName("hardware")]
    public string Hardware { get; set; }

    [JsonPropertyName("cpuType")]
    public string CpuType { get; set; } = "arm64-v8a";

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; }

    [JsonPropertyName("display")]
    public string Display { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; }

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("deviceInfo")]
    public string DeviceInfo { get; set; }

    [JsonPropertyName("sdkVersion")]
    public string SdkVersion { get; set; }

    [JsonPropertyName("osVersion")]
    public string OsVersion { get; set; }

    [JsonPropertyName("devId")]
    public string DevId { get; set; } = "REL";

    [JsonPropertyName("buildTags")]
    public string BuildTags { get; set; } = "release-keys";

    [JsonPropertyName("buildType")]
    public string BuildType { get; set; } = "user";

    [JsonPropertyName("buildUser")]
    public string BuildUser { get; set; }

    [JsonPropertyName("buildTime")]
    public string BuildTime { get; set; }

    [JsonPropertyName("screenSize")]
    public string ScreenSize { get; set; }

    // ---- 稳定身份：一次安装内不变 ----

    [JsonPropertyName("vendor")]
    public string Vendor { get; set; }

    [JsonPropertyName("romCapacity")]
    public string RomCapacity { get; set; }

    [JsonPropertyName("ramCapacity")]
    public string RamCapacity { get; set; }

    [JsonPropertyName("appMemory")]
    public string AppMemory { get; set; }

    [JsonPropertyName("isTablet")]
    public int IsTablet { get; set; }

    [JsonPropertyName("simState")]
    public int SimState { get; set; }

    [JsonPropertyName("ui_mode")]
    public string UiMode { get; set; } = "UI_MODE_TYPE_NORMAL";

    [JsonPropertyName("sdCapacity")]
    public int SdCapacity { get; set; }

    [JsonPropertyName("hasKeyboard")]
    public int HasKeyboard { get; set; }

    [JsonPropertyName("appInstallTimeDiff")]
    public long AppInstallTimeDiff { get; set; }

    [JsonPropertyName("appUpdateTimeDiff")]
    public long AppUpdateTimeDiff { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("oaid")]
    public string Oaid { get; set; }

    [JsonPropertyName("vaid")]
    public string Vaid { get; set; }

    [JsonPropertyName("aaid")]
    public string Aaid { get; set; }

    // ---- 每次采集都可变的运行时状态 ----

    [JsonPropertyName("packageName")]
    public string PackageName { get; set; } = "com.mihoyo.hyperion";

    [JsonPropertyName("packageVersion")]
    public string PackageVersion { get; set; } = "2.44.0";

    [JsonPropertyName("networkType")]
    public string NetworkType { get; set; } = "WiFi";

    [JsonPropertyName("romRemain")]
    public string RomRemain { get; set; }

    [JsonPropertyName("ramRemain")]
    public string RamRemain { get; set; }

    [JsonPropertyName("accelerometer")]
    public string Accelerometer { get; set; }

    [JsonPropertyName("magnetometer")]
    public string Magnetometer { get; set; }

    [JsonPropertyName("gyroscope")]
    public string Gyroscope { get; set; }

    [JsonPropertyName("sdRemain")]
    public int SdRemain { get; set; }

    [JsonPropertyName("ringMode")]
    public int RingMode { get; set; }

    [JsonPropertyName("isAirMode")]
    public int IsAirMode { get; set; }

    [JsonPropertyName("batteryStatus")]
    public int BatteryStatus { get; set; }

    [JsonPropertyName("chargeStatus")]
    public int ChargeStatus { get; set; }

    // ---- 安全/调试状态：真实零售机固定为 0 ----

    [JsonPropertyName("isRoot")]
    public int IsRoot { get; set; }

    [JsonPropertyName("debugStatus")]
    public int DebugStatus { get; set; }

    [JsonPropertyName("proxyStatus")]
    public int ProxyStatus { get; set; }

    [JsonPropertyName("emulatorStatus")]
    public int EmulatorStatus { get; set; }

    [JsonPropertyName("isMockLocation")]
    public int IsMockLocation { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    internal AndroidDeviceExtFields Clone()
    {
        return (AndroidDeviceExtFields)MemberwiseClone();
    }


}
