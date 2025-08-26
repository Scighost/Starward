using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// This struct provides callback functions to pass pixel data in a streaming
/// manner instead of requiring the entire frame data in memory at once.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct JxlChunkedFrameInputSource
{
    /// <summary>
    /// A pointer to any user-defined data or state. This can be used to pass
    /// information to the callback functions.
    /// </summary>
    public IntPtr opaque;

    /// <summary>
    /// <para>
    /// <see cref="JpegxlGetColorChannelsPixelFormatFunc"/>
    /// Get the pixel format that color channel data will be provided in.
    /// When called, pixel_format points to a suggested pixel format; if
    /// color channel data can be given in this pixel format, processing might
    /// be more efficient.
    /// </para>
    /// <para>
    /// This function will be called exactly once, before any call to get_color_channel_at.
    /// </para>
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, JxlPixelFormat*, void> get_color_channels_pixel_format;

    /// <summary>
    /// <para>
    /// <see cref="JpegxlGetColorChannelDataAtFunc"/>
    /// Callback to retrieve a rectangle of color channel data at a specific
    /// location. It is guaranteed that xpos and ypos are multiples of 8. xsize,
    /// ysize will be multiples of 8, unless the resulting rectangle would be out
    /// of image bounds. Moreover, xsize and ysize will be at most 2048. The
    /// returned data will be assumed to be in the format returned by the
    /// (preceding) call to get_color_channels_pixel_format, except the align
    /// parameter of the pixel format will be ignored. Instead, the i-th row will
    /// be assumed to start at position return_value + i * *row_offset, with the
    /// value of *row_offset decided by the callee.
    /// </para>
    /// <para>
    /// Note that multiple calls to get_color_channel_data_at may happen before a
    /// call to release_buffer.
    /// </para>
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, nuint, nuint, nuint, nuint, nuint*, void*> get_color_channel_data_at;

    /// <summary>
    /// <para>
    /// <see cref="JpegxlGetExtraChannelPixelFormatFunc"/>
    /// Get the pixel format that extra channel data will be provided in.
    /// When called, pixel_format points to a suggested pixel format; if
    /// extra channel data can be given in this pixel format, processing might
    /// be more efficient.
    /// </para>
    /// <para>
    /// This function will be called exactly once per index, before any call to
    /// get_extra_channel_data_at with that given index.
    /// </para>
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, nuint, JxlPixelFormat*, void> get_extra_channel_pixel_format;

    /// <summary>
    /// <para>
    /// <see cref="JpegxlGetExtraChannelDataAtFunc"/>
    /// Callback to retrieve a rectangle of extra channel ec_index data at a
    /// specific location. It is guaranteed that xpos and ypos are multiples of
    /// 8. xsize, ysize will be multiples of 8, unless the resulting rectangle
    /// would be out of image bounds. Moreover, xsize and ysize will be at most
    /// 2048. The returned data will be assumed to be in the format returned by the
    /// (preceding) call to get_extra_channels_pixel_format_at with the
    /// corresponding extra channel index ec_index, except the align parameter
    /// of the pixel format will be ignored. Instead, the i-th row will be
    /// assumed to start at position return_value + i * *row_offset, with the
    /// value of *row_offset decided by the callee.
    /// </para>
    /// <para>
    /// Note that multiple calls to get_extra_channel_data_at may happen before a
    /// call to release_buffer.
    /// </para>
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, nuint, nuint, nuint, nuint, nuint, nuint*, void*> get_extra_channel_data_at;

    /// <summary>
    /// <see cref="JpegxlReleaseBufferFunc"/>
    /// Releases the buffer buf (obtained through a call to
    /// get_color_channel_data_at or get_extra_channel_data_at). This function
    /// will be called exactly once per call to get_color_channel_data_at or
    /// get_extra_channel_data_at.
    /// </summary>
    public delegate* unmanaged[Cdecl]<void*, void*, void> release_buffer;


    /// <summary>
    /// <para>
    /// Get the pixel format that color channel data will be provided in.
    /// When called, pixel_format points to a suggested pixel format; if
    /// color channel data can be given in this pixel format, processing might
    /// be more efficient.
    /// </para>
    /// <para>
    /// This function will be called exactly once, before any call to
    /// get_color_channel_at.
    /// </para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="pixel_format">format for pixels</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlGetColorChannelsPixelFormatFunc(void* opaque, JxlPixelFormat* pixel_format);


    /// <summary>
    /// <para>
    /// Callback to retrieve a rectangle of color channel data at a specific
    /// location. It is guaranteed that xpos and ypos are multiples of 8. xsize,
    /// ysize will be multiples of 8, unless the resulting rectangle would be out
    /// of image bounds. Moreover, xsize and ysize will be at most 2048. The
    /// returned data will be assumed to be in the format returned by the
    /// (preceding) call to get_color_channels_pixel_format, except the align
    /// parameter of the pixel format will be ignored. Instead, the i-th row will
    /// be assumed to start at position return_value + i * *row_offset, with the
    /// value of *row_offset decided by the callee.
    /// </para>
    /// <para>
    /// Note that multiple calls to get_color_channel_data_at may happen before a
    /// call to release_buffer.
    /// </para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="xpos">horizontal position for the data.</param>
    /// <param name="ypos">vertical position for the data.</param>
    /// <param name="xsize">horizontal size of the requested rectangle of data.</param>
    /// <param name="ysize">vertical size of the requested rectangle of data.</param>
    /// <param name="row_offset">pointer to a the byte offset between consecutive rows of
    /// the retrieved pixel data.</param>
    /// <returns>pointer to the retrieved pixel data.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* JpegxlGetColorChannelDataAtFunc(void* opaque, nuint xpos, nuint ypos, nuint xsize, nuint ysize, nuint* row_offset);


    /// <summary>
    /// <para>
    /// Get the pixel format that extra channel data will be provided in.
    /// When called, pixel_format points to a suggested pixel format; if
    /// extra channel data can be given in this pixel format, processing might
    /// be more efficient.
    /// </para>
    /// <para>
    /// This function will be called exactly once per index, before any call to
    /// get_extra_channel_data_at with that given index.
    /// </para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="ec_index">zero-indexed index of the extra channel</param>
    /// <param name="pixel_format">format for extra channel data</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlGetExtraChannelPixelFormatFunc(void* opaque, nuint ec_index, JxlPixelFormat* pixel_format);


    /// <summary>
    /// <para>
    /// Callback to retrieve a rectangle of extra channel ec_index data at a
    /// specific location. It is guaranteed that xpos and ypos are multiples of
    /// 8. xsize, ysize will be multiples of 8, unless the resulting rectangle
    /// would be out of image bounds. Moreover, xsize and ysize will be at most
    /// 2048. The returned data will be assumed to be in the format returned by the
    /// (preceding) call to get_extra_channels_pixel_format_at with the
    /// corresponding extra channel index ec_index, except the align parameter
    /// of the pixel format will be ignored. Instead, the i-th row will be
    /// assumed to start at position return_value + i * *row_offset, with the
    /// value of *row_offset decided by the callee.
    /// </para>
    /// <para>
    /// Note that multiple calls to get_extra_channel_data_at may happen before a
    /// call to release_buffer.
    /// </para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="ec_index">zero-indexed index of the extra channel</param>
    /// <param name="xpos">horizontal position for the data.</param>
    /// <param name="ypos">vertical position for the data.</param>
    /// <param name="xsize">horizontal size of the requested rectangle of data.</param>
    /// <param name="ysize">vertical size of the requested rectangle of data.</param>
    /// <param name="row_offset">pointer to a the byte offset between consecutive rows of
    /// the retrieved pixel data.</param>
    /// <returns>pointer to the retrieved pixel data.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* JpegxlGetExtraChannelDataAtFunc(void* opaque, nuint ec_index, nuint xpos, nuint ypos, nuint xsize, nuint ysize, nuint* row_offset);


    /// <summary>
    /// Releases the buffer buf (obtained through a call to
    /// get_color_channel_data_at or get_extra_channel_data_at). This function
    /// will be called exactly once per call to get_color_channel_data_at or
    /// get_extra_channel_data_at.
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="buf">pointer returned by get_color_channel_data_at or
    /// get_extra_channel_data_at</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlReleaseBufferFunc(void* opaque, void* buf);
}

