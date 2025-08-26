using Starward.Codec.JpegXL.CodeStream;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// A class that holds the settings for a single frame when encoding a Jpeg XL image.
/// </summary>
public class JxlEncoderFrameSettings
{

    private JxlEncoderFrameSettingsPtr _frameSettingsPtr;


    internal JxlEncoderFrameSettingsPtr GetPtr() => _frameSettingsPtr;


    internal JxlEncoderFrameSettings(JxlEncoderFrameSettingsPtr frameSettingsPtr)
    {
        _frameSettingsPtr = frameSettingsPtr;
        if (_frameSettingsPtr == IntPtr.Zero)
        {
            throw new JxlEncodeException("Failed to create JxlEncoderFrame.");
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether to use lossless encoding for this frame.
    /// </summary>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the value.</exception>
    public bool Lossless
    {
        get;
        set
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetFrameLossless(_frameSettingsPtr, value);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set Lossless.");
            }
            field = value;
        }
    } = false;


    /// <summary>
    /// Gets or sets the Butteraugli distance for this frame.
    /// <para>A lower distance means higher quality. 0.0 is lossless, 1.0 is a visually lossless sweet spot, and higher values are lower quality.</para>
    /// <para>The value is clamped between 0.0 and 25.0.</para>
    /// <para>Default is 1.0.</para>
    /// </summary>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the value.</exception>
    public float Distance
    {
        get;
        set
        {
            value = Math.Clamp(value, 0.0f, 25.0f);
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetFrameDistance(_frameSettingsPtr, value);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set Distance.");
            }
            field = value;
        }
    } = 1.0f;


    /// <summary>
    /// Gets or sets the quality of the frame, where 100 is lossless and 0 is the lowest quality.
    /// <para>This is an alternative to setting the <see cref="Distance"/> directly.</para>
    /// </summary>
    public float Quality
    {
        get => DistanceToQuality(Distance);
        set => Distance = QualityToDistance(value);
    }


    /// <summary>
    /// Sets a specific encoder option for this frame.
    /// </summary>
    /// <param name="settingId">The ID of the setting to change.</param>
    /// <param name="value">The integer value to set.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the option.</exception>
    public void SetOption(JxlEncoderFrameSettingId settingId, long value)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderFrameSettingsSetOption(_frameSettingsPtr, settingId, value);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set frame setting option: {settingId}");
        }
    }


    /// <summary>
    /// Sets a specific floating-point encoder option for this frame.
    /// </summary>
    /// <param name="settingId">The ID of the setting to change.</param>
    /// <param name="value">The float value to set.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the option.</exception>
    public void SetFloatOption(JxlEncoderFrameSettingId settingId, float value)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderFrameSettingsSetFloatOption(_frameSettingsPtr, settingId, value);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set frame setting float option: {settingId}");
        }
    }


    /// <summary>
    /// Overrides the frame header information for this frame.
    /// </summary>
    /// <param name="frameHeader">The frame header to set.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the header.</exception>
    public void SetFrameHeader(in JxlFrameHeader frameHeader)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetFrameHeader(_frameSettingsPtr, frameHeader);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set frame header.");
        }
    }


    /// <summary>
    /// Sets the name of this frame.
    /// </summary>
    /// <param name="name">The name to set.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the name.</exception>
    public void SetFrameName(string name)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetFrameName(_frameSettingsPtr, name);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set frame name.");
        }
    }

    /// <summary>
    /// Sets the bit depth for this frame.
    /// </summary>
    /// <param name="bitDepth">The bit depth information.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the bit depth.</exception>
    public void SetFrameBitDepth(in JxlBitDepth bitDepth)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetFrameBitDepth(_frameSettingsPtr, bitDepth);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set frame bit depth.");
        }
    }

    /// <summary>
    /// Sets the blend information for an extra channel.
    /// </summary>
    /// <param name="index">The index of the extra channel.</param>
    /// <param name="blendInfo">The blend information.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the blend info.</exception>
    public void SetExtraChannelBlendInfo(nuint index, in JxlBlendInfo blendInfo)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetExtraChannelBlendInfo(_frameSettingsPtr, index, blendInfo);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set extra channel blend info.");
        }
    }


    /// <summary>
    /// Adds an image frame to the encoder with the specified pixel format and buffer.
    /// </summary>
    /// <param name="pixelFormat">The pixel format of the input buffer.</param>
    /// <param name="buffer">A span containing the raw pixel data.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to add the frame.</exception>
    public unsafe void AddImageFrame(in JxlPixelFormat pixelFormat, ReadOnlySpan<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderAddImageFrame(_frameSettingsPtr, pixelFormat, (nint)p, (nuint)buffer.Length);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to add image frame.");
            }
        }
    }

    /// <summary>
    /// Adds a JPEG frame to the encoder for recompression.
    /// </summary>
    /// <param name="buffer">A span containing the raw JPEG data.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to add the frame.</exception>
    public unsafe void AddJpegFrame(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderAddJPEGFrame(_frameSettingsPtr, (nint)p, (nuint)buffer.Length);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to add JPEG frame.");
            }
        }
    }


    /// <summary>
    /// Adds a frame using a chunked input source, for streaming scenarios.
    /// </summary>
    /// <param name="isLastFrame">Whether this is the last frame of the image.</param>
    /// <param name="chunkedInputSource">The chunked input source providing the data.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to add the frame.</exception>
    public void AddChunkedFrame(bool isLastFrame, JxlChunkedFrameInputSource chunkedInputSource)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderAddChunkedFrame(_frameSettingsPtr, isLastFrame, chunkedInputSource);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to add chunked frame.");
        }
    }


    /// <summary>
    /// Sets the buffer for an extra channel.
    /// </summary>
    /// <param name="index">The index of the extra channel.</param>
    /// <param name="pixelFormat">The pixel format of the input buffer.</param>
    /// <param name="buffer">A span containing the raw pixel data for the extra channel.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the buffer.</exception>
    public unsafe void SetExtraChannelBuffer(uint index, in JxlPixelFormat pixelFormat, ReadOnlySpan<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetExtraChannelBuffer(_frameSettingsPtr, pixelFormat, (nint)p, (nuint)buffer.Length, index);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set extra channel buffer.");
            }
        }
    }



    private static float QualityToDistance(float quality)
    {
        double distance = quality switch
        {
            >= 100 => 0,
            >= 30 => 0.1 + 0.09 * (100 - quality),
            >= 0 => 53.0 / 3000.0 * quality * quality - 23.0 / 20.0 * quality + 25.0,
            _ => 25,
        };
        return (float)distance;
    }


    private static float DistanceToQuality(float distance)
    {
        double quality = distance switch
        {
            <= 0 => 100,
            < 6.4f => (910.0 - 100.0 * distance) / 9.0,
            < 25 => 1725.0 / 53.0 - 75.0 / 53.0 * Math.Sqrt((424 * distance - 2665) / 15.0),
            _ => 0,
        };
        return (float)quality;
    }


}
