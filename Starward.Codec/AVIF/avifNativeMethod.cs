using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

public static partial class avifNativeMethod
{

    private const string LibraryName = "avif";


    [LibraryImport(LibraryName)]
    public static partial CString avifVersion();



    [LibraryImport(LibraryName, EntryPoint = "avifCodecVersions")]
    public static partial void avifCodecVersionsInternal(Span<byte> buffer);

    public static unsafe string? avifCodecVersions()
    {
        Span<byte> buffer = stackalloc byte[256];
        avifCodecVersionsInternal(buffer);
        fixed (byte* p = buffer)
        {
            return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty;
        }
    }


    [LibraryImport(LibraryName)]
    public static partial uint avifLibYUVVersion();


    [LibraryImport(LibraryName)]
    public static partial CString avifResultToString(avifResult result);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRWDataRealloc(avifRWData* rw, ulong newSize);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRWDataSet(avifRWData* raw, byte* data, nuint size);


    [LibraryImport(LibraryName)]

    public static unsafe partial void avifRWDataFree(avifRWData* raw);


    /// <summary>
    /// Validates the first bytes of the Exif payload and finds the TIFF header offset (up to UINT32_MAX).
    /// </summary>
    /// <param name="exif"></param>
    /// <param name="exifSize"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifGetExifTiffHeaderOffset(byte* exif, nuint exifSize, nuint* offset);


    /// <summary>
    /// Returns the offset to the Exif 8-bit orientation value and AVIF_RESULT_OK, or an error.
    /// If the offset is set to exifSize, there was no parsing error but no orientation tag was found.
    /// </summary>
    /// <param name="exif"></param>
    /// <param name="exifSize"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifGetExifOrientationOffset(byte* exif, nuint exifSize, nuint* offset);


    /// <summary>
    /// Returns the avifPixelFormatInfo depending on the avifPixelFormat.
    /// When monochrome is AVIF_TRUE, chromaShiftX and chromaShiftY are set to 1 according to the AV1 specification but they should be ignored.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="info"></param>
    [LibraryImport(LibraryName)]
    public static unsafe partial void avifGetPixelFormatInfo(avifPixelFormat format, avifPixelFormatInfo* info);



    /// <summary>
    /// outPrimaries: rX, rY, gX, gY, bX, bY, wX, wY
    /// </summary>
    /// <param name="acp"></param>
    /// <param name="outPrimaries">8 float array</param>
    [LibraryImport(LibraryName)]
    public static partial void avifColorPrimariesGetValues(avifColorPrimaries acp, Span<float> outPrimaries);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="inPrimaries">8 float array</param>
    /// <param name="outName">char**</param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifColorPrimaries avifColorPrimariesFind(Span<float> inPrimaries, byte** outName);


    /// <summary>
    /// If the given transfer characteristics can be expressed with a simple gamma value, sets 'gamma'
    /// to that value and returns AVIF_RESULT_OK. Returns an error otherwise.
    /// </summary>
    /// <param name="atc"></param>
    /// <param name="gamma"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static partial avifResult avifTransferCharacteristicsGetGamma(avifTransferCharacteristics atc, ref float gamma);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifTransferCharacteristics avifTransferCharacteristicsFindByGamma(float gamma);


    /// <summary>
    /// Creates an int32/uint32 fraction that is approximately equal to 'v'.
    /// Returns AVIF_FALSE if 'v' is NaN or abs(v) is > INT32_MAX.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fraction"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifDoubleToSignedFraction(double value, avifSignedFraction* fraction);



    /// <summary>
    /// Creates a uint32/uint32 fraction that is approximately equal to 'v'.
    /// Returns AVIF_FALSE if 'v' is &lt; 0 or &gt; UINT32_MAX or NaN.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fraction"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifDoubleToUnsignedFraction(double value, avifSignedFraction* fraction);



