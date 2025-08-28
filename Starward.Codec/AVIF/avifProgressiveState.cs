namespace Starward.Codec.AVIF;

public enum avifProgressiveState
{
    /// <summary>
    /// The current AVIF/Source does not offer a progressive image. This will always be the state
    /// for an image sequence.
    /// </summary>
    Unavaliable = 0,

    /// <summary>
    /// The current AVIF/Source offers a progressive image, but avifDecoder.allowProgressive is not
    /// enabled, so it will behave as if the image was not progressive and will simply decode the
    /// best version of this item.
    /// </summary>
    Avaliable,

    /// <summary>
    /// The current AVIF/Source offers a progressive image, and avifDecoder.allowProgressive is true.
    /// In this state, avifDecoder.imageCount will be the count of all of the available progressive
    /// layers, and any specific layer can be decoded using avifDecoderNthImage() as if it was an
    /// image sequence, or simply using repeated calls to avifDecoderNextImage() to decode better and
    /// better versions of this image.
    /// </summary>
    Active,
}
