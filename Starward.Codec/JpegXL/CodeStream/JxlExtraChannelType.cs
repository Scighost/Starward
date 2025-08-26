namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// Given type of an extra channel.
/// </summary>
public enum JxlExtraChannelType
{
    /// <summary>
    /// Alpha channel
    /// </summary>
    Alpha,
    /// <summary>
    /// Depth channel
    /// </summary>
    Depth,
    /// <summary>
    /// Spot color channel
    /// </summary>
    SpotColor,
    /// <summary>
    /// Selection mask
    /// </summary>
    SelectionMask,
    /// <summary>
    /// Black channel (for CMYK)
    /// </summary>
    Black,
    /// <summary>
    /// Color Filter Array channel
    /// </summary>
    CFA,
    /// <summary>
    /// Thermal channel
    /// </summary>
    Thermal,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved0,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved1,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved2,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved3,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved4,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved5,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved6,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved7,
    /// <summary>
    /// Unknown channel type
    /// </summary>
    Unknown,
    /// <summary>
    /// Optional channel that can be discarded
    /// </summary>
    Optional
}