    /// <summary>
    /// These will return AVIF_FALSE if the resultant values violate any standards, and if so, the output
    /// values are not guaranteed to be complete or correct and should not be used.
    /// </summary>
    /// <param name="cropRect"></param>
    /// <param name="clap"></param>
    /// <param name="imageWidth"></param>
    /// <param name="imageHeight"></param>
    /// <param name="diag"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifCropRectFromCleanApertureBox(avifCropRect* cropRect, avifCleanApertureBox* clap, uint imageWidth, uint imageHeight, avifDiagnostics* diag);


    /// <summary>
    /// These will return AVIF_FALSE if the resultant values violate any standards, and if so, the output
    /// values are not guaranteed to be complete or correct and should not be used.
    /// </summary>
    /// <param name="clap"></param>
    /// <param name="cropRect"></param>
    /// <param name="imageWidth"></param>
    /// <param name="imageHeight"></param>
    /// <param name="diag"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifCleanApertureBoxFromCropRect(avifCleanApertureBox* clap, avifCropRect* cropRect, uint imageWidth, uint imageHeight, avifDiagnostics* diag);


    /// <summary>
    /// If this function returns true, the image must be upsampled from 4:2:0 or 4:2:2 to 4:4:4 before
    /// Clean Aperture values are applied. This can be done by converting the avifImage to RGB using
    /// avifImageYUVToRGB() and only using the cropRect region of the avifRGBImage.
    /// </summary>
    /// <param name="cropRect"></param>
    /// <param name="yuvFormat"></param>
    /// <returns></returns>

    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifCropRectRequiresUpsampling(avifCropRect* cropRect, avifPixelFormat yuvFormat);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifCropRectConvertCleanApertureBox(avifCropRect* cropRect, avifCleanApertureBox* clap, uint imageWidth, uint imageHeight, avifPixelFormat yuvFormat, avifDiagnostics* diag);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifCleanApertureBoxConvertCropRect(avifCleanApertureBox* clap, avifCropRect* cropRect, uint imageWidth, uint imageHeight, avifPixelFormat yuvFormat, avifDiagnostics* diag);


    /// <summary>
    /// Allocates a gain map. Returns NULL if a memory allocation failed.
    /// The 'image' field is NULL by default and must be allocated separately.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifGainMap* avifGainMapCreate();



    /// <summary>
    /// Frees a gain map, including the 'image' field if non NULL.
    /// </summary>
    /// <param name="gainMap"></param>
    [LibraryImport(LibraryName)]
    public static unsafe partial void avifGainMapDestroy(avifGainMap* gainMap);

    /// <summary>
    /// avifImageCreate() and avifImageCreateEmpty() return NULL if arguments are invalid or if a memory allocation failed.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="depth"></param>
    /// <param name="yuvFormat"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifImage* avifImageCreate(uint width, uint height, uint depth, avifPixelFormat yuvFormat);

    /// <summary>
    /// avifImageCreate() and avifImageCreateEmpty() return NULL if arguments are invalid or if a memory allocation failed.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifImage* avifImageCreateEmpty();


    /// <summary>
    /// Performs a deep copy of an image, including all metadata and planes, and the gain map metadata/planes if present.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageCreate(avifImage* dstImage, avifImage* srcImage, avifPlanesFlag planes);


    /// <summary>
    /// Performs a shallow copy of a rectangular area of an image. 'dstImage' does not own the planes.
    /// Ignores the gainMap field.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageSetViewRect(avifImage* dstImage, avifImage* srcImage, avifCropRect* rect);


    [LibraryImport(LibraryName)]
    public static unsafe partial void avifImageDestroy(avifImage* image);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageSetProfileICC(avifImage* image, IntPtr icc, nuint iccSize);


