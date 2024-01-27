using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class GenshinInstallGameService : InstallGameService
{


    public override GameBiz CurrentGame => GameBiz.GenshinImpact;


    public GenshinInstallGameService(ILogger<GenshinInstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
        : base(logger, gameResourceService, launcherClient, httpClient)
    {

    }




    protected override async Task PrepareForRepairAsync()
    {
        await base.PrepareForRepairAsync();

        var list = new List<DownloadFileTask>();
        if (VoiceLanguages.HasFlag(VoiceLanguage.Chinese))
        {
            list.AddRange(await GetPkgVersionsAsync($"{separateUrlPrefix}/Audio_Chinese_pkg_version").ConfigureAwait(false));
        }
        if (VoiceLanguages.HasFlag(VoiceLanguage.English))
        {
            list.AddRange(await GetPkgVersionsAsync($"{separateUrlPrefix}/Audio_English(US)_pkg_version").ConfigureAwait(false));
        }
        if (VoiceLanguages.HasFlag(VoiceLanguage.Japanese))
        {
            list.AddRange(await GetPkgVersionsAsync($"{separateUrlPrefix}/Audio_Japanese_pkg_version").ConfigureAwait(false));
        }
        if (VoiceLanguages.HasFlag(VoiceLanguage.Korean))
        {
            list.AddRange(await GetPkgVersionsAsync($"{separateUrlPrefix}/Audio_Korean_pkg_version").ConfigureAwait(false));
        }

        separateResources.AddRange(list);

        string? exe_cn = Path.Join(InstallPath, "YuanShen.exe");
        string? exe_os = Path.Join(InstallPath, "GenshinImpact.exe");
        string? data_cn = Path.Join(InstallPath, "YuanShen_Data");
        string? data_os = Path.Join(InstallPath, "GenshinImpact_Data");
        if (CurrentGameBiz is GameBiz.hk4e_cn)
        {
            if (File.Exists(exe_os))
            {
                File.Delete(exe_os);
            }
            if (!Directory.Exists(data_cn) && Directory.Exists(data_os))
            {
                Directory.Move(data_os, data_cn);
            }
        }
        if (CurrentGameBiz is GameBiz.hk4e_global)
        {
            if (File.Exists(exe_cn))
            {
                File.Delete(exe_cn);
            }
            if (!Directory.Exists(data_os) && Directory.Exists(data_cn))
            {
                Directory.Move(data_cn, data_os);
            }
        }
        await _gameResourceService.SetVoiceLanguageAsync(CurrentGameBiz, InstallPath, VoiceLanguages).ConfigureAwait(false);
        await MoveAudioAssetsFromPersistentToStreamAssetsAsync().ConfigureAwait(false);
    }



    /// <summary>
    /// 原神专用，将 Persistent\AudioAssets 目录下的文件移动到 StreamingAssets\AudioAssets 目录下
    /// </summary>
    /// <returns></returns>
    protected async Task MoveAudioAssetsFromPersistentToStreamAssetsAsync()
    {
        await Task.Run(() =>
        {
            string dataName = CurrentGameBiz switch
            {
                GameBiz.hk4e_cn => "YuanShen_Data",
                GameBiz.hk4e_global => "GenshinImpact_Data",
                _ => "",
            };
            if (!string.IsNullOrWhiteSpace(dataName))
            {
                var source = Path.Combine(InstallPath, $@"{dataName}\Persistent\AudioAssets");
                var target = Path.Combine(InstallPath, $@"{dataName}\StreamingAssets\AudioAssets");
                if (Directory.Exists(source))
                {
                    var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                    _logger.LogInformation("Move audio assets: {count} files.", files.Length);
                    foreach (var file in files)
                    {
                        var relative = Path.GetRelativePath(source, file);
                        var dest = Path.Combine(target, relative);
                        if (File.Exists(dest))
                        {
                            File.SetAttributes(dest, FileAttributes.Archive);
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        File.Move(file, dest, true);
                        if (File.Exists(dest))
                        {
                            File.SetAttributes(dest, FileAttributes.Archive);
                        }
                    }
                }
            }
        }).ConfigureAwait(false);
    }



    protected override async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        await base.VerifySeparateFilesAsync(cancellationToken).ConfigureAwait(false);

        // 清理不在资源列表中的文件
        string assetsFolder = CurrentGameBiz switch
        {
            GameBiz.hk4e_cn => Path.Combine(InstallPath, @"YuanShen_Data\StreamingAssets"),
            GameBiz.hk4e_global => Path.Combine(InstallPath, @"GenshinImpact_Data\StreamingAssets"),
            _ => "",
        };
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
                if (file.EndsWith("_tmp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
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
