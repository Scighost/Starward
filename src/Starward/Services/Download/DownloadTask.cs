using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal class DownloadTask
{



    private int _totalCount;
    public int TotalCount => _totalCount;



    private int _finishCount;
    public int FinishCount => _finishCount;



    private long _totalBytes;
    public long TotalBytes => _totalBytes;



    private long _finishBytes;
    public long FinishBytes => _finishBytes;


    private int concurrentDownloadCount;



    private ConcurrentQueue<DownloadFile> _downloadQueue;



    public void Start()
    {
    }



    public void Stop()
    {

    }





    private async Task DownloadAsync()
    {
        try
        {
            Interlocked.Increment(ref concurrentDownloadCount);

            while (_downloadQueue.TryDequeue(out DownloadFile? file))
            {

            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            Interlocked.Decrement(ref concurrentDownloadCount);
        }
    }




    private async Task DownloadFileAsync()
    {
        const int BUFFER_SIZE = 1 << 16;
        string file = Path.Combine(InstallPath, task.FileName);
        string file_tmp = file + "_tmp";
        string target_file;
        if (File.Exists(file) || noTmp)
        {
            target_file = file;
        }
        else
        {
            target_file = file_tmp;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target_file)!);
        using var fs = File.Open(target_file, FileMode.OpenOrCreate);
        if (fs.Length < task.Size)
        {
            if (string.IsNullOrWhiteSpace(task.Url))
            {
                task.Url = $"{separateUrlPrefix}/{task.FileName.TrimStart('/')}";
            }
            _logger.LogInformation("Download: FileName {name}, Url {url}", task.FileName, task.Url);
            fs.Position = fs.Length;
            var request = new HttpRequestMessage(HttpMethod.Get, task.Url) { Version = HttpVersion.Version11 };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref progressBytes, length);
            }
            _logger.LogInformation("Download Successfully: FileName {name}", task.FileName);
        }
    }



}
