namespace Starward.Codec.AVIF;

/// <summary>
/// avifRange is only applicable to YUV planes. RGB and alpha planes are always full range.
/// </summary>
public enum avifRange
{
    /// <summary>
    /// Y [16..235], UV [16..240] (bit depth 8)<br/>
    /// Y [64..940], UV [64..960] (bit depth 10)<br/>
    /// Y [256..3760], UV [256..3840] (bit depth 12)<br/>
    /// </summary>
    Limited = 0,

    /// <summary>
    /// [0..255] (bit depth 8)<br/>
    /// [0..1023] (bit depth 10)<br/>
    /// [0..4095] (bit depth 12)<br/>
    /// </summary>
    Full = 1,
}
