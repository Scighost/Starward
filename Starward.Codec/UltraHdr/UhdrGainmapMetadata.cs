using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
    public float MaxContentBoost0;
    public float MaxContentBoost1;
    public float MaxContentBoost2;

    /// <summary>
    /// Value to control how much darker an image can get, when shown on
    /// an HDR display, relative to the SDR rendition. This is constant for a
    /// given image. Value MUST be in linear scale.
    /// </summary>
    public float MinContentBoost0;
    public float MinContentBoost1;
    public float MinContentBoost2;

    /// <summary>
    /// Encoding Gamma of the gainmap image.
    /// </summary>
    public float Gamma0;
    public float Gamma1;
    public float Gamma2;

    /// <summary>
    /// The offset to apply to the SDR pixel values during gainmap generation
    /// and application.
    /// </summary>
    public float OffsetSdr0;
    public float OffsetSdr1;
    public float OffsetSdr2;

    /// <summary>
    /// The offset to apply to the HDR pixel values during gainmap generation
    /// and application.
    /// </summary>
    public float OffsetHdr0;
    public float OffsetHdr1;
    public float OffsetHdr2;

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
