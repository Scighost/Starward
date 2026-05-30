using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Microsoft.Extensions.Configuration;
using Starward.Setup.Locale;
using Starward.Setup.Services;
using System.Diagnostics;

namespace Starward.Setup.Views;

public class UpdateWindow : WindowBase
{

    const int WindowWidth = 540;

    const int WindowHeight = 300;



    private string InstallFolder { get; set; }



    private ObservableValue<string> VersionChanged = new(" ");

    private ObservableValue<string> UpdateProgressText = new(" ");

    private ObservableValue<double> ProgressBarValue = new(0);

    private ObservableValue<string> ErrorText = new(" ");

    private StackPanel StackPanel_UpdateProgress;

    private StackPanel StackPanel_ErrorInfo;

    private ProgressRing ProgressRing_Update;

    private Button Button_Update;

    private Button Button_Launch;


    private readonly UpdateService _updateService = new();



    public UpdateWindow() : base()
    {
        this.Resizable(WindowWidth, WindowHeight);
        this.Title = Lang.StarwardUpdater;
        BuildUI();
    }



    private void BuildUI()
    {
        this.Content = new Grid().Columns("2*,3*").Children(
            new TextBlock().Top()
                           .Left()
                           .FontSize(13)
                           .Text(Lang.StarwardUpdater)
                           .Foreground(Theme.Palette.DisabledAccent)
                           .Margin(12, 8, 0, 0),
            new Image().Column(0)
                       .CenterHorizontal()
                       .CenterVertical()
                       .Size(200)
                       .ImageScaleQuality(ImageScaleQuality.HighQuality)
                       .SourceResource<InstallWindow>("Starward.Setup.Firefly.png"),

            BuildUpdateUI()
          );
    }


    private StackPanel BuildHeader()
    {
        return new StackPanel().CenterVertical()
                               .Spacing(4)
                               .Children(
            new TextBlock().FontWeight(FontWeight.Bold)
                           .FontSize(24)
                           .Foreground(Theme.Palette.Accent)
                           .Text("Starward"),
            new TextBlock().FontSize(13)
                           .Foreground(Theme.Palette.DisabledAccent)
                           .Text(Lang.GameLauncherForMiHoYo),
            new TextBlock().Margin(0, 2, 0, 0)
                           .Foreground(Theme.Palette.DisabledAccent)
                           .BindText(VersionChanged));
    }



    private UIElement BuildUpdateUI()
    {
        return new Grid().Rows("Auto,*,38")
                         .Margin(0, 32, 24, 24)
                         .Children(
            BuildHeader(),

            new StackPanel().Ref(out StackPanel_UpdateProgress)
                            .Row(1)
                            .CenterVertical()
                            .Spacing(12)
                            .IsVisible(false)
                            .Children(
                new TextBlock().BindText(UpdateProgressText)
                               .FontSize(13)
                               .CenterHorizontal()
                               .Foreground(Theme.Palette.DisabledText),
                new ProgressBar().Width(320)
                                 .Height(20)
                                 .CenterHorizontal()
                                 .BindValue(ProgressBarValue)),

            new StackPanel().Ref(out StackPanel_ErrorInfo)
                            .Row(1)
                            .CenterVertical()
                            .Spacing(12)
                            .IsVisible(false)
                            .Children(
                new PromptIcon { Kind = PromptIconKind.Error }.Size(36).Left(),
                new TextBlock().FontSize(14)
                               .TextWrapping(TextWrapping.Wrap)
                               .BindText(ErrorText)),

            new ProgressRing { IsActive = true }.Ref(out ProgressRing_Update)
                                                .Row(1)
                                                .Size(40)
                                                .Center()
                                                .Foreground(Theme.Palette.Accent),

            new Button().Ref(out Button_Launch)
                        .Row(1)
                        .OnClick(Launch)
                        .FontSize(14)
                        .Padding(20, 10, 20, 10)
                        .Center()
                        .Center()
                        .Content(Lang.StarwardLaunch)
                        .BorderThickness(0)
                        .IsVisible(false),


             new Button().Ref(out Button_Update)
                        .Row(2)
                        .OnClick(StartUpdate)
                        .FontSize(14)
                        .Padding(20, 10, 20, 10)
                        .Bottom()
                        .Right()
                        .Content(Lang.Update)
                        .BorderThickness(0)
                        .IsVisible(false)

             );


    }




