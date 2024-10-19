using ICSharpCode.SharpZipLib.Zip;
using Starward.Core.ZipStreamDownload.Exceptions;
using Starward.Core.ZipStreamDownload.Extensions;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP格式帮助类
/// </summary>
internal static class ZipFormat
{
    /// <summary>
    /// Reverse locates a block with the desired <paramref name="signature"/>.
    /// </summary>
    /// <param name="stream" />
    /// <param name="signature">The signature to find.</param>
    /// <param name="endLocation">Location, marking the end of block.</param>
    /// <param name="minimumBlockSize">Minimum size of the block.</param>
    /// <param name="maximumVariableData">The maximum variable data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static Task<long> _LocateBlockWithSignatureAsync(Stream stream, int signature,
        long endLocation, int minimumBlockSize, int maximumVariableData,
        CancellationToken cancellationToken = default)
    {
        var pos = endLocation - minimumBlockSize;
        if (pos < 0) return Task.FromResult<long>(-1);
        var giveUpMarker = Math.Max(pos - maximumVariableData, 0);
        // TODO: This loop could be optimized for speed.
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pos < giveUpMarker) return Task.FromResult<long>(-1);
            stream.Seek(pos--, SeekOrigin.Begin);
        } while (stream.ReadInt() != signature);
        return Task.FromResult(stream.Position);
    }

    /// <summary>
    /// Reverse locates a block with the desired <paramref name="signature"/> (Async).
    /// </summary>
    /// <param name="stream" />
    /// <param name="signature">The signature to find.</param>
    /// <param name="startLocation">Location, marking the end of block.</param>
    /// <param name="minimumBlockSize">Minimum size of the block.</param>
    /// <param name="maximumVariableData">The maximum variable data.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    private static Task<long> _LocateBlockWithSignatureReverseAsync(Stream stream, int signature,
        long startLocation, int minimumBlockSize, int maximumVariableData,
        CancellationToken cancellationToken = default)
    {
        var pos = startLocation + minimumBlockSize;
        if (pos > stream.Length) return Task.FromResult<long>(-1);
        var giveUpMarker = Math.Min(pos + maximumVariableData, stream.Length);
        // TODO: This loop could be optimized for speed.
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pos > giveUpMarker) return Task.FromResult<long>(-1);
            stream.Seek(pos++, SeekOrigin.Begin);
        } while (stream.ReadInt(reverse: true) != signature);
        return Task.FromResult(stream.Position);
    }

    /// <summary>
    /// Locates a block with the desired <paramref name="signature"/>.
    /// </summary>
    /// <param name="stream" />
    /// <param name="signature">The signature to find.</param>
    /// <param name="startLocation">Location, marking the end of block.</param>
    /// <param name="minimumBlockSize">Minimum size of the block.</param>
    /// <param name="maximumVariableData">The maximum variable data.</param>
    /// <param name="reverse">Reverse lookup</param>
    /// <returns>Returns the offset of the first byte after the signature; -1 if not found</returns>
    public static long LocateBlockWithSignature(Stream stream, int signature,
        long startLocation, int minimumBlockSize, int maximumVariableData, bool reverse = false)
        => reverse
            ? _LocateBlockWithSignatureReverseAsync(stream, signature, startLocation,
                minimumBlockSize, maximumVariableData).GetAwaiter().GetResult()
            : _LocateBlockWithSignatureAsync(stream, signature, startLocation,
                minimumBlockSize, maximumVariableData).GetAwaiter().GetResult();

    /// <summary>
    /// Locates a block with the desired <paramref name="signature"/> (Async).
    /// </summary>
    /// <param name="stream" />
    /// <param name="signature">The signature to find.</param>
    /// <param name="startLocation">Location, marking the end of block.</param>
    /// <param name="minimumBlockSize">Minimum size of the block.</param>
    /// <param name="maximumVariableData">The maximum variable data.</param>
    /// <param name="reverse">Reverse lookup</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Returns the offset of the first byte after the signature; -1 if not found</returns>
    public static Task<long> LocateBlockWithSignatureAsync(Stream stream, int signature,
        long startLocation, int minimumBlockSize, int maximumVariableData, bool reverse = false,
        CancellationToken cancellationToken = default)
        => reverse
            ? _LocateBlockWithSignatureReverseAsync(stream, signature, startLocation, minimumBlockSize,
                maximumVariableData, cancellationToken)
            : _LocateBlockWithSignatureAsync(stream, signature, startLocation, minimumBlockSize, maximumVariableData,
                cancellationToken);

    /// <summary>
    /// Write the required records to end the central directory.
    /// </summary>
    /// <param name="stream" />
    /// <param name="noOfEntries">The number of entries in the directory.</param>
    /// <param name="sizeEntries">The size of the entries in the directory.</param>
    /// <param name="start">The start of the central directory.</param>
    /// <param name="comment">The archive comment.  (This can be null).</param>
    internal static void WriteEndOfCentralDirectory(Stream stream,
        long noOfEntries, long sizeEntries, long start, byte[]? comment)
    {
        if (noOfEntries >= 0xffff ||
            start >= 0xffffffff ||
            sizeEntries >= 0xffffffff)
            WriteZip64EndOfCentralDirectory(stream, noOfEntries, sizeEntries, start);
        stream.WriteNumber(ZipConstants.EndOfCentralDirectorySignature);
        // TODO: ZipFile Multi disk handling not done
        stream.WriteNumber((ushort)0);                    // number of this disk
        stream.WriteNumber((ushort)0);                    // no of disk with start of central dir
        // Number of entries
        if (noOfEntries >= 0xffff)
        {
            stream.WriteNumber((ushort)0xffff);  // Zip64 marker
            stream.WriteNumber((ushort)0xffff);
        }
        else
        {
            stream.WriteNumber((ushort)noOfEntries);          // entries in central dir for this disk
            stream.WriteNumber((ushort)noOfEntries);          // total entries in central directory
        }

        // Size of the central directory
        if (sizeEntries >= 0xffffffff) stream.WriteNumber(0xffffffffU);    // Zip64 marker
        else stream.WriteNumber((uint)sizeEntries);

        // offset of start of central directory
        if (start >= 0xffffffff) stream.WriteNumber(0xffffffffU);    // Zip64 marker
        else stream.WriteNumber((uint)start);
        var commentLength = comment?.Length ?? 0;
        if (commentLength > 0xffff) ZipFileTestFailedException
            .ThrowByReasonCentralDirectory($"Comment length ({commentLength}) is larger than 64K");
        stream.WriteNumber((ushort)commentLength);
        if (commentLength > 0) stream.WriteLittleEndianBytes(comment);
    }

    /// <summary>
    /// Write Zip64 end of central directory records (File header and locator).
    /// </summary>
    /// <param name="stream" />
    /// <param name="noOfEntries">The number of entries in the central directory.</param>
    /// <param name="sizeEntries">The size of entries in the central directory.</param>
    /// <param name="centralDirOffset">The offset of the central directory.</param>
    private static void WriteZip64EndOfCentralDirectory(Stream stream,
        long noOfEntries, long sizeEntries, long centralDirOffset)
    {
        var centralSignatureOffset = centralDirOffset + sizeEntries;
        stream.WriteNumber((uint)ZipConstants.Zip64CentralFileHeaderSignature);
        stream.WriteNumber(44UL);    // Size of this record (total size of remaining fields in header or full size - 12)
        stream.WriteNumber((ushort)ZipConstants.VersionMadeBy);   // Version made by
        stream.WriteNumber((ushort)ZipConstants.VersionZip64);   // Version to extract
        stream.WriteNumber(0U);      // Number of this disk
        stream.WriteNumber(0U);      // number of the disk with the start of the central directory
        stream.WriteNumber(noOfEntries);       // No of entries on this disk
        stream.WriteNumber(noOfEntries);       // Total No of entries in central directory
        stream.WriteNumber(sizeEntries);       // Size of the central directory
        stream.WriteNumber(centralDirOffset);  // offset of start of central directory

        // zip64 extensible data sector not catered for here (variable size)
        // Write the Zip64 end of central directory locator
        stream.WriteNumber((uint)ZipConstants.Zip64CentralDirLocatorSignature);
        // no of the disk with the start of the zip64 end of central directory
        stream.WriteNumber(0U);
        // relative offset of the zip64 end of central directory record
        stream.WriteNumber((ulong)centralSignatureOffset);
        // total number of disks
        stream.WriteNumber(1U);
    }
}