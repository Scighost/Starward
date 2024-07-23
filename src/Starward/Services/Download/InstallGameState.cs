namespace Starward.Services.Download;

internal enum InstallGameState
{

    None,

    Download,

    Verify,

    Decompress,

    Clean,

    Finish,

    Error,

}
