using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.ParallelRunner;
using System.Text;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Main class for Jpeg XL encoding.
/// </summary>
public class JxlEncoder : IDisposable
{

    /// <summary>
    /// Get the libjxl encoder version
    /// </summary>
    public static Version Version => GetEncoderVersion();


    private static Version GetEncoderVersion()
    {
        uint version = JxlEncoderNativeMethod.JxlEncoderVersion();
        uint patch = version % 1000;
        uint minor = (version / 1000) % 1000;
        uint major = version / 1000000;
        return new Version((int)major, (int)minor, (int)patch);
    }



    private JxlEncoderPtr _encoderPtr;

    private JxlThreadParallelRunnerPtr _parallelRunnerPtr;

    private IntPtr _jxlParallelRunnerFunction;

    private JxlCmsInterface _cmsInterface;


    /// <summary>
    /// Initializes a new instance of the <see cref="JxlEncoder"/> class.
    /// </summary>
    /// <exception cref="JxlEncodeException"></exception>
    public JxlEncoder()
    {
        _encoderPtr = JxlEncoderNativeMethod.JxlEncoderCreate();
        if (_encoderPtr == IntPtr.Zero)
        {
            throw new JxlEncodeException("Failed to create JxlEncoder.");
        }
        _parallelRunnerPtr = JxlThreadParallelRunnerPtr.GetDefault();
        _jxlParallelRunnerFunction = JxlParallelRunnerNativeMethod.GetJxlThreadParallelRunner();
        _cmsInterface = JxlCmsInterface.GetDefault();
        JxlEncoderNativeMethod.JxlEncoderSetParallelRunner(_encoderPtr, _jxlParallelRunnerFunction, _parallelRunnerPtr);
        JxlEncoderNativeMethod.JxlEncoderSetCms(_encoderPtr, _cmsInterface);
    }


