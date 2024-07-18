namespace Starward.Services.Download;

internal enum InstallGameState
{

    None,

    Queue,

    Download,

    Verify,

    Decompress,

    Finish,

    Error,

}
