namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// Image orientation metadata.
/// Values 1..8 match the EXIF definitions.
/// The name indicates the operation to perform to transform from the encoded
/// image to the display image.
/// </summary>
public enum JxlOrientation
{
    /// <summary>
    /// No transformation.
    /// </summary>
    Identity = 1,
    /// <summary>
    /// Flip horizontally.
    /// </summary>
    FlipHorizontal = 2,
    /// <summary>
    /// Rotate 180 degrees.
    /// </summary>
    Rotate180 = 3,
    /// <summary>
    /// Flip vertically.
    /// </summary>
    FlipVertical = 4,
    /// <summary>
    /// Transpose (flip across top-left to bottom-right axis).
    /// </summary>
    Transpose = 5,
    /// <summary>
    /// Rotate 90 degrees clockwise.
    /// </summary>
    Rotate90CW = 6,
    /// <summary>
    /// Anti-transpose (flip across top-right to bottom-left axis).
    /// </summary>
    AntiTranspose = 7,
    /// <summary>
    /// Rotate 90 degrees counter-clockwise.
    /// </summary>
    Rotate90CCW = 8,
}
