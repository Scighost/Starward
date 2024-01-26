using System;
using System.Collections.Generic;

namespace Starward.Services.InstallGame;

internal class CheckSumFailedException : Exception
{


    public List<string> Files { get; init; }


    public CheckSumFailedException(string message, List<string> files) : base(message)
    {
        Files = files;
    }

}