    /// <summary>
    /// Sets Exif metadata. Attempts to parse the Exif metadata for Exif orientation. Sets
    /// image->transformFlags, image->irot and image->imir if the Exif metadata is parsed successfully,
    /// otherwise leaves image->transformFlags, image->irot and image->imir unchanged.
    /// <para/>
    /// Warning: If the Exif payload is set and invalid, avifEncoderWrite() may return AVIF_RESULT_INVALID_EXIF_PAYLOAD.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="exif"></param>
    /// <param name="exifSize"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageSetMetadataExif(avifImage* image, IntPtr exif, nuint exifSize);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageSetMetadataXMP(avifImage* image, IntPtr xmp, nuint xmpSize);


    /// <summary>
    /// Allocate/free/steal planes. These functions ignore the gainMap field.
    /// Ignores any pre-existing planes
    /// </summary>
    /// <param name="image"></param>
    /// <param name="planes"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageAllocatePlanes(avifImage* image, avifPlanesFlag planes);


    /// <summary>
    /// Allocate/free/steal planes. These functions ignore the gainMap field.
    /// Ignores already-freed planes
    /// </summary>
    /// <param name="image"></param>
    /// <param name="planes"></param>
    [LibraryImport(LibraryName)]
    public static unsafe partial void avifImageFreePlanes(avifImage* image, avifPlanesFlag planes);


    [LibraryImport(LibraryName)]
    public static unsafe partial void avifImageStealPlanes(avifImage* dstImage, avifImage* srcImage, avifPlanesFlag planes);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageAddOpaqueProperty(avifImage* image, FixedArray4<byte> boxtype, IntPtr data, nuint dataSize);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageAddUUIDProperty(avifImage* image, FixedArray16<byte> uuid, IntPtr data, nuint dataSize);


    /// <summary>
    /// Scales the YUV/A planes in-place. dstWidth and dstHeight must both be &lt;= AVIF_DEFAULT_IMAGE_DIMENSION_LIMIT and
    /// dstWidth*dstHeight should be &lt;= AVIF_DEFAULT_IMAGE_SIZE_LIMIT.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="dstWidth"></param>
    /// <param name="dstHeight"></param>
    /// <param name="diag"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageScale(avifImage* image, uint dstWidth, uint dstHeight, avifDiagnostics* diag);


    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifRGBFormatChannelCount(avifRGBFormat format);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifRGBFormatHasAlpha(avifRGBFormat format);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifRGBFormatIsGray(avifRGBFormat format);





    /// <summary>
    /// Sets rgb->width, rgb->height, and rgb->depth to image->width, image->height, and image->depth.
    /// Sets rgb->pixels to NULL and rgb->rowBytes to 0. Sets the other fields of 'rgb' to default values.
    /// </summary>
    /// <param name="rgb"></param>
    /// <param name="image"></param>
    [LibraryImport(LibraryName)]
    public static unsafe partial void avifRGBImageSetDefaults(avifRGBImage* rgb, avifImage* image);


    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifRGBImagePixelSize(avifRGBImage* rgb);


    /// <summary>
    /// Convenience functions. If you supply your own pixels/rowBytes, you do not need to use these.
    /// </summary>
    /// <param name="rgb"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRGBImageAllocatePixels(avifRGBImage* rgb);



    [LibraryImport(LibraryName)]
    public static unsafe partial void avifRGBImageFreePixels(avifRGBImage* rgb);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageRGBToYUV(avifImage* image, avifRGBImage* rgb);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageYUVToRGB(avifImage* image, avifRGBImage* rgb);


    /// <summary>
    /// Premultiply handling functions.
    /// (Un)premultiply is automatically done by the main conversion functions above,
    /// so usually you don't need to call these. They are there for convenience.
    /// </summary>
    /// <param name="rgb"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRGBImagePremultiplyAlpha(avifRGBImage* rgb);


    /// <summary>
    /// Premultiply handling functions.
    /// (Un)premultiply is automatically done by the main conversion functions above,
    /// so usually you don't need to call these. They are there for convenience.
    /// </summary>
    /// <param name="rgb"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRGBImageUnpremultiplyAlpha(avifRGBImage* rgb);



