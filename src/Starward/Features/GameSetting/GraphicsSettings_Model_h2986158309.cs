using System.Text.Json.Serialization;

namespace Starward.Features.GameSetting;

/// <summary>
/// 星穹铁道图形设置
/// </summary>
public record GraphicsSettings_Model_h2986158309
{
    /// <summary>
    /// 30 60 120
    /// </summary>
    [JsonPropertyName("FPS")]
    public int FPS { get; set; }

    /// <summary>
    /// 垂直同步
    /// </summary>
    [JsonPropertyName("EnableVSync")]
    public bool EnableVSync { get; set; }

    /// <summary>
    /// 渲染精度 0.6 0.8 1.0 1.2 1.4 1.6 1.8 2.0
    /// </summary>
    [JsonPropertyName("RenderScale")]
    public double RenderScale { get; set; }


    [JsonPropertyName("ResolutionQuality")]
    public int ResolutionQuality { get; set; }

    /// <summary>
    /// 0 2 3 4
    /// </summary>
    [JsonPropertyName("ShadowQuality")]
    public int ShadowQuality { get; set; }

    /// <summary>
    /// 1 2 3 4 5
    /// </summary>
    [JsonPropertyName("LightQuality")]
    public int LightQuality { get; set; }

    /// <summary>
    /// 2 3 4
    /// </summary>
    [JsonPropertyName("CharacterQuality")]
    public int CharacterQuality { get; set; }

    /// <summary>
    /// 1 2 3 4 5
    /// </summary>
    [JsonPropertyName("EnvDetailQuality")]
    public int EnvDetailQuality { get; set; }

    /// <summary>
    /// 1 2 3 4 5
    /// </summary>
    [JsonPropertyName("ReflectionQuality")]
    public int ReflectionQuality { get; set; }

    /// <summary>
    /// 0 1 2 3 4 5
    /// </summary>
    [JsonPropertyName("BloomQuality")]
    public int BloomQuality { get; set; }

    /// <summary>
    /// 0 1 2
    /// </summary>
    [JsonPropertyName("AAMode")]
    public int AAMode { get; set; }


    [JsonPropertyName("EnableMetalFXSU")]
    public bool EnableMetalFXSU { get; set; }


    [JsonPropertyName("EnableHalfResTransparent")]
    public bool EnableHalfResTransparent { get; set; }


    [JsonPropertyName("EnableSelfShadow")]
    public int EnableSelfShadow { get; set; }


    [JsonPropertyName("DlssQuality")]
    public int DlssQuality { get; set; }


}
