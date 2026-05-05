using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Starward.Setup.Locale;
using Starward.Setup.Services;
using System.Diagnostics;

namespace Starward.Setup.Views;

public class UninstallWindow : WindowBase
{

    const int WindowWidth = 540;

    const int WindowHeight = 300;



    private ObservableValue<bool> UninstallEnabled = new(false);


    private StackPanel StackPanel_UninstallInfo;

    private ProgressRing ProgressRing_CheckUninstall;

    private ProgressRing ProgressRing_Uninstalling;

    private Button Button_Uninstall;

    private Button Button_UninstallFinished;

    private CheckBox CheckBox_Caution;

    private CheckBox CheckBox_CacheData;



    public UninstallWindow() : base()
    {
        this.Resizable(WindowWidth, WindowHeight);
        this.Title = Lang.StarwardUninstaller;
        BuildUI();
    }



    private void BuildUI()
    {
        this.Content = new Grid().Columns("2*,3*").Children(
            new TextBlock().Top()
                           .Left()
                           .FontSize(13)
                           .Text(Lang.StarwardUninstaller)
                           .Foreground(Theme.Palette.DisabledAccent)
                           .Margin(12, 8, 0, 0),
            new Image().Column(0)
                       .CenterHorizontal()
                       .CenterVertical()
                       .Size(200)
                       .ImageScaleQuality(ImageScaleQuality.HighQuality)
                       .SourceResource<InstallWindow>("Starward.Setup.Firefly.png"),

            BuildUninstallUI()
          );
    }


    private StackPanel BuildHeader()
    {
        return new StackPanel().CenterVertical()
                               .Spacing(2)
                               .Children(
            new TextBlock().FontWeight(FontWeight.Bold)
                           .FontSize(24)
                           .Foreground(Theme.Palette.Accent)
                           .Text("Starward"),
            new TextBlock().FontSize(13)
                           .Foreground(Theme.Palette.DisabledAccent)
                           .Text(Lang.GameLauncherForMiHoYo));
    }



    private UIElement BuildUninstallUI()
    {
        return new Grid().Rows("Auto,*,Auto")
                         .Margin(0, 32, 24, 24)
                         .Children(
            BuildHeader(),

            new StackPanel().Ref(out StackPanel_UninstallInfo)
                            .Row(1)
                            .CenterVertical()
                            .Spacing(4),

            new ProgressRing { IsActive = true }.Ref(out ProgressRing_CheckUninstall)
                                                .Row(1)
                                                .Size(40)
                                                .Center()
                                                .Foreground(Theme.Palette.Accent),

            new Button().Ref(out Button_UninstallFinished)
                      .Row(1)
                      .OnClick(() => Environment.Exit(0))
                      .FontSize(14)
                      .Padding(20, 10, 20, 10)
                      .Center()
                      .Center()
                      .Content(Lang.UninstallationComplete)
                      .BorderThickness(0)
                      .IsVisible(false),

            new CheckBox().Ref(out CheckBox_Caution)
                          .Row(2)
                          .Bottom()
                          .Left()
                          .Margin(0, 0, 0, 20)
                          .IsVisible(false)
                          .BindIsChecked(UninstallEnabled)
                          .Content(Lang.IAcknowledgeTheAboveRisks),

            new CheckBox().Ref(out CheckBox_CacheData)
                          .Row(2)
                          .Bottom()
                          .Left()
                          .Content(Lang.KeepCachedData),

            new Button().Ref(out Button_Uninstall)
                        .Row(2)
                        .OnClick(Uninstall)
                        .FontSize(14)
                        .Padding(20, 10, 20, 10)
                        .Bottom()
                        .Right()
                        .Content(Lang.Uninstall)
                        .BorderThickness(0)
                        .BindIsEnabled(UninstallEnabled),

            new ProgressRing { IsActive = true }.Ref(out ProgressRing_Uninstalling)
                                                .Row(2)
                                                .Size(38)
                                                .Bottom()
                                                .Right()
                                                .IsVisible(false)
                                                .Foreground(Theme.Palette.Accent)

             );


    }




    protected override void OnLoaded()
    {
        base.OnLoaded();
        CheckUninstall();
    }



