using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Foundation;
using Windows.Graphics.DirectX;


namespace Starward.Features.GameSetting;

[INotifyPropertyChanged]
public sealed partial class GenshinHDRLuminanceSettingDialog : ContentDialog
{

    public GameBiz CurrentGameBiz { get; set; }


    private DisplayInformation _displayInformation;


    public GenshinHDRLuminanceSettingDialog()
    {
        InitializeComponent();
        this.Loaded += GenshinHDRLuminanceSettingDialog_Loaded;
        this.Unloaded += GenshinHDRLuminanceSettingDialog_Unloaded;
    }



    private async void GenshinHDRLuminanceSettingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            this.XamlRoot.Changed -= XamlRoot_Changed;
            this.XamlRoot.Changed += XamlRoot_Changed;
            this.XamlRoot.SetWindowDragRectangles([new Rect(0, 0, 10000, 48)]);
            _displayInformation = DisplayInformation.CreateForWindowId(this.XamlRoot.GetAppWindow().Id);
            _displayInformation.AdvancedColorInfoChanged += _displayInformation_AdvancedColorInfoChanged;
            UpdateDisplayInfomation(_displayInformation);
            sceneBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), Path.Combine(AppContext.BaseDirectory, @"Assets\Image\Scene.jxr"));
            moraBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), Path.Combine(AppContext.BaseDirectory, @"Assets\Image\Sign.png"));
            uiBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), Path.Combine(AppContext.BaseDirectory, @"Assets\Image\UI.jxr"));
            (MaxLuminance, SceneLuminance, UILuminance) = GameSettingService.GetGenshinHDRLuminance(CurrentGameBiz);
            DrawMora();
            DrawScene();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        try
        {
            sender.SetWindowDragRectangles([new Rect(0, 0, 10000, 48)]);
        }
        catch { }
    }


    private void GenshinHDRLuminanceSettingDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            sceneBitmap?.Dispose();
            moraBitmap?.Dispose();
            uiBitmap?.Dispose();
            _displayInformation?.AdvancedColorInfoChanged -= _displayInformation_AdvancedColorInfoChanged;
            _displayInformation?.Dispose();
            CanvasSwapChainPanel_Mora.SwapChain?.Dispose();
            CanvasSwapChainPanel_Scene.SwapChain?.Dispose();
            this.XamlRoot.Changed -= XamlRoot_Changed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private CanvasBitmap moraBitmap;

    private CanvasBitmap sceneBitmap;

    private CanvasBitmap uiBitmap;

    public bool SupportHDR { get; set => SetProperty(ref field, value); }

    public string DisplayInfomation { get; set => SetProperty(ref field, value); }

    public bool MoraVisibility { get; set => SetProperty(ref field, value); } = true;



    public int MaxLuminance
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                DrawMora();
            }
        }
    } = 1000;


    public int SceneLuminance
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                DrawScene();
            }
        }
    } = 300;


    public int UILuminance
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                DrawScene();
            }
        }
    } = 350;




    private void DrawMora()
    {
        try
        {
            float w = (float)CanvasSwapChainPanel_Mora.Width;
            float h = (float)CanvasSwapChainPanel_Mora.Height;
            int dpi = (int)(CanvasSwapChainPanel_Mora.CompositionScaleX * 96);
            CanvasSwapChainPanel_Mora.SwapChain ??= new CanvasSwapChain(CanvasDevice.GetSharedDevice(), w, h, dpi, DirectXPixelFormat.R16G16B16A16Float, 2, CanvasAlphaMode.Premultiplied);
            CanvasSwapChain swapChain = CanvasSwapChainPanel_Mora.SwapChain;
            if (dpi != swapChain.Dpi)
            {
                swapChain.ResizeBuffers(w, h, dpi, DirectXPixelFormat.R16G16B16A16Float, 2);
            }

            float gain = MaxLuminance / 80f;
            using CanvasDrawingSession ds = swapChain.CreateDrawingSession(Colors.Transparent);
            // HDR White Maxluminance
            ds.Clear(new System.Numerics.Vector4(125, 125, 125, 1));
            var effect = new LinearTransferEffect
            {
                Source = moraBitmap,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
                RedSlope = gain,
                GreenSlope = gain,
                BlueSlope = gain,
            };
            ds.DrawImage(effect, new Rect(0.1f * w, 0.1f * h, 0.8f * w, 0.8f * h), moraBitmap.Bounds, 1, CanvasImageInterpolation.Cubic);
            ds.Dispose();
            swapChain.Present();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private void DrawScene()
    {
        try
        {
            float w = (float)CanvasSwapChainPanel_Scene.Width;
            float h = (float)CanvasSwapChainPanel_Scene.Height;
            int dpi = (int)(CanvasSwapChainPanel_Scene.CompositionScaleX * 96);
            CanvasSwapChainPanel_Scene.SwapChain ??= new CanvasSwapChain(CanvasDevice.GetSharedDevice(), w, h, dpi, DirectXPixelFormat.R16G16B16A16Float, 2, CanvasAlphaMode.Premultiplied);
            CanvasSwapChain swapChain = CanvasSwapChainPanel_Scene.SwapChain;
            if (dpi != swapChain.Dpi)
            {
                swapChain.ResizeBuffers(w, h, dpi, DirectXPixelFormat.R16G16B16A16Float, 2);
            }

            float sceneGain = SceneLuminance / 80f;
            using CanvasDrawingSession ds = swapChain.CreateDrawingSession(Colors.Transparent);
            var sceneEffect = new LinearTransferEffect
            {
                Source = sceneBitmap,
                RedSlope = sceneGain,
                GreenSlope = sceneGain,
                BlueSlope = sceneGain,
            };
            ds.DrawImage(sceneEffect, new Rect(0, 0, w, h), sceneBitmap.Bounds, 1, CanvasImageInterpolation.Cubic);

            // https://github.com/CollapseLauncher/Collapse/blob/cb9ed246edec06d3bfcbcdde7057c917a557cdc3/CollapseLauncher/XAMLs/MainApp/Pages/GameSettingsPages/GenshinGameSettingsPage.xaml.cs#L533
            static float GammaCorrection(float val, float max) => val * MathF.Pow(val / max, 2.2f);
            float uiGain = (GammaCorrection((UILuminance - SceneLuminance + 350f) * 2, 1600) + 50) / 80;
            var uiEffect = new LinearTransferEffect
            {
                Source = uiBitmap,
                RedSlope = uiGain,
                GreenSlope = uiGain,
                BlueSlope = uiGain,
            };
            ds.DrawImage(uiEffect, new Rect(0, 0, w, h), sceneBitmap.Bounds, 1, CanvasImageInterpolation.Cubic);
            ds.Dispose();
            swapChain.Present();

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    private void _displayInformation_AdvancedColorInfoChanged(DisplayInformation sender, object args)
    {
        try
        {
            UpdateDisplayInfomation(sender);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private void UpdateDisplayInfomation(DisplayInformation display)
    {
        var info = display.GetAdvancedColorInfo();
        SupportHDR = info.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange;
        string kind = info.CurrentAdvancedColorKind switch
        {
            DisplayAdvancedColorKind.StandardDynamicRange => $"{Lang.GenshinHDRLuminanceSettingDialog_StandardDynamicRange} (SDR)",
            DisplayAdvancedColorKind.WideColorGamut => $"{Lang.GenshinHDRLuminanceSettingDialog_WideColorGamut} (WCG)",
            DisplayAdvancedColorKind.HighDynamicRange => $"{Lang.GenshinHDRLuminanceSettingDialog_HighDynamicRange} (HDR)",
            _ => "",
        };
        DisplayInfomation = $"""
            {Lang.GenshinHDRLuminanceSettingDialog_ColorSpace}: {kind}
            {Lang.GenshinHDRLuminanceSettingDialog_PeakLuminance}: {info.MaxLuminanceInNits} nits
            {Lang.GenshinHDRLuminanceSettingDialog_MaxFullScreenLuminance}: {info.MaxAverageFullFrameLuminanceInNits} nits
            {Lang.GenshinHDRLuminanceSettingDialog_SDRWhiteLuminance}: {info.SdrWhiteLevelInNits} nits
            """;
    }




    [RelayCommand]
    private void Save()
    {

        try
        {
            GameSettingService.SetGenshinHDRLuminance(CurrentGameBiz, MaxLuminance, SceneLuminance, UILuminance);
            this.Hide();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    [RelayCommand]
    private void Reset()
    {
        try
        {
            MaxLuminance = 1000;
            SceneLuminance = 300;
            UILuminance = 350;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    [RelayCommand]
    private void AutoAdjust()
    {
        try
        {
            var info = _displayInformation.GetAdvancedColorInfo();
            MaxLuminance = (int)Math.Clamp(info.MaxLuminanceInNits, 300, 2000);
            SceneLuminance = (int)Math.Clamp(info.SdrWhiteLevelInNits + 20, 100, 500);
            UILuminance = SceneLuminance + 50;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }





    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }


}
