namespace Starward.Services.InstallGame;

internal enum InstallGameState
{

    None,

    Prepare,

    Download,

    Verify,

    Decompress,

    Patch,

    Finish,

    Error,

}
