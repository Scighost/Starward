using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifContentLightLevelInformationBox
{
    // 'clli' from ISO/IEC 23000-22:2019 (MIAF) 7.4.4.2.2. The SEI message semantics written above
    // each entry were originally described in ISO/IEC 23008-2:2020 (HEVC) section D.3.35,
    // available at https://standards.iso.org/ittf/PubliclyAvailableStandards/

    // Given the red, green, and blue colour primary intensities in the linear light domain for the
    // location of a luma sample in a corresponding 4:4:4 representation, denoted as E_R, E_G, and E_B,
    // the maximum component intensity is defined as E_Max = Max(E_R, Max(E_G, E_B)).
    // The light level corresponding to the stimulus is then defined as the CIE 1931 luminance
    // corresponding to equal amplitudes of E_Max for all three colour primary intensities for red,
    // green, and blue (with appropriate scaling to reflect the nominal luminance level associated
    // with peak white, e.g. ordinarily scaling to associate peak white with 10 000 candelas per
    // square metre when transfer_characteristics is equal to 16).

    // max_content_light_level, when not equal to 0, indicates an upper bound on the maximum light
    // level among all individual samples in a 4:4:4 representation of red, green, and blue colour
    // primary intensities (in the linear light domain) for the pictures of the CLVS, in units of
    // candelas per square metre. When equal to 0, no such upper bound is indicated by
    // max_content_light_level.
    public ushort MaxCLL;

    // max_pic_average_light_level, when not equal to 0, indicates an upper bound on the maximum
    // average light level among the samples in a 4:4:4 representation of red, green, and blue
    // colour primary intensities (in the linear light domain) for any individual picture of the
    // CLVS, in units of candelas per square metre. When equal to 0, no such upper bound is
    // indicated by max_pic_average_light_level.
    public ushort MaxPALL;
}
