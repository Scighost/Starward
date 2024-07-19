namespace Starward.Services.Download;

internal enum InstallGameState
{

    None,

    Prepared,

    Queue,

    Download,

    Verify,

    Decompress,

    Finish,

    Error,

}
