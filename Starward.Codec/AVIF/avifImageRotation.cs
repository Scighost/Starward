namespace Starward.Codec.AVIF;


/// <summary>
/// angle * 90 specifies the angle (in anti-clockwise direction) in units of degrees.
/// </summary>
public enum avifImageRotation : byte
{
    Angle0 = 0,

    Angle90 = 1,

    Angle180 = 2,

    Angle270 = 3,
}