    protected override void OnLoaded()
    {
        base.OnLoaded();
        StartUpdate();
    }





    private async void StartUpdate()
    {
        try
        {
            StackPanel_UpdateProgress.IsVisible = false;
            ProgressRing_Update.IsVisible = true;
            Button_Update.IsVisible = false;

            var config = new ConfigurationBuilder().AddCommandLine(Environment.GetCommandLineArgs()).Build();
            string? installFolder = config.GetValue<string>("InstallFolder");
            string? oldVersion = config.GetValue<string>("OldVersion");
            string? newVersion = config.GetValue<string>("NewVersion");
            bool preview = config.GetValue<bool>("Preview");
            int pid = config.GetValue<int>("pid");

            if (!Directory.Exists(installFolder))
            {
                ProgressRing_Update.IsVisible = false;
                StackPanel_ErrorInfo.IsVisible = true;
                ErrorText.Value = Lang.TheDestinationFolderDoesNotExistPleaseTryAgain;
                return;
            }
            InstallFolder = installFolder;


            if (string.IsNullOrWhiteSpace(oldVersion))
            {
                string exe = Path.Combine(installFolder, "Starward.exe");
                if (File.Exists(exe))
                {
                    oldVersion = FileVersionInfo.GetVersionInfo(exe).ProductVersion;
                }
            }

            try
            {
                if (pid > 0)
                {
                    Process.GetProcessById(pid).Kill();
                    await Task.Delay(1000);
                }
            }
            catch { }

            if (!await CheckProcessAsync(installFolder))
            {
                ProgressRing_Update.IsVisible = false;
                Button_Update.IsVisible = true;
                return;
            }


            var task = _updateService.UpdateAsync(installFolder, oldVersion, newVersion, preview);

            while (!task.IsCompleted)
            {
                if (string.IsNullOrWhiteSpace(_updateService.OldVersion) && string.IsNullOrWhiteSpace(_updateService.NewVersion))
                {
                    VersionChanged.Value = " ";
                }
                else
                {
                    VersionChanged.Value = $"{(_updateService.OldVersion ?? "·")}  →  {(_updateService.NewVersion ?? "·")}";
                }
                if (_updateService.TotalBytes > 0)
                {
                    const double MB = 1 << 20;
                    StackPanel_UpdateProgress.IsVisible = true;
                    ProgressRing_Update.IsVisible = false;
                    ProgressBarValue.Value = (double)_updateService.DownloadBytes / _updateService.TotalBytes * 100;
                    UpdateProgressText.Value = $"{_updateService.DownloadBytes / MB:F2}/{_updateService.TotalBytes / MB:F2} MB";
                }
                else
                {
                    StackPanel_UpdateProgress.IsVisible = false;
                    ProgressRing_Update.IsVisible = true;
                }
                await Task.Delay(16);
            }

            await task;

            StackPanel_UpdateProgress.IsVisible = false;
            ProgressRing_Update.IsVisible = false;
            Button_Launch.IsVisible = true;
        }
        catch (Exception ex)
        {
            StackPanel_UpdateProgress.IsVisible = false;
            ProgressRing_Update.IsVisible = false;
            Button_Update.IsVisible = true;
            await MessageBox.NotifyAsync($"{Lang.AnErrorOccurredDuringTheUpdate}:\n{ex.Message}");
        }
    }



    private void Launch()
    {
        try
        {
            string exe = Path.Combine(InstallFolder, "Starward.exe");
            if (File.Exists(exe))
            {
                Process.Start("explorer", $"""
                    "{Path.Combine(InstallFolder, "Starward.exe")}"
                    """);
                Environment.Exit(0);
            }
            else
            {
                StartUpdate();
            }
        }
        catch { }
    }




}
