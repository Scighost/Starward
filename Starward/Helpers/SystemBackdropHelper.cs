using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System;
using System.Net;
using System.Runtime.InteropServices;
using Windows.UI;
using WinRT;

namespace Starward.Helpers;

public class SystemBackdropHelper
{

    private readonly Window _window;

    private WindowsSystemDispatcherQueueHelper? wsdqHelper; // See below for implementation.

    private SystemBackdropConfiguration? configurationSource;

    private MicaController? micaController;

    private DesktopAcrylicController? acrylicController;

    private SystemBackdropProperty? backdropProperty;

    private bool alwaysActive;




    public SystemBackdropHelper(Window window, SystemBackdropProperty? backdropProperty = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
        this.backdropProperty = backdropProperty;
    }



    public void ResetBackdrop()
    {
        micaController?.Dispose();
        micaController = null;
        acrylicController?.Dispose();
        acrylicController = null;
        _window.Activated -= Window_Activated;
        _window.Closed -= Window_Closed;
        ((FrameworkElement)_window.Content).ActualThemeChanged -= Window_ThemeChanged;
        configurationSource = null;
        alwaysActive = false;
    }



    public void SetBackdropProperty(SystemBackdropProperty? backdropProperty = null)
    {
        micaController?.ResetProperties();
        acrylicController?.ResetProperties();
        this.backdropProperty = backdropProperty;
        SetControllerProperties();
    }



    public bool TrySetMica(bool useMicaAlt = false, bool fallbackToAcrylic = false, bool alwaysActive = false)
    {
        ResetBackdrop();
        if (MicaController.IsSupported())
        {
            wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            configurationSource = new SystemBackdropConfiguration();
            _window.Activated += Window_Activated;
            _window.Closed += Window_Closed;
            ((FrameworkElement)_window.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            micaController = new MicaController { Kind = useMicaAlt ? MicaKind.BaseAlt : MicaKind.Base };
            SetControllerProperties();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            micaController.AddSystemBackdropTarget(_window.As<ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(configurationSource);

            this.alwaysActive = alwaysActive;
            return true; // succeeded
        }
        else if (fallbackToAcrylic)
        {
            return TrySetAcrylic(alwaysActive);
        }
        else
        {
            return false;
        }
    }



    public bool TrySetAcrylic(bool alwaysActive = false)
    {
        ResetBackdrop();
        if (DesktopAcrylicController.IsSupported())
        {
            wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            configurationSource = new SystemBackdropConfiguration();
            _window.Activated += Window_Activated;
            _window.Closed += Window_Closed;
            ((FrameworkElement)_window.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            acrylicController = new DesktopAcrylicController();
            SetControllerProperties();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            acrylicController.AddSystemBackdropTarget(_window.As<ICompositionSupportsSystemBackdrop>());
            acrylicController.SetSystemBackdropConfiguration(configurationSource);

            this.alwaysActive = alwaysActive;
            return true; // succeeded
        }
        else
        {
            return false;
        }
    }




    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (configurationSource != null)
        {
            configurationSource.IsInputActive = alwaysActive || args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }


    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (configurationSource != null)
        {
            SetConfigurationSourceTheme();
        }
        if (backdropProperty != null)
        {
            SetControllerProperties();
        }
    }


    private void Window_Closed(object sender, WindowEventArgs args)
    {
        ResetBackdrop();
    }



    private void SetConfigurationSourceTheme()
    {
        if (configurationSource != null)
        {
            configurationSource.Theme = ((FrameworkElement)_window.Content).ActualTheme switch
            {
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                _ => SystemBackdropTheme.Default,
            };
        }
    }



    private void SetControllerProperties()
    {
        if (backdropProperty != null)
        {
            var actualTheme = ((FrameworkElement)_window.Content).ActualTheme;
            if (actualTheme is ElementTheme.Default)
            {
                acrylicController?.ResetProperties();
                micaController?.ResetProperties();
            }
            if (actualTheme is ElementTheme.Light)
            {
                if (acrylicController != null)
                {
                    acrylicController.FallbackColor = backdropProperty.FallbackColorLight.ToColor();
                    acrylicController.LuminosityOpacity = backdropProperty.LuminosityOpacityLight;
                    acrylicController.TintColor = backdropProperty.TintColorLight.ToColor();
                    acrylicController.TintOpacity = backdropProperty.TintOpacityLight;
                }
                if (micaController != null)
                {
                    micaController.FallbackColor = backdropProperty.FallbackColorLight.ToColor();
                    micaController.LuminosityOpacity = backdropProperty.LuminosityOpacityLight;
                    micaController.TintColor = backdropProperty.TintColorLight.ToColor();
                    micaController.TintOpacity = backdropProperty.TintOpacityLight;
                }
            }
            if (actualTheme is ElementTheme.Dark)
            {
                if (acrylicController != null)
                {
                    acrylicController.FallbackColor = backdropProperty.FallbackColorDark.ToColor();
                    acrylicController.LuminosityOpacity = backdropProperty.LuminosityOpacityDark;
                    acrylicController.TintColor = backdropProperty.TintColorDark.ToColor();
                    acrylicController.TintOpacity = backdropProperty.TintOpacityDark;
                }
                if (micaController != null)
                {
                    micaController.FallbackColor = backdropProperty.FallbackColorDark.ToColor();
                    micaController.LuminosityOpacity = backdropProperty.LuminosityOpacityDark;
                    micaController.TintColor = backdropProperty.TintColorDark.ToColor();
                    micaController.TintOpacity = backdropProperty.TintOpacityDark;
                }
            }
        }
    }



    private class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController(in DispatcherQueueOptions options, out nint dispatcherQueueController);

        nint m_dispatcherQueueController;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == 0)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                _ = CreateDispatcherQueueController(options, out m_dispatcherQueueController);
            }
        }
    }


}



