using System;

namespace Starward.Models;

[Flags]
public enum UninstallStep
{

    None = 0,

    BackupScreenshot = 1,

    CleanRegistry = 2,

    DeleteTempFiles = 4,

    DeleteGameAssets = 8,

}
