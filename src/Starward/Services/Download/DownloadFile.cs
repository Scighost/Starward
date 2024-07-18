using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal class DownloadFile
{

    public string FileName { get; set; }


    public string Url { get; set; }


    public string Path { get; set; }


    public bool IsFolder { get; set; }


    public long Size { get; set; }


    public long DecompressedSize { get; set; }


    public string MD5 { get; set; }


    public Range Range { get; set; }


    public bool WriteAsTempFile { get; set; }


}
