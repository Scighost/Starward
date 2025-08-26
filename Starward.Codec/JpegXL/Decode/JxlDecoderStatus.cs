namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// <para>
/// Return value for <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>.
/// The values from <see cref="BasicInfo"/> onwards are optional informative
/// events that can be subscribed to, they are never returned if they
/// have not been registered with <see cref="JxlDecoderNativeMethod.JxlDecoderSubscribeEvents"/>.
/// </para>
/// </summary>
[Flags]
public enum JxlDecoderStatus
{
    /// <summary>
    /// <para>
    /// Function call finished successfully, or decoding is finished and there is
    /// nothing more to be done.
    /// </para>
    /// <para>
    /// Note that <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/> will return <see cref="Success"/> if
    /// all events that were registered with <see cref="JxlDecoderNativeMethod.JxlDecoderSubscribeEvents"/> were
    /// processed, even before the end of the JPEG XL codestream.
    /// </para>
    /// <para>
    /// In this case, the return value <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will be the same
    /// as it was at the last signaled event. E.g. if <see cref="FullImage"/> was
    /// subscribed to, then all bytes from the end of the JPEG XL codestream
    /// (including possible boxes needed for jpeg reconstruction) will be returned
    /// as unprocessed.
    /// </para>
    /// </summary>
    Success = 0,

    /// <summary>
    /// An error occurred, for example invalid input file or out of memory.
    /// </summary>
    Error = 1,

    /// <summary>
    /// <para>
    /// The decoder needs more input bytes to continue. Before the next
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/> call, more input data must be set, by calling
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> (if input was set previously) and then calling
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderSetInput"/>. <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> returns how many bytes
    /// are not yet processed, before a next call to <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>
    /// all unprocessed bytes must be provided again (the address need not match,
    /// but the contents must), and more bytes must be concatenated after the
    /// unprocessed bytes.
    /// </para>
    /// <para>
    /// In most cases, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return no unprocessed bytes
    /// at this event, the only exceptions are if the previously set input ended
    /// within (a) the raw codestream signature, (b) the signature box, (c) a box
    /// header, or (d) the first 4 bytes of a `brob`, `ftyp`, or `jxlp` box. In any
    /// of these cases the number of unprocessed bytes is less than 20.
    /// </para>
    /// </summary>
    NeedMoreInput = 2,

    /// <summary>
    /// <para>
    /// The decoder is able to decode a preview image and requests setting a
    /// preview output buffer using <see cref="JxlDecoderNativeMethod.JxlDecoderSetPreviewOutBuffer"/>. This occurs
    /// if <see cref="PreviewImage"/> is requested and it is possible to decode a
    /// preview image from the codestream and the preview out buffer was not yet
    /// set. There is maximum one preview image in a codestream.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the frame header (including ToC) of the preview frame as
    /// unprocessed.
    /// </para>
    /// </summary>
    NeedPreviewOutBuffer = 3,

    /// <summary>
    /// <para>
    /// The decoder requests an output buffer to store the full resolution image,
    /// which can be set with <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutBuffer"/> or with
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutCallback"/>. This event re-occurs for new frames if
    /// there are multiple animation frames and requires setting an output again.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the frame header (including ToC) as unprocessed.
    /// </para>
    /// </summary>
    NeedImageOutBuffer = 5,

    /// <summary>
    /// The JPEG reconstruction buffer is too small for reconstructed JPEG
    /// codestream to fit. <see cref="JxlDecoderNativeMethod.JxlDecoderSetJPEGBuffer"/> must be called again to
    /// make room for remaining bytes. This event may occur multiple times
    /// after <see cref="JpegReconstruction"/>.
    /// </summary>
    JpegNeedMoreOutput = 6,

