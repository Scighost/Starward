namespace SharpSevenZip.EventArguments;

public sealed class ExtractProgressEventArgs : EventArgs
{

    public ExtractProgressEventArgs(long totalBytes, long deltaBytes, long finishBytes)
    {
        TotalBytes = totalBytes;
        DeltaBytes = deltaBytes;
        FinishBytes = finishBytes;
        DeltaPercent = totalBytes == 0 ? 0 : ((double)deltaBytes / totalBytes);
        FinishPercent = totalBytes == 0 ? 0 : ((double)finishBytes / totalBytes);
    }

    public long TotalBytes { get; init; }

    public long DeltaBytes { get; init; }

    public long FinishBytes { get; init; }

    /// <summary>
    /// Max is 1
    /// </summary>
    public double DeltaPercent { get; init; }

    /// <summary>
    /// Max is 1
    /// </summary>
    public double FinishPercent { get; init; }

}