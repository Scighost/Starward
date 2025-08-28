namespace Starward.Codec.AVIF;

[Flags]
public enum avifAddImageFlag : uint
{
    None = 0,

    /// <summary>
    /// Force this frame to be a keyframe (sync frame).
    /// </summary>
    ForceKeyframe = (1 << 0),

    /// <summary>
    /// Use this flag when encoding a single frame, single layer image.
    /// Signals "still_picture" to AV1 encoders, which tweaks various compression rules.
    /// This is enabled automatically when using the avifEncoderWrite() single-image encode path.
    /// </summary>
    Single = (1 << 1),
}