    /// <summary>
    /// Sets the basic image information for the encoder. This includes image dimensions, bit depth, and other essential metadata.
    /// This is the first step after creating an encoder.
    /// </summary>
    /// <param name="basicInfo">The basic image information.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the basic info.</exception>
    public void SetBasicInfo(in JxlBasicInfo basicInfo)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetBasicInfo(_encoderPtr, basicInfo);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set basic info.");
        }
    }


    /// <summary>
    /// Sets the color encoding information for the image.
    /// </summary>
    /// <param name="colorEncoding">The color encoding information.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the color encoding.</exception>
    public void SetColorEncoding(in JxlColorEncoding colorEncoding)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetColorEncoding(_encoderPtr, colorEncoding);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set color encoding.");
        }
    }


    /// <summary>
    /// Sets the ICC color profile for the image.
    /// </summary>
    /// <param name="iccProfile">A span containing the raw ICC profile data.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the ICC profile.</exception>
    public void SetICCProfile(ReadOnlySpan<byte> iccProfile)
    {
        unsafe
        {
            fixed (byte* p = iccProfile)
            {
                var status = JxlEncoderNativeMethod.JxlEncoderSetICCProfile(_encoderPtr, (IntPtr)p, (nuint)iccProfile.Length);
                if (status != JxlEncoderStatus.Success)
                {
                    throw new JxlEncodeException($"Failed to set ICC profile.");
                }
            }
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether to encode the image into a container format (e.g., BMFF).
    /// <para>Default is <see langword="false"/>.</para>
    /// </summary>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set this option.</exception>
    public bool UseContainer
    {
        get;
        set
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderUseContainer(_encoderPtr, value);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set UseContainer.");
            }
            field = value;
        }
    }


    /// <summary>
    /// Gets or sets the codestream level.
    /// <para>Valid values are -1 (encoder default), 5, or 10.</para>
    /// <para>Default is -1.</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not -1, 5, or 10.</exception>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the level.</exception>
    public int CodeStreamLevel
    {
        get;
        set
        {
            if (value is -1 or 5 or 10)
            {
                JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetCodestreamLevel(_encoderPtr, value);
                if (status != JxlEncoderStatus.Success)
                {
                    throw new JxlEncodeException($"Failed to set CodeStreamLevel.");
                }
                field = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(CodeStreamLevel), "CodeStreamLevel must be -1, 5 or 10.");
            }
        }
    } = -1;


    /// <summary>
    /// Gets or sets a value indicating whether to store JPEG reconstruction metadata in the codestream.
    /// This is necessary for lossless reconstruction of JPEG files.
    /// <para>Default is <see langword="false"/>.</para>
    /// </summary>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set this option.</exception>
    public bool StoreJpegMetadata
    {
        get;
        set
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderStoreJPEGMetadata(_encoderPtr, value);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set StoreJpegMetadata.");
            }
            field = value;
        }
    }


    /// <summary>
    /// Creates a new <see cref="JxlEncoderFrameSettings"/> instance for configuring individual frames.
    /// </summary>
    /// <param name="sourceFrame">An optional source frame settings to copy from.</param>
    /// <returns>A new <see cref="JxlEncoderFrameSettings"/> instance.</returns>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to create the settings object.</exception>
    public JxlEncoderFrameSettings CreateFrameSettings(JxlEncoderFrameSettings? sourceFrame = null)
    {
        JxlEncoderFrameSettingsPtr frameSettingsPtr = JxlEncoderNativeMethod.JxlEncoderFrameSettingsCreate(_encoderPtr, sourceFrame?.GetPtr() ?? default);
        if (frameSettingsPtr == IntPtr.Zero)
        {
            throw new JxlEncodeException("Failed to create JxlEncoderFrameSettings.");
        }
        return new JxlEncoderFrameSettings(frameSettingsPtr);
    }


    /// <summary>
    /// Sets the information for an extra channel at a specific index.
    /// </summary>
    /// <param name="index">The index of the extra channel.</param>
    /// <param name="extraChannelInfo">The extra channel information.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the info.</exception>
    public void SetExtraChannelInfo(nuint index, in JxlExtraChannelInfo extraChannelInfo)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetExtraChannelInfo(_encoderPtr, index, extraChannelInfo);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set extra channel info.");
        }
    }

    /// <summary>
    /// Sets the name for an extra channel at a specific index.
    /// </summary>
    /// <param name="index">The index of the extra channel.</param>
    /// <param name="name">The name of the extra channel.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the name.</exception>
    public unsafe void SetExtraChannalName(nuint index, string name)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(name);
        fixed (byte* p = bytes)
        {
            JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetExtraChannelName(_encoderPtr, index, (IntPtr)p, (nuint)bytes.Length);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to set extra channel name.");
            }
        }
    }



    /// <summary>
    /// Adds a metadata box to the container.
    /// <para>This requires <see cref="UseContainer"/> to be <see langword="true"/>.</para>
    /// </summary>
    /// <param name="type">The 4-character box type.</param>
    /// <param name="content">The raw content of the box.</param>
    /// <param name="compressBox">Whether to compress the box content.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to add the box.</exception>
    public unsafe void AddBox(JxlBoxType type, ReadOnlySpan<byte> content, bool compressBox)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderUseBoxes(_encoderPtr);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to enable boxes.");
        }
        fixed (byte* p = content)
        {
            status = JxlEncoderNativeMethod.JxlEncoderAddBox(_encoderPtr, type, (IntPtr)p, (nuint)content.Length, compressBox);
            if (status != JxlEncoderStatus.Success)
            {
                throw new JxlEncodeException($"Failed to add box.");
            }
        }
    }


    /// <summary>
    /// Enables expert options, which may not be suitable for general use.
    /// </summary>
    public void AllowExpertOptions()
    {
        JxlEncoderNativeMethod.JxlEncoderAllowExpertOptions(_encoderPtr);
    }


    /// <summary>
    /// Signals that no more animation frames will be added.
    /// </summary>
    public void CloseFrames()
    {
        JxlEncoderNativeMethod.JxlEncoderCloseFrames(_encoderPtr);
    }

    /// <summary>
    /// Signals that no more metadata boxes will be added.
    /// </summary>
    public void CloseBoxes()
    {
        JxlEncoderNativeMethod.JxlEncoderCloseBoxes(_encoderPtr);
    }

    /// <summary>
    /// Signals that no more image data will be added.
    /// </summary>
    public void CloseInput()
    {
        JxlEncoderNativeMethod.JxlEncoderCloseInput(_encoderPtr);
    }



    /// <summary>
    /// Encodes the image and returns the compressed data as a byte array.
    /// </summary>
    /// <returns>A byte array containing the compressed JXL data.</returns>
    public byte[] Encode()
    {
        MemoryStream ms = new MemoryStream();
        Encode(ms);
        return ms.ToArray();
    }


    /// <summary>
    /// Encodes the image and writes the compressed data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write the compressed data to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the encoder encounters an error during processing.</exception>
    public void Encode(Stream stream)
    {
        CloseInput();
        byte[] buffer = new byte[8192];
        JxlEncoderStatus status;
        while (true)
        {
            unsafe
            {
                nuint bufferSize = (nuint)buffer.Length;
                fixed (byte* pBuffer = buffer)
                {
                    nint bufferPtr = (nint)pBuffer;
                    status = JxlEncoderNativeMethod.JxlEncoderProcessOutput(_encoderPtr, ref bufferPtr, ref bufferSize);
                    var span = new ReadOnlySpan<byte>(pBuffer, buffer.Length - (int)bufferSize);
                    if (span.Length > 0)
                    {
                        stream.Write(span);
                    }
                    if (status == JxlEncoderStatus.Success)
                    {
                        break;
                    }
                    if (status == JxlEncoderStatus.Error)
                    {
                        throw new InvalidOperationException("JxlEncoderProcessOutput failed with error status.");
                    }
                }
            }
        }
    }


    /// <summary>
    /// Sets a custom output processor for handling the compressed data.
    /// </summary>
    /// <param name="outputProcessor">The output processor to use.</param>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to set the output processor.</exception>
    public void SetOutputProcessor(JxlEncoderOutputProcessor outputProcessor)
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderSetOutputProcessor(_encoderPtr, outputProcessor);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to set output processor.");
        }
    }


    /// <summary>
    /// Flushes any buffered input data.
    /// </summary>
    /// <exception cref="JxlEncodeException">Thrown if the native encoder fails to flush the input.</exception>
    public void FlushInput()
    {
        JxlEncoderStatus status = JxlEncoderNativeMethod.JxlEncoderFlushInput(_encoderPtr);
        if (status != JxlEncoderStatus.Success)
        {
            throw new JxlEncodeException($"Failed to flush input.");
        }
    }



    private bool disposedValue;

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="JxlEncoder"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // 释放托管状态(托管对象)
            }

            // 释放未托管的资源(未托管的对象)并重写终结器
            // 将大型字段设置为 null
            if (_encoderPtr != IntPtr.Zero)
            {
                JxlEncoderNativeMethod.JxlEncoderDestroy(_encoderPtr);
                _encoderPtr = IntPtr.Zero;
            }
            if (_parallelRunnerPtr != IntPtr.Zero)
            {
                JxlParallelRunnerNativeMethod.JxlThreadParallelRunnerDestroy(_parallelRunnerPtr);
                _parallelRunnerPtr = IntPtr.Zero;
            }
            disposedValue = true;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="JxlEncoder"/> class.
    /// </summary>
    ~JxlEncoder()
    {
        // 不要更改此代码。请将清理代码放入"Dispose(bool disposing)"方法中
        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        Dispose(disposing: false);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="JxlEncoder"/>.
    /// </summary>
    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }


}
