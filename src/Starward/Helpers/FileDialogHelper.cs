// https://referencesource.microsoft.com/#system.windows.forms/winforms/Managed/System/WinForms/FileDialog_Vista_Interop.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static Starward.Helpers.FileDialogNative;

namespace Starward.Helpers;

internal static class FileDialogHelper
{
    /// <summary>
    /// The operation was canceled by the user.
    /// </summary>
    /// <remarks>
    /// see https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
    /// </remarks>
    private const int ERROR_CANCELLED = 0x000004C7;

    public static async Task<string?> PickSingleFileAsync(nint parentWindow, params (string Name, string Extension)[] fileTypeFilter)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            foreach (var filter in fileTypeFilter)
            {
                picker.FileTypeFilter.Add(filter.Extension);
            }
            InitializeWithWindow.Initialize(picker, parentWindow);
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
        catch (COMException)
        {
            return await Task.Run(() =>
            {
                IFileOpenDialog? dialog = null;
                IShellItem? shell = null;

                try
                {
                    dialog = new NativeFileOpenDialog();
                    dialog.SetOptions(FOS.FOS_NOREADONLYRETURN | FOS.FOS_DONTADDTORECENT);

                    SetFileTypeFilter(dialog, fileTypeFilter);

                    try
                    {
                        ((HRESULT)dialog.Show(parentWindow)).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
                    {
                        return null;
                    }


                    dialog.GetResult(out shell);
                    shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var name);
                    return name;
                }
                finally
                {
                    if (dialog != null) Marshal.FinalReleaseComObject(dialog);
                    if (shell != null) Marshal.FinalReleaseComObject(shell);
                }
            }).ConfigureAwait(false);
        }
    }



    public static async Task<string?> OpenSaveFileDialogAsync(nint parentWindow, string? fileName = null, params (string Name, string Extension)[] fileTypeFilter)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                picker.SuggestedFileName = fileName;
            }
            foreach (var filter in fileTypeFilter)
            {
                picker.FileTypeChoices.Add(filter.Name, new List<string> { filter.Extension });
            }
            InitializeWithWindow.Initialize(picker, parentWindow);
            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }
        catch (COMException)
        {
            return await Task.Run(() =>
            {
                IFileSaveDialog? dialog = null;
                IShellItem? shell = null;

                try
                {
                    dialog = new NativeFileSaveDialog();
                    dialog.SetOptions(FOS.FOS_NOREADONLYRETURN | FOS.FOS_DONTADDTORECENT | FOS.FOS_OVERWRITEPROMPT);

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        dialog.SetFileName(fileName);
                    }

                    var types = SetFileTypeFilter(dialog, fileTypeFilter);

                    try
                    {
                        ((HRESULT)dialog.Show(parentWindow)).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
                    {
                        return null;
                    }

                    dialog.GetResult(out shell);
                    shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var name);
                    dialog.GetFileTypeIndex(out uint index);
                    var extension = Path.GetExtension(types[index - 1].pszSpec);
                    if (!name.EndsWith(extension))
                    {
                        name += extension;
                    }
                    return name;
                }
                finally
                {
                    if (dialog != null) Marshal.FinalReleaseComObject(dialog);
                    if (shell != null) Marshal.FinalReleaseComObject(shell);
                }
            }).ConfigureAwait(false);
        }
    }



    private static COMDLG_FILTERSPEC[] SetFileTypeFilter(in IFileDialog dialog, params (string Name, string Spec)[] fileTypeFilter)
    {
        uint count = (uint)fileTypeFilter.Length;
        COMDLG_FILTERSPEC[] types;
        if (fileTypeFilter == null || count == 0)
        {
            count++;
            types = new COMDLG_FILTERSPEC[] { new COMDLG_FILTERSPEC { pszName = "all", pszSpec = "*" } };
        }
        else
        if (count == 1)
        {
            types = new COMDLG_FILTERSPEC[] { new COMDLG_FILTERSPEC { pszName = fileTypeFilter[0].Name, pszSpec = "*" + fileTypeFilter[0].Spec } };
        }
        else
        {
            count++;
            types = new COMDLG_FILTERSPEC[count];
            types[0] = new COMDLG_FILTERSPEC { pszName = "all", pszSpec = string.Join(';', fileTypeFilter.Select(x => $"*{x.Spec}")) };
            fileTypeFilter.Select(x => new COMDLG_FILTERSPEC { pszName = x.Name, pszSpec = x.Spec }).ToArray().CopyTo(types, 1);
        }
        dialog.SetFileTypes(count, types);
        return types;
    }



    public static async Task<string?> PickFolderAsync(nint parentWindow)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            InitializeWithWindow.Initialize(picker, parentWindow);
            var file = await picker.PickSingleFolderAsync();
            return file?.Path;
        }
        catch (COMException)
        {
            return await Task.Run(() =>
            {
                IFileDialog? dialog = null;
                IShellItem? shell = null;

                try
                {
                    dialog = new NativeFileOpenDialog();
                    dialog.SetOptions(FOS.FOS_NOREADONLYRETURN | FOS.FOS_DONTADDTORECENT | FOS.FOS_PICKFOLDERS);

                    try
                    {
                        ((HRESULT)dialog.Show(parentWindow)).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
                    {
                        return null;
                    }

                    dialog.GetResult(out shell);
                    shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var name);
                    return name;
                }
                finally
                {
                    if (dialog != null) Marshal.FinalReleaseComObject(dialog);
                    if (shell != null) Marshal.FinalReleaseComObject(shell);
                }
            }).ConfigureAwait(false);
        }
    }




}