    [LibraryImport(LibraryName)]
    public static unsafe partial int avifFullToLimitedY(uint depth, int value);

    [LibraryImport(LibraryName)]
    public static unsafe partial int avifFullToLimitedUV(uint depth, int value);

    [LibraryImport(LibraryName)]
    public static unsafe partial int avifLimitedToFullY(uint depth, int value);

    [LibraryImport(LibraryName)]
    public static unsafe partial int avifLimitedToFullUV(uint depth, int value);





    [LibraryImport(LibraryName)]
    public static unsafe partial CString avifCodecName(avifCodecChoice choice, avifCodecFlag requiredFlags);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifCodecChoice avifCodecChoiceFromName([MarshalAs(UnmanagedType.LPStr)] string name);



    /// <summary>
    /// Returns NULL if the reader cannot be allocated.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifIO* avifIOCreateMemoryReader(IntPtr data, nuint size);


    /// <summary>
    /// Returns NULL if the file cannot be opened or if the reader cannot be allocated.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifIO* avifIOCreateFileReader([MarshalAs(UnmanagedType.LPStr)] string filename);


    [LibraryImport(LibraryName)]
    public static unsafe partial void avifIODestroy(avifIO* io);




    [LibraryImport(LibraryName)]
    public static unsafe partial CString avifProgressiveStateToString(avifProgressiveState progressiveState);


    /// <summary>
    /// Creates a decoder initialized with default settings values.
    /// Returns NULL in case of memory allocation failure.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifDecoder* avifDecoderCreate();


    [LibraryImport(LibraryName)]
    public static unsafe partial void avifDecoderDestroy(avifDecoder* decoder);


    /// <summary>
    /// Simple interfaces to decode a single image, independent of the decoder afterwards (decoder may be destroyed).
    /// call avifDecoderSetIO*() first
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderRead(avifDecoder* decoder, avifImage* image);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderReadMemory(avifDecoder* decoder, avifImage* image, IntPtr data, nuint size);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderReadFile(avifDecoder* decoder, avifImage* image, [MarshalAs(UnmanagedType.LPStr)] string filename);



    /// <summary>
    /// Multi-function alternative to avifDecoderRead() for image sequences and gaining direct access
    /// to the decoder's YUV buffers (for performance's sake). Data passed into avifDecoderParse() is NOT
    /// copied, so it must continue to exist until the decoder is destroyed.
    ///
    /// Usage / function call order is:
    /// * avifDecoderCreate()
    /// * avifDecoderSetSource() - optional, the default (AVIF_DECODER_SOURCE_AUTO) is usually sufficient
    /// * avifDecoderSetIO*()
    /// * avifDecoderParse()
    /// * avifDecoderNextImage() - in a loop, using decoder->image after each successful call
    /// * avifDecoderDestroy()
    ///
    /// NOTE: Until avifDecoderParse() returns AVIF_RESULT_OK, no data in avifDecoder should
    ///       be considered valid, and no queries (such as Keyframe/Timing/MaxExtent) should be made.
    ///
    /// You can use avifDecoderReset() any time after a successful call to avifDecoderParse()
    /// to reset the internal decoder back to before the first frame. Calling either
    /// avifDecoderSetSource() or avifDecoderParse() will automatically Reset the decoder.
    ///
    /// avifDecoderSetSource() allows you not only to choose whether to parse tracks or
    /// items in a file containing both, but switch between sources without having to
    /// Parse again. Normally AVIF_DECODER_SOURCE_AUTO is enough for the common path.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderSetSource(avifDecoder* decoder, avifDecoderSource source);



    [LibraryImport(LibraryName)]
    public static unsafe partial void avifDecoderSetIO(avifDecoder* decoder, avifIO* io);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderSetIOMemory(avifDecoder* decoder, IntPtr data, nuint size);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderSetIOFile(avifDecoder* decoder, [MarshalAs(UnmanagedType.LPStr)] string filename);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderParse(avifDecoder* decoder);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderNextImage(avifDecoder* decoder);



    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderNthImage(avifDecoder* decoder, uint frameIndex);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderReset(avifDecoder* decoder);


