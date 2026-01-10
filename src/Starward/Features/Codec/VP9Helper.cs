using Microsoft.Win32;
using Starward.Codec.VP9Decoder;
using System;
using System.IO;

namespace Starward.Features.Codec;

public class VP9Helper
{


    public static bool VP9MFTRegistered { get; private set; }

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



    /// <summary>
    /// 判断给定的文件是否是 WebM Profile 0 的视频文件
    /// </summary>
    public static bool IsWebmButNotProfile0(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            Span<byte> buffer = new byte[1 << 20];
            int read = fs.Read(buffer);
            buffer = buffer.Slice(0, read);

            if (!IsWebMFile(buffer))
            {
                return false;
            }

            // 查找 SimpleBlock (0xA3) 或 Block (0xA1)
            int index = -1;
            byte value0 = 0xA1, value1 = 0xA3;
            while ((index = buffer.IndexOfAny(value0, value1)) >= 0)
            {
                buffer = buffer.Slice(index + 1);
                // skip block size
                if (!SkipEBMLSize(buffer, out int offset))
                {
                    continue;
                }
                buffer = buffer.Slice(offset);
                // skip track number
                if (!SkipEBMLVInt(buffer, out offset))
                {
                    continue;
                }
                buffer = buffer.Slice(offset);
                // skip timecode (2 bytes) and flags (1 byte)
                buffer = buffer.Slice(3);

                if (buffer.Length > 1)
                {
                    int profile = ParseVP9Profile(buffer);
                    return profile != 0;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }


    private static bool IsWebMFile(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 4)
        {
            return false;
        }
        Span<byte> ebmlHeader = [0x1A, 0x45, 0xDF, 0xA3];
        if (!buffer.Slice(0, 4).SequenceEqual(ebmlHeader))
        {
            return false;
        }
        Span<byte> docType = [0x42, 0x82, 0x84];
        int index = buffer.IndexOf(docType);
        if (index < 0 || index + 4 > buffer.Length)
        {
            return false;
        }
        uint value = BitConverter.ToUInt32(buffer.Slice(index + 3, 4)) | 0x20202020;
        return value == 0x6D626577;
    }


    private static int ParseVP9Profile(ReadOnlySpan<byte> buffer)
    {
        byte firstByte = buffer[0];

        // 检查 frame_marker (最高 2 位应该是 0b10)
        int frameMarker = (firstByte >> 6) & 0x03;
        if (frameMarker != 2)
        {
            return -1;
        }

        // 提取 profile bits
        // profile_low_bit:  bit 5
        // profile_high_bit: bit 4
        int profileLowBit = (firstByte >> 5) & 0x01;
        int profileHighBit = (firstByte >> 4) & 0x01;

        int profile = (profileHighBit << 1) | profileLowBit;

        // Profile 值范围: 0-3
        if (profile >= 0 && profile <= 3)
        {
            return profile;
        }

        return -1;
    }


    private static bool SkipEBMLSize(ReadOnlySpan<byte> buffer, out int offset)
    {
        offset = 0;
        if (buffer.Length == 0)
        {
            return false;
        }
        byte firstByte = buffer[offset++];
        int numBytes = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((firstByte & (0x80 >> i)) != 0)
            {
                numBytes = i + 1;
                break;
            }
        }
        if (numBytes == 0 || offset + numBytes - 1 > buffer.Length)
        {
            return false;
        }
        offset += numBytes - 1;
        return true;
    }


    private static bool SkipEBMLVInt(ReadOnlySpan<byte> buffer, out int offset)
    {
        offset = 0;
        if (buffer.Length == 0)
        {
            return false;
        }
        byte firstByte = buffer[offset++];
        for (int i = 0; i < 8; i++)
        {
            if ((firstByte & (0x80 >> i)) != 0)
            {
                offset += i;
                return true;
            }
        }
        return false;
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


}
