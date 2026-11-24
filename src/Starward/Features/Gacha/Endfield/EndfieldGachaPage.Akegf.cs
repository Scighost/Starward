using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldGachaPage
{
    public bool IsExportingAkegf { get; private set => SetProperty(ref field, value); }

    [RelayCommand]
    private async Task OpenAkegfSpecificationAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://www.akedata.wiki/?plugin=akegf&id=standard"));
    }

    [RelayCommand]
    private async Task ExportAkegfAsync()
    {
        if (IsSyncing || IsExportingAkegf || SelectedAccount is not { } account)
        {
            return;
        }
        IsExportingAkegf = true;
        try
        {
            string name = $"Starward_AKEGF_{DateTime.Now:yyyyMMddHHmmss}.json";
            string? path = await FileDialogHelper.OpenSaveFileDialogAsync(XamlRoot, name, ("AKEGF", ".json"));
            if (path is null)
            {
                return;
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            byte[] bytes = await _service.ExportAkegfAsync(account, timeout.Token);
            string temporary = Path.Combine(Path.GetDirectoryName(path)!, $".akegf-{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllBytesAsync(temporary, bytes, timeout.Token);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                File.Delete(temporary);
            }
            StatusSeverity = InfoBarSeverity.Success;
            StatusText = "已导出未签名的 AKEGF v0.4。";
            IsStatusOpen = true;
        }
        catch (Exception error)
        {
            _logger.LogWarning("AKEGF export failed: {ErrorType}", error.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = error switch
            {
                OperationCanceledException => "AKEGF 导出超时，请重试。",
                InvalidOperationException => error.Message,
                _ => "AKEGF 导出失败，请检查本地数据和保存位置后重试。",
            };
            IsStatusOpen = true;
        }
        finally
        {
            IsExportingAkegf = false;
        }
    }
}