public record SystemBackdropProperty
{

    public required uint FallbackColorDark { get; init; }

    public required uint FallbackColorLight { get; init; }

    public required float LuminosityOpacityDark { get; init; }

    public required float LuminosityOpacityLight { get; init; }

    public required uint TintColorDark { get; init; }

    public required uint TintColorLight { get; init; }

    public required float TintOpacityDark { get; init; }

    public required float TintOpacityLight { get; init; }



    public static readonly SystemBackdropProperty AcrylicDefault = new()
    {
        FallbackColorDark = 0xFF545454,
        FallbackColorLight = 0xFFD3D3D3,
        LuminosityOpacityDark = 0.64f,
        LuminosityOpacityLight = 0.64f,
        TintColorDark = 0xFF545454,
        TintColorLight = 0xFFD3D3D3,
        TintOpacityDark = 0,
        TintOpacityLight = 0,
    };

    public static readonly SystemBackdropProperty MicaDefault = new()
    {
        FallbackColorDark = 0xFF202020,
        FallbackColorLight = 0xFFF3F3F3,
        LuminosityOpacityDark = 1,
        LuminosityOpacityLight = 1,
        TintColorDark = 0xFF202020,
        TintColorLight = 0xFFF3F3F3,
        TintOpacityDark = 0.8f,
        TintOpacityLight = 0.5f,
    };

    public static readonly SystemBackdropProperty MicaAltDefault = new()
    {
        FallbackColorDark = 0xFF202020,
        FallbackColorLight = 0xFFE8E8E8,
        LuminosityOpacityDark = 1,
        LuminosityOpacityLight = 1,
        TintColorDark = 0xFF0A0A0A,
        TintColorLight = 0xFFDADADA,
        TintOpacityDark = 0,
        TintOpacityLight = 0.5f,
    };

}


file static class UInt32ToColorHelper
{
    [StructLayout(LayoutKind.Explicit)]
    private struct UInt32ToColor
    {
        [FieldOffset(0)]
        public uint Value;

        [FieldOffset(0)]
        public Color Color;
    }

    public static Color ToColor(this uint value)
    {
        return new UInt32ToColor { Value = (uint)IPAddress.HostToNetworkOrder((int)value) }.Color;
    }
}