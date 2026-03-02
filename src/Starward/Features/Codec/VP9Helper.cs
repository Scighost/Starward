using Microsoft.Win32;
using Starward.Codec.VP9Decoder;
using System;
using System.Buffers;
using System.IO;

namespace Starward.Features.Codec;

public partial class VP9Helper
{


    public static bool VP9MFTRegistered { get; private set; }

    public static bool VorbisMFTRegistered { get; private set; }

    private static bool _videoBackground;


    public static void RegisterVP9Decoder(bool videoBackground = false)
    {
        try
        {
            if (!VP9MFTRegistered)
            {
                int hr = VP9Decoder.RegisterVP9DecoderLocal();
                if (hr >= 0)
                {
                    VP9MFTRegistered = true;
                }
            }
            if (videoBackground)
            {
                _videoBackground = true;
            }
        }
        catch { }
    }


    public static void UnregisterVP9Decoder(bool videoBackground = false)
    {
        try
        {
            if (_videoBackground && !videoBackground)
            {
                return;
            }
            if (VP9MFTRegistered)
            {
                int hr = VP9Decoder.UnregisterVP9DecoderLocal();
            }
            VP9MFTRegistered = false;
        }
        catch { }
    }


    public static void RegisterVorbisDecoder()
    {
        try
        {
            if (!VorbisMFTRegistered)
            {
                int hr = VP9Decoder.RegisterVorbisDecoderLocal();
                if (hr >= 0)
                {
                    VorbisMFTRegistered = true;
                }
            }
        }
        catch { }
    }


    public static void UnregisterVorbisDecoder()
    {
        try
        {
            if (VorbisMFTRegistered)
            {
                int hr = VP9Decoder.UnregisterVorbisDecoderLocal();
            }
            VorbisMFTRegistered = false;
        }
        catch { }
    }


    /// <summary>
    /// 是否安装 VP9 Video Extensions
    /// </summary>
    /// <returns></returns>
    public static bool IsVP9DecoderInstalled()
    {
        try
        {
            var packages = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
            if (packages is not null)
            {
                string[] names = packages.GetSubKeyNames();
                foreach (var item in names)
                {
                    if (item.StartsWith("Microsoft.VP9VideoExtensions"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }



    /// <summary>
    /// 判断给定的文件是否是 VP9 编码的 Profile 1/2/3 视频文件（高 Profile）
    /// </summary>
    public static bool IsVP9HighProfile(string filePath)
    {
        const int ReadSize = 1 << 20;
        byte[]? rented = null;
        try
        {
            using var fs = File.OpenRead(filePath);
            rented = ArrayPool<byte>.Shared.Rent(ReadSize);
            int read = fs.Read(rented, 0, ReadSize);
            ReadOnlySpan<byte> buffer = rented.AsSpan(0, read);

            if (!IsWebMFile(buffer))
            {
                return false;
            }

            // 查找 SimpleBlock (0xA3) 或 Block (0xA1)
            while (buffer.Length > 0)
            {
                int index = buffer.IndexOfAny((byte)0xA1, (byte)0xA3);
                if (index < 0)
                {
                    break;
                }
                buffer = buffer.Slice(index + 1);
                // 跳过 block size (EBML VINT)
                if (!SkipEBMLVInt(buffer, out int offset))
                {
                    continue;
                }
                buffer = buffer.Slice(offset);
                // 跳过 track number (EBML VINT)
                if (!SkipEBMLVInt(buffer, out offset))
                {
                    continue;
                }
                buffer = buffer.Slice(offset);
                // 跳过 timecode (2 bytes) 和 flags (1 byte)
                if (buffer.Length < 3)
                {
                    continue;
                }
                buffer = buffer.Slice(3);

                if (buffer.Length > 0)
                {
                    int profile = ParseVP9Profile(buffer);
                    if (profile >= 0)
                    {
                        return profile != 0;
                    }
                    // profile == -1 表示无法从当前字节解析帧头，继续查找下一个块
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }


    /// <summary>
    /// 判断给定的文件是否是 VP8 编码的 WebM 视频文件
    /// </summary>
    public static bool IsVP8VideoFile(string filePath)
    {
        const int ReadSize = 1 << 20;
        byte[]? rented = null;
        try
        {
            using var fs = File.OpenRead(filePath);
            rented = ArrayPool<byte>.Shared.Rent(ReadSize);
            int read = fs.Read(rented, 0, ReadSize);
            ReadOnlySpan<byte> buffer = rented.AsSpan(0, read);

            if (!IsWebMFile(buffer))
            {
                return false;
            }

            // 在 Tracks 段中查找 CodecID 元素 (0x86) 包含 "V_VP8" 的记录
            return buffer.IndexOf("V_VP8"u8) >= 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }


    private static bool IsWebMFile(ReadOnlySpan<byte> buffer)
    {
        // EBML magic: 0x1A 0x45 0xDF 0xA3
        ReadOnlySpan<byte> ebmlMagic = [0x1A, 0x45, 0xDF, 0xA3];
        if (!buffer.StartsWith(ebmlMagic))
        {
            return false;
        }
        // DocType element: ID=0x4282, Size=0x84 (4 bytes), Value="webm"
        ReadOnlySpan<byte> docTypePrefix = [0x42, 0x82, 0x84];
        int index = buffer.IndexOf(docTypePrefix);
        if (index < 0 || index + docTypePrefix.Length + 4 > buffer.Length)
        {
            return false;
        }
        return buffer.Slice(index + docTypePrefix.Length, 4).SequenceEqual("webm"u8);
    }


    /// <summary>
    /// 从 VP9 帧头解析 Profile 值（0~3），返回 -1 表示不是有效的 VP9 帧头
    /// </summary>
    private static int ParseVP9Profile(ReadOnlySpan<byte> buffer)
    {
        byte firstByte = buffer[0];

        // frame_marker 占最高 2 位，固定为 0b10
        if (((firstByte >> 6) & 0x03) != 2)
        {
            return -1;
        }

        // profile_low_bit: bit5，profile_high_bit: bit4
        int profile = ((firstByte >> 3) & 0x02) | ((firstByte >> 5) & 0x01);
        return profile; // 0~3 均合法
    }


    /// <summary>
    /// 跳过一个 EBML VINT，返回其占用的字节数
    /// </summary>
    private static bool SkipEBMLVInt(ReadOnlySpan<byte> buffer, out int offset)
    {
        offset = 0;
        if (buffer.Length == 0)
        {
            return false;
        }
        byte firstByte = buffer[0];
        for (int i = 0; i < 8; i++)
        {
            if ((firstByte & (0x80 >> i)) != 0)
            {
                offset = i + 1;
                return offset <= buffer.Length;
            }
        }
        return false;
    }


}
