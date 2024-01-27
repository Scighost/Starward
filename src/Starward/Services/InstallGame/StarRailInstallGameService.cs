using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class StarRailInstallGameService : InstallGameService
{


    public override GameBiz CurrentGame => GameBiz.StarRail;


    public StarRailInstallGameService(ILogger<StarRailInstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
        : base(logger, gameResourceService, launcherClient, httpClient)
    {

    }





    protected override async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        await base.VerifySeparateFilesAsync(cancellationToken).ConfigureAwait(false);

        // 删除不在资源列表中的文件
        string assetsFolder = @"StarRail_Data\StreamingAssets";
        List<string>? existFiles = null;
        if (Directory.Exists(assetsFolder))
        {
            existFiles = Directory.GetFiles(assetsFolder, "*", SearchOption.AllDirectories).ToList();
        }
        var packageFiles = separateResources.Select(x => Path.GetFullPath(Path.Combine(InstallPath, x.FileName))).ToList();
        if (existFiles != null)
        {
            var deleteFiles = existFiles.Except(packageFiles).ToList();
            foreach (var file in deleteFiles)
            {
                if (File.Exists(file))
                {
                    _logger.LogInformation("Delete deprecated file: {file}", file);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
        }

        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressBytes = 0;
        TotalCount = downloadTasks.Count;
        progressCount = 0;
    }




}
