namespace Starward.Codec.UltraHdr;

/// <summary>
/// Algorithm return codes
/// </summary>
public enum UhdrCodecError
{
    /// <summary>
    /// Operation completed without error
    /// </summary>
    OK,
    /// <summary>
    /// Generic codec error, refer detail field for more information
    /// </summary>
    Error,
    /// <summary>
    /// Unknown error, refer detail field for more information
    /// </summary>
    UnknownError,
    /// <summary>
    /// An application-supplied parameter is not valid.
    /// </summary>
    InvalidParameter,
    /// <summary>
    /// Memory operation failed.
    /// </summary>
    MemoryError,
    /// <summary>
    /// An application-invoked operation is not valid.
    /// </summary>
    InvalidOperation,
    /// <summary>
    /// The library does not implement a feature required for the operation.
    /// </summary>
    UnsupportedFeature,
    /// <summary>
    /// Not for usage, indicates end of list.
    /// </summary>
    ListEnd,
}
