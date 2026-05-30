using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Microsoft.Win32;
using SharpCompress.Common;
using Starward.Setup.Locale;
using Starward.Setup.Services;
using Starward.Setup.Views;
using System.Diagnostics;
using WindowsShortcutFactory;

namespace Starward.Setup.Views;

public class InstallWindow : WindowBase
{


    const int WindowWidth = 660;

    const int WindowHeight = 400;




    private ObservableValue<string> InstallFolder = new(" ");

    private ObservableValue<bool> AgreePrivacyPolicy = new(false);

    private ObservableValue<string> DriveAvailableSizeText = new("-");

    private ObservableValue<string> InstallSizeText = new("-");

    private ObservableValue<double> ProgressBarValue = new(0);

    private ObservableValue<string> InstallProgressText = new(" ");


    private TextBlock TextBlock_TitleHeader;

    private StackPanel StackPanel_InstallInfo;

    private StackPanel StackPanel_InstallProgress;

    private StackPanel StackPanel_PrivacyPolicy;

    private Button Button_Launch;




    private InstallService _installService = new();




    public InstallWindow() : base()
    {
        this.Resizable(WindowWidth, WindowHeight);
        this.Title = Lang.StarwardSetup;
        BuildUI();
    }



    #region UI



    private void BuildUI()
    {
        this.Content = new Grid().Rows("1*,1*").Children(
             new TextBlock().Ref(out TextBlock_TitleHeader)
                            .Row(0)
                            .Top()
                            .Left()
                            .FontSize(13)
                            .Text(Lang.StarwardSetup)
                            .Foreground(Theme.Palette.DisabledAccent)
                            .Margin(12, 8, 0, 0),
             BuildHeader(),
             BuildInstallInfo().Row(1),
             BuildInstallProgress().Row(1).IsVisible(false),
             BuildPravicyPolicy().Row(1),
             new Button().Ref(out Button_Launch)
                         .OnClick(Launch)
                         .FontSize(14)
                         .Padding(20, 10, 20, 10)
                         .Center()
                         .Content(Lang.StarwardLaunch)
                         .BorderThickness(0)
                         .Row(1)
                         .IsVisible(false)
          );
    }


    private StackPanel BuildHeader()
    {
        return new StackPanel().Row(0)
                               .Center()
                               .Horizontal()
                               .Spacing(24)
                               .Children(
            new Image().Center()
                       .Size(120)
                       .ImageScaleQuality(ImageScaleQuality.HighQuality)
                       .SourceResource<InstallWindow>("Starward.Setup.Firefly.png"),
            new StackPanel().CenterVertical()
                            .Spacing(4)
                            .Children(
                new TextBlock().FontWeight(FontWeight.Bold)
                               .FontSize(28)
                               .Foreground(Theme.Palette.Accent)
                               .Text("Starward"),
                new TextBlock().FontSize(14)
                               .Foreground(Theme.Palette.DisabledAccent)
                               .Text(Lang.GameLauncherForMiHoYo)));
    }



    private StackPanel BuildInstallInfo()
    {
        return new StackPanel().Ref(out StackPanel_InstallInfo)
                               .CenterHorizontal()
                               .Top()
                               .Spacing(8)
                               .Children(

            new Border().Margin(12, 0, 12, 0)
                        .CornerRadius(8)
                        .BorderThickness(1)
                        .MinWidth(440)
                        .Height(36)
                        .BorderBrush(Theme.Palette.PlaceholderText)
                        .Child(

                new Grid().Columns("*,Auto").Children(
                    new TextBlock().TextWrapping(TextWrapping.Wrap)
                                   .BindText(InstallFolder)
                                   .FontSize(12)
                                   .Margin(8, 0, 8, 0),
                    new Button().Content(Lang.Change)
                                .BorderThickness(0)
                                .FontSize(13)
                                .OnClick(ChangeInstallFolder))),

            new StackPanel().Margin(12, 0, 12, 0)
                            .Horizontal()
                            .Spacing(4)
                            .Children(
                new TextBlock().CenterVertical().Text(Lang.SpaceRequired).WithTheme((t, c) => c.Foreground = t.Palette.DisabledText),
                new TextBlock().CenterVertical().BindText(InstallSizeText).WithTheme((t, c) => c.Foreground = t.Palette.DisabledText),
                new TextBlock().CenterVertical().Text(Lang.SpaceAvailable).Margin(4, 0, 0, 0).WithTheme((t, c) => c.Foreground = t.Palette.DisabledText),
                new TextBlock().CenterVertical().BindText(DriveAvailableSizeText).WithTheme((t, c) => c.Foreground = t.Palette.DisabledText)),

            new Button().Content(Lang.Install)
                        .BindIsEnabled(AgreePrivacyPolicy)
                        .OnClick(StartInstall)
                        .CenterHorizontal()
                        .BorderThickness(0)
                        .FontSize(15)
                        .Padding(20, 10, 20, 10)
                        .Margin(0, 20, 0, 0));
    }


