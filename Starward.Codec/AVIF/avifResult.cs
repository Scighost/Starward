namespace Starward.Codec.AVIF;

public enum avifResult
{
    OK = 0,
    UnknownError = 1,
    InvalidFTYP = 2,
    NoContent = 3,
    NoYUVFormatSelected = 4,
    ReformatFailed = 5,
    UnspuuortedDepth = 6,
    EncodeColorFailed = 7,
    EncodeAlphaFailed = 8,
    BMFFParseFailed = 9,
    MissingImageItem = 10,
    DecodeColorFailed = 11,
    DecodeAlphaFailed = 12,
    ColorAlphaSizeMismatch = 13,
    ISPESizeMismatch = 14,
    NoCodecAvaliable = 15,
    NoImageRemaining = 16,
    InvalidExifPayload = 17,
    InvalidImageGrid = 18,
    InvalidCodecSpecficOption = 19,
    TruncatedData = 20,
    /// <summary>
    /// the avifIO field of avifDecoder is not set
    /// </summary>
    IONotSet = 21,
    IOError = 22,
    /// <summary>
    /// similar to EAGAIN/EWOULDBLOCK, this means the avifIO doesn't have necessary data available yet
    /// </summary>
    WaitingNoIO = 23,
    /// <summary>
    /// an argument passed into this function is invalid
    /// </summary>
    InvalidArgument = 24,
    /// <summary>
    /// a requested code path is not (yet) implemented
    /// </summary>
    NotImplemented = 25,
    OutOfMemory = 26,
    /// <summary>
    /// a setting that can't change is changed during encoding
    /// </summary>
    CannotChangeSetting = 27,
    /// <summary>
    /// the image is incompatible with already encoded images
    /// </summary>
    IncompatibaleImage = 28,
    /// <summary>
    /// some invariants have not been satisfied (likely a bug in libavif)
    /// </summary>
    InternalError = 29,
    EncodeGainMapFailed = 30,
    DecodeGainMapFailed = 31,
    InvalidToneMappedImage = 32,
#if AVIF_ENABLE_EXPERIMENTAL_SAMPLE_TRANSFORM
    EncodeSampleTransformFailed= 33,
    DecodeSampleTransformFailed= 34,
#endif

    /// <summary>
    /// Kept for backward compatibility; please use the symbols above instead.
    /// </summary>
    NoAV1ItemsFound = MissingImageItem,
}
