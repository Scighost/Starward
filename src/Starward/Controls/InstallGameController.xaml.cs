using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Starward.Services.Download;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class InstallGameController : UserControl
{


    private readonly ILogger<InstallGameController> _logger = AppConfig.GetLogger<InstallGameController>();


    private readonly InstallGameManager _installGameManager = AppConfig.GetService<InstallGameManager>();


    public InstallGameController()
    {
        this.InitializeComponent();
    }





    private ObservableCollection<InstallGameStateModel> _installServices = new();










}