    /// <summary>
    /// Keyframe information
    /// frameIndex - 0-based, matching avifDecoder->imageIndex, bound by avifDecoder->imageCount
    /// "nearest" keyframe means the keyframe prior to this frame index (returns frameIndex if it is a keyframe)
    /// These functions may be used after a successful call (AVIF_RESULT_OK) to avifDecoderParse().
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="frameIndex"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifDecoderIsKeyframe(avifDecoder* decoder, uint frameIndex);


    /// <summary>
    /// Timing helper - This does not change the current image or invoke the codec (safe to call repeatedly)
    /// This function may be used after a successful call (AVIF_RESULT_OK) to avifDecoderParse().
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="frameIndex"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifDecoderNearestKeyframe(avifDecoder* decoder, uint frameIndex);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderNthImageTiming(avifDecoder* decoder, uint frameIndex, avifImageTiming* outTiming);


    /// <summary>
    /// When avifDecoderNextImage() or avifDecoderNthImage() returns AVIF_RESULT_WAITING_ON_IO, this
    /// function can be called next to retrieve the number of top rows that can be immediately accessed
    /// from the luma plane of decoder->image, and alpha if any. The corresponding rows from the chroma planes,
    /// if any, can also be accessed (half rounded up if subsampled, same number of rows otherwise).
    /// If a gain map is present and  (imageContentToDecode &amp; AVIF_IMAGE_CONTENT_GAIN_MAP) is nonzero,
    /// the gain map's planes can also be accessed in the same way.
    /// If the gain map's height is different from the main image, then the number of available gain map
    /// rows is at least:
    /// roundf((float)decoded_row_count / decoder->image->height * decoder->image->gainMap.image->height)
    /// When gain map scaling is needed, callers might choose to use a few less rows depending on how many rows
    /// are needed by the scaling algorithm, to avoid the last row(s) changing when more data becomes available.
    /// decoder->allowIncremental must be set to true before calling avifDecoderNextImage() or
    /// avifDecoderNthImage(). Returns decoder->image->height when the last call to avifDecoderNextImage() or
    /// avifDecoderNthImage() returned AVIF_RESULT_OK. Returns 0 in all other cases.
    /// WARNING: Experimental feature.
    /// </summary>
    /// <param name="decoder"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifDecoderDecodedRowCount(avifDecoder* decoder);



    /// <summary>
    /// Streaming data helper - Use this to calculate the maximal AVIF data extent encompassing all AV1
    /// sample data needed to decode the Nth image. The offset will be the earliest offset of all
    /// required AV1 extents for this frame, and the size will create a range including the last byte of
    /// the last AV1 sample needed. Note that this extent may include non-sample data, as a frame's
    /// sample data may be broken into multiple extents and interleaved with other data, or in
    /// non-sequential order. This extent will also encompass all AV1 samples that this frame's sample
    /// depends on to decode (such as samples for reference frames), from the nearest keyframe up to this
    /// Nth frame.
    ///
    /// If avifDecoderNthImageMaxExtent() returns AVIF_RESULT_OK and the extent's size is 0 bytes, this
    /// signals that libavif doesn't expect to call avifIO's Read for this frame's decode. This happens if
    /// data for this frame was read as a part of avifDecoderParse() (typically in an idat box inside of
    /// a meta box).
    ///
    /// This function may be used after a successful call (AVIF_RESULT_OK) to avifDecoderParse().
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="frameIndex"></param>
    /// <param name="outExtent"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifDecoderNthImageMaxExtent(avifDecoder* decoder, uint frameIndex, avifExtent* outExtent);


