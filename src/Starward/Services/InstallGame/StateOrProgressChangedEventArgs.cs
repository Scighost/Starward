using System;

namespace Starward.Services.InstallGame;

internal class StateOrProgressChangedEventArgs : EventArgs
{

    public InstallGameState State { get; init; }

    public long TotalBytes { get; init; }

    public long ProgressBytes { get; init; }

    public int TotalCount { get; init; }

    public int ProgressCount { get; init; }

    public Exception? Exception { get; init; }

}

