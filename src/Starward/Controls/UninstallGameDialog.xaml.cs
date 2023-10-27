using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Starward.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class UninstallGameDialog : UserControl
{

    public UninstallStep Steps { get; set; }


    public UninstallStep EnableSteps { get; set; }


    public UninstallGameDialog()
    {
        this.InitializeComponent();
    }





    public bool IsBackupScreenshotEnable => EnableSteps.HasFlag(UninstallStep.BackupScreenshot);

    public bool BackupScreenshot
    {
        get => Steps.HasFlag(UninstallStep.BackupScreenshot);
        set
        {
            if (value)
            {
                Steps |= UninstallStep.BackupScreenshot;
            }
            else
            {
                Steps &= ~UninstallStep.BackupScreenshot;
            }
        }
    }


    public bool IsCleanRegistryEnable => EnableSteps.HasFlag(UninstallStep.CleanRegistry);

    public bool CleanRegistry
    {
        get => Steps.HasFlag(UninstallStep.CleanRegistry);
        set
        {
            if (value)
            {
                Steps |= UninstallStep.CleanRegistry;
            }
            else
            {
                Steps &= ~UninstallStep.CleanRegistry;
            }
        }
    }


    public bool IsDeleteTempFilesEnable => EnableSteps.HasFlag(UninstallStep.DeleteTempFiles);

    public bool DeleteTempFiles
    {
        get => Steps.HasFlag(UninstallStep.DeleteTempFiles);
        set
        {
            if (value)
            {
                Steps |= UninstallStep.DeleteTempFiles;
            }
            else
            {
                Steps &= ~UninstallStep.DeleteTempFiles;
            }
        }
    }


    public bool IsDeleteGameAssetsEnable => EnableSteps.HasFlag(UninstallStep.DeleteGameAssets);

    public bool DeleteGameAssets
    {
        get => Steps.HasFlag(UninstallStep.DeleteGameAssets);
        set
        {
            if (value)
            {
                Steps |= UninstallStep.DeleteGameAssets;
            }
            else
            {
                Steps &= ~UninstallStep.DeleteGameAssets;
            }
        }
    }


}