    /// <summary>
    /// Creates an encoder initialized with default settings values.
    /// Returns NULL if a memory allocation failed.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifEncoder* avifEncoderCreate();


    /// <summary>
    /// Encodes and writes a single image to `output`.
    /// On success (AVIF_RESULT_OK), `output` must be freed with avifRWDataFree().
    /// For more complex use cases, see `avifEncoderAddImage()` and `avifEncoderAddImageGrid()` below.
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="image"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifEncoderWrite(avifEncoder* encoder, avifImage* image, avifRWData* output);


    [LibraryImport(LibraryName)]
    public static unsafe partial void avifEncoderDestroy(avifEncoder* encoder);





    /// <summary>
    /// Multi-function alternative to avifEncoderWrite() for advanced features.
    ///
    /// Usage / function call order is:
    /// * avifEncoderCreate()
    /// - Still image:
    ///   * avifEncoderAddImage() [exactly once]
    /// - Still image grid:
    ///   * avifEncoderAddImageGrid() [exactly once, AVIF_ADD_IMAGE_FLAG_SINGLE is assumed]
    /// - Image sequence (animation):
    ///   * Set encoder->timescale (Hz) correctly
    ///   * avifEncoderAddImage() ... [repeatedly; at least once]
    /// - Still layered image:
    ///   * Set encoder->extraLayerCount correctly
    ///   * avifEncoderAddImage() ... [exactly encoder->extraLayerCount+1 times]
    /// - Still layered grid:
    ///   * Set encoder->extraLayerCount correctly
    ///   * avifEncoderAddImageGrid() ... [exactly encoder->extraLayerCount+1 times]
    /// * avifEncoderFinish()
    /// * avifEncoderDestroy()
    ///
    /// The image passed to avifEncoderAddImage() or avifEncoderAddImageGrid() is encoded during the
    /// call (which may be slow) and can be freed after the function returns.
    ///
    /// durationInTimescales is ignored if AVIF_ADD_IMAGE_FLAG_SINGLE is set in addImageFlags,
    /// or if we are encoding a layered image.
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="image"></param>
    /// <param name="durationInTimescales"></param>
    /// <param name="addImageFlags"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifEncoderAddImage(avifEncoder* encoder, avifImage* image, ulong durationInTimescales, avifAddImageFlag addImageFlags);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifEncoderAddImageGrid(avifEncoder* encoder, uint gridCols, uint gridRows, avifImage** cellImages, avifAddImageFlag addImageFlags);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifEncoderFinish(avifEncoder* encoder, avifRWData* output);


    /// <summary>
    /// Codec-specific, optional "advanced" tuning settings, in the form of string key/value pairs,
    /// to be consumed by the codec in the next avifEncoderAddImage() call.
    /// See the codec documentation to know if a setting is persistent or applied only to the next frame.
    /// key must be non-NULL, but passing a NULL value will delete the pending key, if it exists.
    /// Setting an incorrect or unknown option for the current codec will cause errors of type
    /// AVIF_RESULT_INVALID_CODEC_SPECIFIC_OPTION from avifEncoderWrite() or avifEncoderAddImage().
    /// </summary>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifEncoderSetCodecSpecificOption(avifEncoder* encoder, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);

    /// <summary>
    /// Returns the size in bytes of the AV1 image item containing gain map samples, or 0 if no gain map was encoded.
    /// </summary>
    /// <param name="encoder"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial nuint avifEncoderGetGainMapSizeBytes(avifEncoder* encoder);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifImageUsesU16(avifImage* image);


    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifImageIsOpaque(avifImage* image);


    [LibraryImport(LibraryName)]
    public static unsafe partial byte* avifImagePlane(avifImage* image, int channel);


    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifImagePlaneRowBytes(avifImage* image, int channel);


    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifImagePlaneWidth(avifImage* image, int channel);


    [LibraryImport(LibraryName)]
    public static unsafe partial uint avifImagePlaneHeight(avifImage* image, int channel);


