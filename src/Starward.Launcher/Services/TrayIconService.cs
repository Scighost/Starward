using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Hosting;

namespace Starward.Launcher.Services;

internal class TrayIconService : IDisposable
{
    private readonly CancellationToken _stoppingToken;
    private readonly TrayIcon _trayIcon;
    private Icon? _icon;

    private Thread? _uiThread;

    public TrayIconService(IHostApplicationLifetime lifetime)
    {
        _stoppingToken = lifetime.ApplicationStopping;
        _trayIcon = new TrayIcon
        {
            ToolTip = "Starward"
        };
        _trayIcon.MessageWindow.MouseEventReceived += OnMouseEventReceived;
        _trayIcon.MessageWindow.TaskbarCreated += OnTaskbarCreated;
    }

    private bool IsDisposed { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event Action? OnLeftClick;
    public event Action? OnRightClick;

    public void Create()
    {
        uint threadId = 0;
        _uiThread = new Thread(() =>
        {
            threadId = PInvoke.GetCurrentThreadId();
            _trayIcon.Create();
            while (!_stoppingToken.IsCancellationRequested && PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(in msg);
                PInvoke.DispatchMessage(in msg);
                Debug.WriteLine(DateTime.Now);
            }
        });
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();
        _stoppingToken.Register(() => PInvoke.PostThreadMessage(threadId, PInvoke.WM_QUIT, 0, 0));
    }

    public void SetIcon(string path)
    {
        EnsureNotDisposed();
        _icon?.Dispose();
        _icon = new Icon(path);
        _trayIcon.Icon = _icon.Handle;
    }

    private void OnTaskbarCreated(object? sender, EventArgs e)
    {
        try
        {
            _ = _trayIcon.TryRemove();
            _trayIcon.Create();
        }
        catch
        {
            // ignored
        }
    }

    private void OnMouseEventReceived(object? sender, MessageWindow.MouseEventReceivedEventArgs args)
    {
        switch (args.MouseEvent)
        {
            case MouseEvent.IconLeftMouseUp:
                OnLeftClick?.Invoke();
                break;
            case MouseEvent.IconRightMouseUp:
                OnRightClick?.Invoke();
                break;
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    ~TrayIconService()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed || !disposing) return;
        IsDisposed = true;
        _trayIcon.Dispose();
        _icon?.Dispose();
        _uiThread?.Join();
    }
}