using System.Runtime.InteropServices;

namespace Starward.Codec.VP9Decoder;

public static partial class VP9Decoder
{

    /// <summary>
    /// Register VP9 Decoder MFT locally for current process.
    /// </summary>
    /// <returns>HRESULT</returns>
    [LibraryImport("VP9DecoderMFT.dll")]
    public static partial int RegisterVP9DecoderLocal();


    /// <summary>
    /// Unregister VP9 Decoder MFT locally for current process.
    /// </summary>
    /// <returns>HRESULT</returns>
    [LibraryImport("VP9DecoderMFT.dll")]
    public static partial int UnregisterVP9DecoderLocal();

}
