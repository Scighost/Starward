using System.Text.Json.Serialization;

namespace Starward.Models.GameSetting;

public record GraphicsSettings_Model_h2986158309
{
    /// <summary>
    /// 30 60 120
    /// </summary>
    [JsonPropertyName("FPS")]
    public int FPS { get; set; }

    [JsonPropertyName("EnableVSync")]
    public bool EnableVSync { get; set; }

    /// <summary>
    /// 0.6 0.8 1.0 1.2 1.4 1.6 1.8 2.0
    /// </summary>
    [JsonPropertyName("RenderScale")]
    public double RenderScale { get; set; }

    [JsonPropertyName("ResolutionQuality")]
    public int ResolutionQuality { get; set; } = 3;

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



    public static GraphicsSettings_Model_h2986158309 VeryLow => new GraphicsSettings_Model_h2986158309
    {
        FPS = 60,
        EnableVSync = false,
        RenderScale = 0.8,
        ShadowQuality = 2,
        LightQuality = 1,
        CharacterQuality = 2,
        EnvDetailQuality = 1,
        ReflectionQuality = 1,
        BloomQuality = 1,
        AAMode = 1,
    };


    public static GraphicsSettings_Model_h2986158309 Low => new GraphicsSettings_Model_h2986158309
    {
        FPS = 60,
        EnableVSync = true,
        RenderScale = 1.0,
        ShadowQuality = 2,
        LightQuality = 2,
        CharacterQuality = 2,
        EnvDetailQuality = 2,
        ReflectionQuality = 2,
        BloomQuality = 2,
        AAMode = 1,
    };


    public static GraphicsSettings_Model_h2986158309 Medium => new GraphicsSettings_Model_h2986158309
    {
        FPS = 60,
        EnableVSync = true,
        RenderScale = 1.0,
        ShadowQuality = 3,
        LightQuality = 3,
        CharacterQuality = 3,
        EnvDetailQuality = 3,
        ReflectionQuality = 3,
        BloomQuality = 3,
        AAMode = 1,
    };


    public static GraphicsSettings_Model_h2986158309 High => new GraphicsSettings_Model_h2986158309
    {
        FPS = 60,
        EnableVSync = true,
        RenderScale = 1.2,
        ShadowQuality = 4,
        LightQuality = 4,
        CharacterQuality = 4,
        EnvDetailQuality = 4,
        ReflectionQuality = 4,
        BloomQuality = 4,
        AAMode = 1,
    };


    public static GraphicsSettings_Model_h2986158309 VeryHigh => new GraphicsSettings_Model_h2986158309
    {
        FPS = 60,
        EnableVSync = true,
        RenderScale = 1.4,
        ShadowQuality = 4,
        LightQuality = 5,
        CharacterQuality = 4,
        EnvDetailQuality = 5,
        ReflectionQuality = 5,
        BloomQuality = 5,
        AAMode = 1,
    };


}
