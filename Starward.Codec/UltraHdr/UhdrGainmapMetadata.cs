using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

/// <summary>
/// Gain map metadata
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdrGainmapMetadata
{
    /// <summary>
    /// Value to control how much brighter an image can get, when shown
    /// on an HDR display, relative to the SDR rendition. This is constant for
    /// a given image. Value MUST be in linear scale.
    /// </summary>
    public FixedArray3<float> MaxContentBoost;

    /// <summary>
    /// Value to control how much darker an image can get, when shown on
    /// an HDR display, relative to the SDR rendition. This is constant for a
    /// given image. Value MUST be in linear scale.
    /// </summary>
    public FixedArray3<float> MinContentBoost;

    /// <summary>
    /// Encoding Gamma of the gainmap image.
    /// </summary>
    public FixedArray3<float> Gamma;

    /// <summary>
    /// The offset to apply to the SDR pixel values during gainmap generation
    /// and application.
    /// </summary>
    public FixedArray3<float> OffsetSdr;

    /// <summary>
    /// The offset to apply to the HDR pixel values during gainmap generation
    /// and application.
    /// </summary>
    public FixedArray3<float> OffsetHdr;

    /// <summary>
    /// Minimum display boost value for which the map is applied completely.
    /// Value MUST be in linear scale.
    /// </summary>
    public float HdrCapacityMin;

    /// <summary>
    /// Maximum display boost value for which the map is applied completely.
    /// Value MUST be in linear scale.
    /// </summary>
    public float HdrCapacityMax;

    /// <summary>
    /// Is gainmap application space same as base image color space
    /// </summary>
    public int UseBaseColorSpace;

}


public struct UhdrGainmapMetadataPtr
{
    private IntPtr _ptr;
    public bool IsNull => _ptr == IntPtr.Zero;

    public UhdrGainmapMetadata ToGainmapMetadata()
    {
        if (IsNull)
        {
            throw new InvalidOperationException("Pointer is null. Cannot convert to UhdrGainmapMetadata.");
        }
        return Marshal.PtrToStructure<UhdrGainmapMetadata>(_ptr);
    }
}


