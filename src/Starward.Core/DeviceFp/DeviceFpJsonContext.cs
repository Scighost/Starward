using System.Text.Json.Serialization;

namespace Starward.Core.DeviceFp;

[JsonSerializable(typeof(DeviceFpRequest))]
[JsonSerializable(typeof(AndroidDeviceInfo))]
[JsonSerializable(typeof(miHoYoApiWrapper<AndroidDeviceExtFields>))]
[JsonSerializable(typeof(miHoYoApiWrapper<DeviceFpResult>))]
public partial class DeviceFpJsonContext : JsonSerializerContext
{

}