    /// <summary>
    /// The box contents output buffer is too small. <see cref="JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer"/>
    /// must be called again to make room for remaining bytes. This event may occur
    /// multiple times after <see cref="Box"/>.
    /// </summary>
    BoxNeedMoreOutput = 7,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// Basic information such as image dimensions and
    /// extra channels. This event occurs max once per image.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the basic info as unprocessed (including the last byte of basic info
    /// if it did not end on a byte boundary).
    /// </para>
    /// </summary>
    BasicInfo = 0x40,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// Color encoding or ICC profile from the
    /// codestream header. This event occurs max once per image and always later
    /// than <see cref="BasicInfo"/> and earlier than any pixel data.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the image header (which is the start of the first frame) as
    /// unprocessed.
    /// </para>
    /// </summary>
    ColorEncoding = 0x100,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// Preview image, a small frame, decoded. This
    /// event can only happen if the image has a preview frame encoded. This event
    /// occurs max once for the codestream and always later than
    /// <see cref="ColorEncoding"/> and before <see cref="Frame"/>.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the preview frame as unprocessed.
    /// </para>
    /// </summary>
    PreviewImage = 0x200,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// Beginning of a frame. 
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderGetFrameHeader"/> can be used at this point. A note on frames:
    /// a JPEG XL image can have internal frames that are not intended to be
    /// displayed (e.g. used for compositing a final frame), but this only returns
    /// displayed frames, unless <see cref="JxlDecoderNativeMethod.JxlDecoderSetCoalescing"/> was set to <see langword="false"/>:
    /// in that case, the individual layers are returned,
    /// without blending. Note that even when coalescing is disabled, only frames
    /// of type kRegularFrame are returned; frames of type kReferenceOnly
    /// and kLfFrame are always for internal purposes only and cannot be accessed.
    /// A displayed frame either has an animation duration or is the only or last
    /// frame in the image. This event occurs max once per displayed frame, always
    /// later than <see cref="ColorEncoding"/>, and always earlier than any pixel
    /// data. While JPEG XL supports encoding a single frame as the composition of
    /// multiple internal sub-frames also called frames, this event is not
    /// indicated for the internal frames.
    /// </para>
    /// <para>
    /// In this case,
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the end of the frame
    /// header (including ToC) as unprocessed.
    /// </para>
    /// </summary>
    Frame = 0x400,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// full frame (or layer, in case coalescing is
    /// disabled) is decoded. <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutBuffer"/> must be used after
    /// getting the basic image information to be able to get the image pixels, if
    /// not this return status only indicates we're past this point in the
    /// codestream. This event occurs max once per frame.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the frame (or if <see cref="JpegReconstruction"/> is subscribed to,
    /// from the end of the last box that is needed for jpeg reconstruction) as
    /// unprocessed.
    /// </para>
    /// </summary>
    FullImage = 0x1000,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// JPEG reconstruction data decoded. 
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderSetJPEGBuffer"/> may be used to set a JPEG reconstruction buffer
    /// after getting the JPEG reconstruction data. If a JPEG reconstruction buffer
    /// is set a byte stream identical to the JPEG codestream used to encode the
    /// image will be written to the JPEG reconstruction buffer instead of pixels
    /// to the image out buffer. This event occurs max once per image and always
    /// before <see cref="FullImage"/>.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the `jbrd` box as unprocessed.
    /// </para>
    /// </summary>
    JpegReconstruction = 0x2000,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// The header of a box of the container format
    /// (BMFF) is decoded. The following API functions related to boxes can be used
    /// after this event:
    ///  - <see cref="JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer"/> and <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseBoxBuffer"/>:
    ///    set and release a buffer to get the box data.
    ///  - <see cref="JxlDecoderNativeMethod.JxlDecoderGetBoxType"/> get the 4-character box typename.
    ///  - <see cref="JxlDecoderNativeMethod.JxlDecoderGetBoxSizeRaw"/> get the size of the box as it appears in
    ///    the container file, not decompressed.
    ///  - <see cref="JxlDecoderNativeMethod.JxlDecoderSetDecompressBoxes"/> to configure whether to get the box
    ///    data decompressed, or possibly compressed.
    /// </para>
    /// <para>
    /// Boxes can be compressed. This is so when their box type is
    /// "brob". In that case, they have an underlying decompressed box
    /// type and decompressed data. <see cref="JxlDecoderNativeMethod.JxlDecoderSetDecompressBoxes"/> allows
    /// configuring which data to get. Decompressing requires
    /// Brotli. <see cref="JxlDecoderNativeMethod.JxlDecoderGetBoxType"/> has a flag to get the compressed box
    /// type, which can be "brob", or the decompressed box type. If a box
    /// is not compressed (its compressed type is not "brob"), then
    /// the output decompressed box type and data is independent of what
    /// setting is configured.
    /// </para>
    /// <para>
    /// The buffer set with <see cref="JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer"/> must be set again for each
    /// next box to be obtained, or can be left unset to skip outputting this box.
    /// The output buffer contains the full box data when the
    /// <see cref="BoxComplete"/> (if subscribed to) or subsequent <see cref="Success"/>
    /// or <see cref="Box"/> event occurs. <see cref="Box"/> occurs for all boxes,
    /// including non-metadata boxes such as the signature box or codestream boxes.
    /// To check whether the box is a metadata type for respectively EXIF, XMP or
    /// JUMBF, use <see cref="JxlDecoderNativeMethod.JxlDecoderGetBoxType"/> and check for types "Exif", "xml " and
    /// "jumb" respectively.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// start of the box header as unprocessed.
    /// </para>
    /// </summary>
    Box = 0x4000,

    /// <summary>
    /// <para>
    /// Informative event by <see cref="JxlDecoderNativeMethod.JxlDecoderProcessInput"/>:
    /// a progressive step in decoding the frame is
    /// reached. When calling <see cref="JxlDecoderNativeMethod.JxlDecoderFlushImage"/> at this point, the flushed
    /// image will correspond exactly to this point in decoding, and not yet
    /// contain partial results (such as partially more fine detail) of a next
    /// step. By default, this event will trigger maximum once per frame, when a
    /// 8x8th resolution (DC) image is ready (the image data is still returned at
    /// full resolution, giving upscaled DC). Use
    /// <see cref="JxlDecoderNativeMethod.JxlDecoderSetProgressiveDetail"/> to configure more fine-grainedness. The
    /// event is not guaranteed to trigger, not all images have progressive steps
    /// or DC encoded.
    /// </para>
    /// <para>
    /// In this case, <see cref="JxlDecoderNativeMethod.JxlDecoderReleaseInput"/> will return all bytes from the
    /// end of the section that was needed to produce this progressive event as
    /// unprocessed.
    /// </para>
    /// </summary>
    FrameProgression = 0x8000,

    /// <summary>
    /// The box being decoded is now complete. This is only emitted if a buffer
    /// was set for the box.
    /// </summary>
    BoxComplete = 0x10000,
}
