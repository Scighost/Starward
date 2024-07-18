using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal enum DownloadState
{

    None,

    Queue,

    Download,

    Verify,

    Decompress,

    Finish,

    Error,

}