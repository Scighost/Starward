namespace Starward.Codec.AVIF;

public enum avifDecoderSource
{
    /// <summary>
    /// Honor the major brand signaled in the beginning of the file to pick between an AVIF sequence
    /// ('avis', tracks-based) or a single image ('avif', item-based). If the major brand is neither
    /// of these, prefer the AVIF sequence ('avis', tracks-based), if present.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Use the primary item and the aux (alpha) item in the avif(s).
    /// This is where single-image avifs store their image.
    /// </summary>
    PrimaryItem,

    /// <summary>
    /// Use the chunks inside primary/aux tracks in the moov block.
    /// This is where avifs image sequences store their images.
    /// </summary>
    Tracks,

    // Decode the thumbnail item. Currently unimplemented.
    // AVIF_DECODER_SOURCE_THUMBNAIL_ITEM
}