    /// <summary>
    /// Returns AVIF_TRUE if input begins with a valid FileTypeBox (ftyp) that supports
    /// either the brand 'avif' or 'avis' (or both), without performing any allocations.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifBool avifPeekCompatibleFileType(avifROData* input);



    /// <summary>
    /// Performs tone mapping on a base image using the provided gain map.
    /// The HDR headroom is log2 of the ratio of HDR to SDR white brightness of the display to tone map for.
    /// 'toneMappedImage' should have the 'format', 'depth', and 'isFloat' fields set to the desired values.
    /// If non NULL, 'clli' will be filled with the light level information of the tone mapped image.
    /// NOTE: only used in tests for now, might be added to the public API at some point.
    /// </summary>
    /// <param name="baseImage"></param>
    /// <param name="gainMap"></param>
    /// <param name="hdrHeadroom"></param>
    /// <param name="outputColorPrimaries"></param>
    /// <param name="outputTransferCharacteristics"></param>
    /// <param name="toneMappedImage"></param>
    /// <param name="clli"></param>
    /// <param name="diag"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageApplyGainMap(avifImage* baseImage,
                                                                  avifGainMap* gainMap,
                                                                  float hdrHeadroom,
                                                                  avifColorPrimaries outputColorPrimaries,
                                                                  avifTransferCharacteristics outputTransferCharacteristics,
                                                                  avifRGBImage* toneMappedImage,
                                                                  avifContentLightLevelInformationBox* clli,
                                                                  avifDiagnostics* diag);



    /// <summary>
    /// Same as above but takes an avifRGBImage as input instead of avifImage.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRGBImageApplyGainMap(avifRGBImage* baseImage,
                                                                     avifColorPrimaries baseColorPrimaries,
                                                                     avifTransferCharacteristics baseTransferCharacteristics,
                                                                     avifGainMap* gainMap,
                                                                     float hdrHeadroom,
                                                                     avifColorPrimaries outputColorPrimaries,
                                                                     avifTransferCharacteristics outputTransferCharacteristics,
                                                                     avifRGBImage* toneMappedImage,
                                                                     avifContentLightLevelInformationBox* clli,
                                                                     avifDiagnostics* diag);


    /// <summary>
    /// Computes a gain map between two images: a base image and an alternate image.
    /// Both images should have the same width and height, and use the same color
    /// primaries. TODO(maryla): allow different primaries.
    /// gainMap->image should be initialized with avifImageCreate(), with the width,
    /// height, depth and yuvFormat fields set to the desired output values for the
    /// gain map. All of these fields may differ from the source images.
    /// </summary>
    /// <param name="baseRgbImage"></param>
    /// <param name="baseColorPrimaries"></param>
    /// <param name="baseTransferCharacteristics"></param>
    /// <param name="altRgbImage"></param>
    /// <param name="altColorPrimaries"></param>
    /// <param name="altTransferCharacteristics"></param>
    /// <param name="gainMap"></param>
    /// <param name="diag"></param>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifRGBImageComputeGainMap(avifRGBImage* baseRgbImage,
                                                                       avifColorPrimaries baseColorPrimaries,
                                                                       avifTransferCharacteristics baseTransferCharacteristics,
                                                                       avifRGBImage* altRgbImage,
                                                                       avifColorPrimaries altColorPrimaries,
                                                                       avifTransferCharacteristics altTransferCharacteristics,
                                                                       avifGainMap* gainMap,
                                                                       avifDiagnostics* diag);



    /// <summary>
    /// Convenience function. Same as above but takes avifImage images as input
    /// instead of avifRGBImage. Gain map computation is performed in RGB space so
    /// the images are converted to RGB first.
    /// </summary>
    /// <returns></returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial avifResult avifImageComputeGainMap(avifImage* baseImage,
                                                                    avifImage* altImage,
                                                                    avifGainMap* gainMap,
                                                                    avifDiagnostics* diag);



}
