namespace Starward.Codec.AVIF;

public enum avifImageMirror : byte
{
    // 'imir' from ISO/IEC 23008-12:2022 6.5.12:
    //
    //     'axis' specifies how the mirroring is performed:
    //
    //     0 indicates that the top and bottom parts of the image are exchanged;
    //     1 specifies that the left and right parts are exchanged.
    //
    //     NOTE In Exif, orientation tag can be used to signal mirroring operations. Exif
    //     orientation tag 4 corresponds to axis = 0 of ImageMirror, and Exif orientation tag 2
    //     corresponds to axis = 1 accordingly.
    //
    // Legal values: [0, 1]
    TopBottomExchanged = 0,

    LeftRightExchanged = 1,
}
