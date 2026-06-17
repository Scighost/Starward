using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// DirectX 配置
/// </summary>
public class GameDXConfig
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    [JsonPropertyName("enable_dx_switch")]
    public bool EnableDXSwitch { get; set; }


    [JsonPropertyName("cmd_args")]
    public string CmdArgs { get; set; }


    [JsonPropertyName("use_dx12_by_default")]
    public bool UseDX12ByDefault { get; set; }


    [JsonPropertyName("enable_highlight_title")]
    public bool EnableHighlightTitle { get; set; }


    [JsonPropertyName("dx11_preview_image")]
    public string DX11PreviewImage { get; set; }


    [JsonPropertyName("dx12_preview_image")]
    public string DX12PreviewImage { get; set; }


    [JsonPropertyName("i18n_intro")]
    public string I18nIntro { get; set; }


    [JsonPropertyName("force_enable")]
    public bool ForceEnable { get; set; }


    [JsonPropertyName("dx11_cmd_args")]
    public string DX11CmdArgs { get; set; }


    [JsonPropertyName("support_list")]
    public List<string> SupportList { get; set; }

}


/// <summary>
/// GPU 信息
/// </summary>
public class GPUInfo
{

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("driver_version")]
    public string DriverVersion { get; set; }

}


/// <summary>
/// 获取 DX 配置的请求参数
/// </summary>
public class GetDXConfigsRequest
{

    [JsonPropertyName("launcher_id")]
    public string LauncherId { get; set; }


    [JsonPropertyName("game_ids")]
    public List<string> GameIds { get; set; }


    [JsonPropertyName("language")]
    public string Language { get; set; }


    [JsonPropertyName("gpu_info")]
    public List<GPUInfo> GPUInfo { get; set; }

}


/// <summary>
/// 获取 DX 配置的响应数据
/// </summary>
internal class GetDXConfigsResponse
{

    [JsonPropertyName("dx_configs")]
    public List<GameDXConfig> DXConfigs { get; set; }

}
