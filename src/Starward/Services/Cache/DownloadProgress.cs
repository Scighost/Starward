namespace Starward.Services.Cache;

public readonly struct DownloadProgress
{
    public DownloadProgress(DownloadState downloadState, long bytesReceived, long? totalBytesToReceive)
    {
        DownloadState = downloadState;
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive ?? -1;
    }

    public readonly DownloadState DownloadState { get; init; }

    public readonly long BytesReceived { get; init; }

    public readonly long TotalBytesToReceive { get; init; }
}


public enum DownloadState
{

    Pending,


    Downloading,


    Completed,


    Canceled,
}