    private UIElement BuildPravicyPolicy()
    {
        return new StackPanel().Ref(out StackPanel_PrivacyPolicy)
                               .Left()
                               .Bottom()
                               .Horizontal()
                               .Margin(12, 0, 0, 8)
                               .Children(
                new CheckBox().BindIsChecked(AgreePrivacyPolicy)
                              .FontSize(13)
                              .Foreground(Theme.Palette.DisabledText)
                              .Content(Lang.IHaveReadAndAgreeToThePrivacyPolicy),
                new Button().Size(24)
                            .Padding(4)
                            .CenterVertical()
                            .StyleName(BuiltInStyles.FlatButton)
                            .OnClick(OpenPrivacyPolicy)
                            .Content(new PathShape().Stretch(Stretch.Uniform)
                                                    .WithTheme((t, p) => p.Fill(t.Palette.Accent))
                                                    .Data("""
                                F1 M 5.625 15 C 4.85026 15 4.121094 14.851889 3.4375 14.555664 C 2.753906 14.25944 2.158203 13.857422 1.650391 13.349609 C 1.142578 
                                12.841797 0.74056 12.246094 0.444336 11.5625 C 0.148112 10.878906 0 10.14974 0 9.375 C 0 8.600261 0.148112 7.871094 0.444336 7.1875 
                                C 0.74056 6.503906 1.142578 5.908203 1.650391 5.400391 C 2.158203 4.892578 2.753906 4.490561 3.4375 4.194336 C 4.121094 3.898113 4.85026 
                                3.75 5.625 3.75 L 8.125 3.75 C 8.294271 3.75 8.440755 3.81185 8.564453 3.935547 C 8.68815 4.059246 8.75 4.20573 8.75 4.375 C 8.75 4.544271 
                                8.68815 4.690756 8.564453 4.814453 C 8.440755 4.938152 8.294271 5.000001 8.125 5 L 5.625 5 C 5.019531 5.000001 4.451497 5.113934 3.920898 
                                5.341797 C 3.390299 5.569662 2.926432 5.882162 2.529297 6.279297 C 2.132161 6.676434 1.819661 7.140301 1.591797 7.670898 C 1.363932 8.201498 
                                1.25 8.769531 1.25 9.375 C 1.25 9.980469 1.363932 10.548503 1.591797 11.079102 C 1.819661 11.609701 2.132161 12.073568 2.529297 12.470703 
                                C 2.926432 12.867839 3.390299 13.180339 3.920898 13.408203 C 4.451497 13.636068 5.019531 13.75 5.625 13.75 L 8.125 13.75 C 8.294271 13.75 
                                8.440755 13.81185 8.564453 13.935547 C 8.68815 14.059245 8.75 14.205729 8.75 14.375 C 8.75 14.544271 8.68815 14.690756 8.564453 14.814453 
                                C 8.440755 14.938151 8.294271 15 8.125 15 Z M 11.875 15 C 11.705729 15 11.559244 14.938151 11.435547 14.814453 C 11.311849 14.690756 11.25 
                                14.544271 11.25 14.375 C 11.25 14.205729 11.311849 14.059245 11.435547 13.935547 C 11.559244 13.81185 11.705729 13.75 11.875 13.75 L 14.375 
                                13.75 C 14.980469 13.75 15.548502 13.636068 16.079102 13.408203 C 16.609699 13.180339 17.073566 12.867839 17.470703 12.470703 C 17.867838 
                                12.073568 18.180338 11.609701 18.408203 11.079102 C 18.636066 10.548503 18.75 9.980469 18.75 9.375 C 18.75 8.769531 18.636066 8.201498 
                                18.408203 7.670898 C 18.180338 7.140301 17.867838 6.676434 17.470703 6.279297 C 17.073566 5.882162 16.609699 5.569662 16.079102 5.341797 
                                C 15.548502 5.113934 14.980469 5.000001 14.375 5 L 11.875 5 C 11.705729 5.000001 11.559244 4.938152 11.435547 4.814453 C 11.311849 4.690756 
                                11.25 4.544271 11.25 4.375 C 11.25 4.20573 11.311849 4.059246 11.435547 3.935547 C 11.559244 3.81185 11.705729 3.75 11.875 3.75 L 14.375 
                                3.75 C 15.149739 3.75 15.878906 3.898113 16.5625 4.194336 C 17.246094 4.490561 17.841797 4.892578 18.349609 5.400391 C 18.857422 5.908203 
                                19.259439 6.503906 19.555664 7.1875 C 19.851887 7.871094 20 8.600261 20 9.375 C 20 10.14974 19.851887 10.878906 19.555664 11.5625 
                                C 19.259439 12.246094 18.857422 12.841797 18.349609 13.349609 C 17.841797 13.857422 17.246094 14.25944 16.5625 14.555664 C 15.878906 
                                14.851889 15.149739 15 14.375 15 Z M 5.556641 10 C 5.38737 10 5.252278 9.934896 5.151367 9.804688 C 5.050456 9.674479 5 9.53125 5 9.375 
                                C 5 9.21875 5.050456 9.075521 5.151367 8.945312 C 5.252278 8.815104 5.38737 8.75 5.556641 8.75 L 14.443359 8.75 C 14.61263 8.75 14.747721 
                                8.815104 14.848633 8.945312 C 14.949543 9.075521 14.999999 9.21875 15 9.375 C 14.999999 9.53125 14.949543 9.674479 14.848633 9.804688 
                                C 14.747721 9.934896 14.61263 10 14.443359 10 Z
                                """)));
    }


