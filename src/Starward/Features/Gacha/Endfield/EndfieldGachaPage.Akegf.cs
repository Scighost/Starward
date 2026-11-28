using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldGachaPage
{
    private static readonly Uri AkegfSpecification = new("https://www.akedata.wiki/?plugin=akegf&id=home");

    private static string AkegfArchiveFolder => Path.Combine(AppConfig.UserDataFolder!, "akegf");

    public bool IsExportingAkegf { get; private set => SetProperty(ref field, value); }

    [RelayCommand]
    private async Task OpenAkegfSpecificationAsync()
    {
        await Launcher.LaunchUriAsync(AkegfSpecification);
    }

    [RelayCommand]
    private async Task ImportAkegfAsync()
    {
        await OpenAkegfAsync(import: true);
    }

    [RelayCommand]
    private async Task OpenImportedAkegfAsync()
    {
        await OpenAkegfAsync(import: false);
    }

    private async Task OpenAkegfAsync(bool import)
    {
        if (IsSyncing || IsExportingAkegf)
        {
            return;
        }
        try
        {
            string? path = import
                ? await FileDialogHelper.PickSingleFileAsync(XamlRoot, ("AKEGF", ".json"))
                : await PickImportedAkegfAsync();
            if (path is null || UnloadCancellationToken.IsCancellationRequested)
            {
                return;
            }
            CancellationToken cancellation = UnloadCancellationToken;
            StatusSeverity = InfoBarSeverity.Informational;
            StatusText = "正在读取 AKEGF 档案并检查签名。";
            IsStatusOpen = true;
            byte[] bytes = await File.ReadAllBytesAsync(path, cancellation);
            using var stream = new MemoryStream(bytes, writable: false);
            EndfieldAkegfArchive archive = await EndfieldAkegfArchive.ReadAsync(stream, cancellation);
            if (import && archive.IsVerified)
            {
                await SaveVerifiedAkegfAsync(bytes, cancellation);
            }
            cancellation.ThrowIfCancellationRequested();
            IsStatusOpen = false;
            await ShowAkegfArchiveAsync(archive);
        }
        catch (OperationCanceledException)
        {
            IsStatusOpen = false;
        }
        catch (Exception error)
        {
            _logger.LogWarning("AKEGF import failed: {ErrorType}", error.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = error switch
            {
                InvalidDataException => error.Message,
                JsonException => "AKEGF 文件的 JSON 格式无效，或包含重复字段。",
                _ => "无法读取 AKEGF 文件。",
            };
            IsStatusOpen = true;
        }
    }

    private async Task<string?> PickImportedAkegfAsync()
    {
        FileInfo[] archives = Directory.Exists(AkegfArchiveFolder)
            ? new DirectoryInfo(AkegfArchiveFolder).EnumerateFiles("*.json")
                .Where(x => Path.GetFileNameWithoutExtension(x.Name).Length == 64)
                .OrderByDescending(x => x.LastWriteTime).ToArray()
            : [];
        if (archives.Length == 0)
        {
            StatusSeverity = InfoBarSeverity.Informational;
            StatusText = "尚未导入通过验签的 AKEGF 档案。";
            IsStatusOpen = true;
            return null;
        }
        var choices = new ComboBox
        {
            Header = "导入时间",
            ItemsSource = archives.Select(x => $"{x.LastWriteTime:yyyy-MM-dd HH:mm:ss} · {x.Name[..8]}").ToArray(),
            SelectedIndex = 0,
            MinWidth = 320,
        };
        var dialog = new ContentDialog
        {
            Title = "已导入的 AKEGF 档案",
            Content = choices,
            PrimaryButtonText = "查看",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary && choices.SelectedIndex >= 0
            ? archives[choices.SelectedIndex].FullName : null;
    }

    private static async Task SaveVerifiedAkegfAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        string folder = AkegfArchiveFolder;
        string file = Path.Combine(folder, $"{Convert.ToHexStringLower(SHA256.HashData(bytes))}.json");
        if (File.Exists(file))
        {
            return;
        }
        Directory.CreateDirectory(folder);
        string temporary = Path.Combine(folder, $".akegf-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, file, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private async Task ShowAkegfArchiveAsync(EndfieldAkegfArchive archive)
    {
        var roles = new ComboBox
        {
            Header = "角色",
            ItemsSource = archive.Roles,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var summary = new TextBlock();
        var records = new ListView { Height = 300, SelectionMode = ListViewSelectionMode.None };
        roles.SelectionChanged += (_, _) =>
        {
            if (roles.SelectedItem is EndfieldAkegfRole role)
            {
                records.ItemsSource = role.Records;
                int draws = role.Records.Count(x => x.Kind == "draw");
                summary.Text = $"共 {role.Records.Count} 条流水 · {draws} 次抽取 · {role.Records.Count - draws} 条其他事件";
            }
        };
        roles.SelectedIndex = 0;
        string verification = archive.Verification switch
        {
            EndfieldAkegfVerification.Verified => "已验签 · 已保存至本地档案",
            EndfieldAkegfVerification.SignatureVersionMismatch => "验签失败 · 签名对应 v0.4，档案声明为 v1.2",
            EndfieldAkegfVerification.SignatureMismatch => "验签失败 · 签名与档案内容不匹配",
            EndfieldAkegfVerification.KeyUnknown => "验签失败 · 公钥未登记",
            EndfieldAkegfVerification.KeyRevoked => "验签失败 · 公钥已撤销",
            EndfieldAkegfVerification.KeyInvalid => "验签失败 · 公钥资料无效",
            EndfieldAkegfVerification.RegistryUnavailable => "未验证 · 公钥目录暂不可用",
            EndfieldAkegfVerification.Example => "官方示例",
            EndfieldAkegfVerification.LegacySignature => "未验证 · 旧版签名",
            _ => "未验证 · 无签名",
        };
        var content = new StackPanel
        {
            MinWidth = 450,
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = archive.IsVerified ? $"{archive.Version} · {verification}"
                        : $"{archive.Version} · {verification} · 当前会话预览",
                    TextWrapping = TextWrapping.Wrap,
                },
                roles,
                summary,
                records,
                new HyperlinkButton { Content = "AKEGF 格式说明", NavigateUri = AkegfSpecification },
            },
        };
        var dialog = new ContentDialog
        {
            Title = "AKEGF 档案",
            Content = content,
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
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
