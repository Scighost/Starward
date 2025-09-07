using System;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.WinUI;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vanara.PInvoke;

namespace Starward.Controls;

public sealed partial class TaskbarIcon : UserControl, IDisposable
{
    private JsonIpcDirectRoutedProvider? _ipcProvider;
    private H.NotifyIcon.TaskbarIcon? _taskbarIcon;

    private bool Created { get; set; }

    public void Create(string? pipeName)
    {
        EnsureNotDisposed();
        if (Created)
        {
            throw new InvalidOperationException("TaskbarIcon already created");
        }
        
        if (!string.IsNullOrEmpty(pipeName))
        {
            _ipcProvider ??= new JsonIpcDirectRoutedProvider(
                pipeName,
                new IpcConfiguration().UseSystemTextJsonIpcObjectSerializer(SourceGenerationContext.Default)
            );
            SetupHandler(_ipcProvider);
            _ipcProvider.StartServer();
            Created = true;
            return;
        }

        _taskbarIcon ??= new H.NotifyIcon.TaskbarIcon()
        {
            Icon = new Icon(IconPath),
            NoLeftClickDelay = true,
            ToolTipText = "Starward",
            LeftClickCommand = LeftClickCommand,
            RightClickCommand = RightClickCommand,
        };

        Content = _taskbarIcon;
        Created = true;
    }

    private void SetupHandler(JsonIpcDirectRoutedProvider ipcProvider)
    {
        ipcProvider.AddRequestHandler("GetIconPathAsync", () => DispatcherQueue.EnqueueAsync(
            () => new GetIconPathResponse(IconPath)
        ));
        ipcProvider.AddNotifyHandler("PingAsync", () => Task.CompletedTask);
        ipcProvider.AddNotifyHandler(nameof(OnLeftClickAsync), OnLeftClickAsync);
        ipcProvider.AddNotifyHandler(nameof(OnRightClickAsync), OnRightClickAsync);
    }

    private Task OnLeftClickAsync()
    {
        return DispatcherQueue.EnqueueAsync(() =>
        {
            User32.mouse_event(0, 0, 0, 0, 0);
            if (LeftClickCommand?.CanExecute(null) is true) LeftClickCommand.Execute(null);
        });
    }

    private Task OnRightClickAsync()
    {
        return DispatcherQueue.EnqueueAsync(() =>
        {
            User32.mouse_event(0, 0, 0, 0, 0);
            if (RightClickCommand?.CanExecute(null) is true) RightClickCommand.Execute(null);
        });
    }

    public string IconPath
    {
        get => (string)GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public static readonly DependencyProperty IconPathProperty =
        DependencyProperty.Register(
            nameof(IconPath),
            typeof(string),
            typeof(TaskbarIcon),
            PropertyMetadata.Create(string.Empty)
        );

    public ICommand? LeftClickCommand
    {
        get => (ICommand?)GetValue(LeftClickCommandProperty);
        set => SetValue(LeftClickCommandProperty, value);
    }

    public static readonly DependencyProperty LeftClickCommandProperty =
        DependencyProperty.Register(
            nameof(LeftClickCommand),
            typeof(ICommand),
            typeof(TaskbarIcon),
            PropertyMetadata.Create(defaultValue: null, (sender, args) =>
            {
                ((TaskbarIcon)sender).OnLeftClickCommandChanged(args.NewValue as ICommand);
            })
        );

    private void OnLeftClickCommandChanged(ICommand? command)
    {
        _taskbarIcon?.LeftClickCommand = command;
    }

    public ICommand? RightClickCommand
    {
        get => (ICommand?)GetValue(RightClickCommandProperty);
        set => SetValue(RightClickCommandProperty, value);
    }

    public static readonly DependencyProperty RightClickCommandProperty =
        DependencyProperty.Register(
            nameof(RightClickCommand),
            typeof(ICommand),
            typeof(TaskbarIcon),
            PropertyMetadata.Create(defaultValue: null, (sender, args) =>
            {
                ((TaskbarIcon)sender).OnRightClickCommandChanged(args.NewValue as ICommand);
            })
        );

    private void OnRightClickCommandChanged(ICommand? command)
    {
        _taskbarIcon?.RightClickCommand = command;
    }

    private bool IsDisposed { get; set; }

    public void Dispose()
    {
        try
        {
            _ipcProvider?.IpcProvider.Dispose();
            _taskbarIcon?.Dispose();
        }
        catch
        {
            // ignored
        }

        IsDisposed = true;
    }

    private void EnsureNotDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(TaskbarIcon));
        }
    }
    
    private record GetIconPathResponse(string IconPath);
    
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(GetIconPathResponse))]
    private partial class SourceGenerationContext : JsonSerializerContext
    {
        
    }
}