    private UIElement BuildInstallProgress()
    {
        return new StackPanel().Ref(out StackPanel_InstallProgress)
                               .Center()
                               .Spacing(12)
                               .Children(
            new TextBlock().BindText(InstallProgressText)
                           .FontSize(13)
                           .CenterHorizontal()
                           .Foreground(Theme.Palette.DisabledText),
            new ProgressBar().Width(400)
                             .Height(20)
                             .BindValue(ProgressBarValue)
            );
    }




    #endregion



    protected override void OnLoaded()
    {
        base.OnLoaded();
        SetDefaultInstallFolder();
        _ = PrepareManifestAsync();
        if (SilentMode)
        {
            StartInstall();
        }
    }


    private void SetDefaultInstallFolder()
    {
        try
        {
            string? folder = Registry.GetValue(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\Starward", "InstallLocation", null) as string;
            if (!Directory.Exists(folder) || !string.Equals(new DirectoryInfo(folder).Name, "Starward", StringComparison.OrdinalIgnoreCase))
            {
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Starward");
            }
            SetInstallFolder(folder);
        }
        catch
        {
            SetInstallFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Starward"));
        }
    }


    private async Task PrepareManifestAsync(CancellationToken cancellation = default)
    {
        try
        {
            await _installService.PrepareManifestAsync(cancellation);
            InstallSizeText.Value = DriveHelper.GetSizeText(_installService.TotalSize);
            TextBlock_TitleHeader.Text = $"{Lang.StarwardSetup}  ·  {_installService.AppVersion}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private async void ChangeInstallFolder()
    {
        try
        {
            string? folder = FileDialog.SelectFolder(new FolderDialogOptions { Owner = this.Handle });
            if (Directory.Exists(folder))
            {
                if (new DirectoryInfo(folder).Name is not "Starward")
                {
                    folder = Path.Combine(folder, "Starward");
                }
                SetInstallFolder(folder);
                if (DriveHelper.IsDeviceRemovableOrOnUSB(folder))
                {
                    await MessageBox.NotifyAsync(Lang.RemovableDeviceDownloadPortable, PromptIconKind.Info, owner: this);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private void SetInstallFolder(string folder)
    {
        try
        {
            InstallFolder.Value = folder;
            DriveAvailableSizeText.Value = DriveHelper.GetDriveAvailableSpaceText(InstallFolder.Value);
        }
        catch { }
    }



    private async void StartInstall()
    {
        const double MB = 1 << 20;
        try
        {
            if (!string.Equals(new DirectoryInfo(InstallFolder.Value).Name, "Starward", StringComparison.OrdinalIgnoreCase))
            {
                if (SilentMode)
                {
                    Console.Error.WriteLine("Silent install failed: invalid install folder.");
                    Environment.Exit(1);
                }
                return;
            }

            if (!await CheckProcessAsync(InstallFolder.Value))
            {
                return;
            }

            ChangeState(InstallState.Installing);

            await PrepareManifestAsync();
            if (_installService.FullPackage)
            {
                int totalCount = _installService.TotalCount;
                int count = 0;
                string lastPath = "";
                var progress = new Progress<ProgressReport>(p =>
                {
                    if (p.EntryPath != lastPath)
                    {
                        count++;
                        lastPath = p.EntryPath;
                        ProgressBarValue.Value = count * 100d / totalCount;
                        InstallProgressText.Value = $"{count}/{totalCount}";
                    }
                });
                await _installService.ExtractAsync(InstallFolder.Value, progress);
            }
            else
            {
                var task = _installService.StartInstallAsync(InstallFolder.Value);
                while (!task.IsCompleted)
                {
                    ProgressBarValue.Value = (double)_installService.DownloadBytes / _installService.TotalBytes * 100;
                    InstallProgressText.Value = $"{_installService.DownloadBytes / MB:F2}/{_installService.TotalBytes / MB:F2} MB";
                    await Task.Delay(16);
                }
                ProgressBarValue.Value = (double)_installService.DownloadBytes / _installService.TotalBytes * 100;
                InstallProgressText.Value = $"{_installService.DownloadBytes / MB:F2}/{_installService.TotalBytes / MB:F2} MB";
                await task;
            }
            await File.WriteAllTextAsync(Path.Combine(InstallFolder.Value, "StarwardInstallFolder"), InstallFolder.Value);
            CreateShortcut();
            ChangeState(InstallState.Finished);
            if (SilentMode)
            {
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            ChangeState(InstallState.None);
            if (SilentMode)
            {
                Console.Error.WriteLine($"Silent install failed: {ex}");
                Environment.Exit(1);
            }
            else
            {
                await MessageBox.NotifyAsync($"{Lang.AnErrorOccurredDuringInstallation}:\n{ex.Message}", PromptIconKind.Error, owner: this);
            }
        }
    }



    private void ChangeState(InstallState state)
    {
        switch (state)
        {
            case InstallState.None:
                StackPanel_InstallInfo.IsVisible = true;
                StackPanel_InstallProgress.IsVisible = false;
                StackPanel_PrivacyPolicy.IsVisible = true;
                Button_Launch.IsVisible = false;
                break;
            case InstallState.Installing:
                StackPanel_InstallInfo.IsVisible = false;
                StackPanel_InstallProgress.IsVisible = true;
                StackPanel_PrivacyPolicy.IsVisible = false;
                Button_Launch.IsVisible = false;
                break;
            case InstallState.Finished:
                StackPanel_InstallInfo.IsVisible = false;
                StackPanel_InstallProgress.IsVisible = false;
                StackPanel_PrivacyPolicy.IsVisible = false;
                Button_Launch.IsVisible = true;
                break;
            default:
                break;
        }
    }


    private enum InstallState
    {
        None = 0,
        Installing = 1,
        Finished = 2,
    }




    private void Launch()
    {
        try
        {
            string exe = Path.Combine(InstallFolder.Value, "Starward.exe");
            if (File.Exists(exe))
            {
                Process.Start("explorer", $"""
                    "{Path.Combine(InstallFolder.Value, "Starward.exe")}"
                    """);
                Environment.Exit(0);
            }
            else
            {
                StartInstall();
            }
        }
        catch { }
    }




    private void CreateShortcut()
    {
        CreateShortcutInternal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Starward.lnk"));
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Starward");
        Directory.CreateDirectory(folder);
        CreateShortcutInternal(Path.Combine(folder, "Starward.lnk"));
    }


    private void CreateShortcutInternal(string shortcutPath)
    {
        using var shortcut = new WindowsShortcut
        {
            Path = Path.Combine(InstallFolder.Value, "Starward.exe"),
            WorkingDirectory = InstallFolder.Value,
        };
        shortcut.Save(shortcutPath);
    }



    private void OpenPrivacyPolicy()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Scighost/Starward/blob/main/docs/Privacy.md",
            UseShellExecute = true,
        });
    }


}