    private async void CheckUninstall()
    {
        try
        {
            StackPanel_UninstallInfo.Clear();
            bool caution = false;
            List<string> textList = new();

            string? currentFolder = Path.GetDirectoryName(Environment.ProcessPath);

            string flag = Path.Combine(currentFolder!, "StarwardInstallFolder");
            bool canUninstall = false;
            if (File.Exists(flag))
            {
                string content = await File.ReadAllTextAsync(flag);
                if (string.Equals(currentFolder, content.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    canUninstall = true;
                }
            }

            if (!canUninstall)
            {
                StackPanel_UninstallInfo.Add(new TextBlock().FontSize(14).Text(Lang.UninstallPathCaution).TextWrapping(TextWrapping.Wrap));
                return;
            }

            string[] files = Directory.GetFiles(currentFolder!, "*.db", SearchOption.AllDirectories);
            bool hasDatabase = files.Any(f => Path.GetFileName(f).Equals("StarwardDatabase.db", StringComparison.OrdinalIgnoreCase));
            if (hasDatabase)
            {
                caution = true;
                textList.Add($"● {Lang.UnstallDatabaseCaution}");
            }

            string[] screenshots = Directory.GetDirectories(currentFolder!, "ScreenShot*", SearchOption.AllDirectories);
            foreach (string screenshot in screenshots)
            {
                if (Directory.GetFiles(screenshot, "*", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    caution = true;
                    textList.Add($"● {Lang.UnstallScreenshotCaution}");
                    break;
                }
            }

            await Task.Delay(1000);

            if (caution)
            {
                CheckBox_Caution.IsVisible = true;
                foreach (string text in textList)
                {
                    StackPanel_UninstallInfo.Add(new TextBlock().Text(text).TextWrapping(TextWrapping.Wrap));
                }
            }
            else
            {
                UninstallEnabled.Value = true;
                StackPanel_UninstallInfo.Add(new TextBlock().FontSize(14).Text(Lang.AllFilesCanBeSafelyDeleted).TextWrapping(TextWrapping.Wrap));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            UninstallEnabled.Value = true;
        }
        finally
        {
            ProgressRing_CheckUninstall.IsVisible = false;
        }
    }




    private async void Uninstall()
    {
        try
        {
            if (!await CheckProcessAsync())
            {
                return;
            }

            ProgressRing_Uninstalling.IsVisible = true;
            Button_Uninstall.IsVisible = false;
            CheckBox_Caution.IsEnabled = false;

            await Task.Delay(1000);

            await Task.Run(() =>
            {
                string? currentFolder = Path.GetDirectoryName(Environment.ProcessPath);
                string[] files = Directory.GetFiles(currentFolder!, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (!string.Equals(file, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }

                if (CheckBox_CacheData.IsChecked is null or false)
                {
                    string cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
                    if (Directory.Exists(cacheFolder))
                    {
                        Directory.Delete(cacheFolder, true);
                    }
                }

                RegistryHelper.DeleteUninstallInfo();
                RegistryHelper.DeleteUrlProtocol();
                RegistryHelper.DeleteRegistrySetting();

                ClearShortcut();
                StartClearProcess();
            });
            await Task.Delay(1000);

            StackPanel_UninstallInfo.IsVisible = false;
            ProgressRing_Uninstalling.IsVisible = false;
            CheckBox_Caution.IsVisible = false;
            CheckBox_CacheData.IsVisible = false;
            Button_UninstallFinished.IsVisible = true;
        }
        catch (Exception ex)
        {
            ProgressRing_Uninstalling.IsVisible = false;
            Button_Uninstall.IsVisible = true;
            CheckBox_Caution.IsEnabled = true;
            await MessageBox.NotifyAsync($"{Lang.AnErrorOccurredDuringUninstallation}:\n{ex.Message}", PromptIconKind.Error, owner: this);
        }
    }



    private static void ClearShortcut()
    {
        string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Starward.lnk");
        if (File.Exists(lnk))
        {
            File.Delete(lnk);
        }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Starward");
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
    }



    private static void StartClearProcess()
    {
        string exe = Path.GetTempFileName() + ".exe";
        File.Copy(Environment.ProcessPath!, exe, true);
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"""
            uninstall-clear {Environment.ProcessId} "{Path.GetDirectoryName(Environment.ProcessPath)}"
            """,
            CreateNoWindow = true,
        });
    }








}
