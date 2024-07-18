using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal class InstallGameService
{


    private readonly ILogger<InstallGameService> _logger;


    private readonly HttpClient _httpClient;









    private ConcurrentQueue<InstallGameItem> _installItemQueue;




    private int _totalCount;
    public int TotalCount => _totalCount;



    private int _finishCount;
    public int FinishCount => _finishCount;



    private long _totalBytes;
    public long TotalBytes => _totalBytes;



    private long _finishBytes;
    public long FinishBytes => _finishBytes;



    private int _concurrentExecuteThreadCount;
    public int ConcurrentExecuteThreadCount => _concurrentExecuteThreadCount;







    private async Task ExecuteTaskAsync()
    {
        try
        {
            Interlocked.Increment(ref _concurrentExecuteThreadCount);

            while (_installItemQueue.TryDequeue(out InstallGameItem? item))
            {
                try
                {
                    switch (item.Type)
                    {
                        case InstallGameItemType.None:
                            break;
                        case InstallGameItemType.Download:
                            await DownloadItemAsync(item);
                            break;
                        case InstallGameItemType.Verify:
                            break;
                        case InstallGameItemType.Decompress:
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {

                }

            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            Interlocked.Decrement(ref _concurrentExecuteThreadCount);
        }
    }




    private async Task DownloadItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 16;
        string file = item.Path;
        string file_tmp = file + "_tmp";
        string target_file;
        target_file = item.WriteAsTempFile ? file_tmp : file;
        Directory.CreateDirectory(Path.GetDirectoryName(target_file)!);
        using var fs = File.Open(target_file, FileMode.OpenOrCreate);
        if (fs.Length < item.Size)
        {
            _logger.LogInformation("Download: FileName {name}, Url {url}", item.FileName, item.Url);
            fs.Position = fs.Length;
            var request = new HttpRequestMessage(HttpMethod.Get, item.Url) { Version = HttpVersion.Version11 };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref _finishBytes, length);
            }
            _logger.LogInformation("Download Successfully: FileName {name}", item.FileName);
        }
    }









}
