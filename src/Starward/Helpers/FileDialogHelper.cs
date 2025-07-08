// https://referencesource.microsoft.com/#system.windows.forms/winforms/Managed/System/WinForms/FileDialog_Vista_Interop.cs

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;
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
                    dialog.GetOptions(out var options);
                    options |= FOS.FOS_DONTADDTORECENT;
                    dialog.SetOptions(options);
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


    public static async Task<string?> PickSingleFileAsync(XamlRoot xamlRoot, params (string Name, string Extension)[] fileTypeFilter)
    {
        return await PickSingleFileAsync((nint)xamlRoot.ContentIslandEnvironment.AppWindowId.Value, fileTypeFilter);
    }


    public static async Task<List<string>> PickMultipleFilesAsync(nint parentWindow, params (string Name, string Extension)[] fileTypeFilter)
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
            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
            return files.Select(x => x.Path).ToList();
        }
        catch (COMException)
        {
            return await Task.Run(() =>
            {
                IFileOpenDialog? dialog = null;
                IShellItemArray? shellArray = null;
                try
                {
                    dialog = new NativeFileOpenDialog();
                    dialog.GetOptions(out var options);
                    options |= FOS.FOS_ALLOWMULTISELECT;
                    options |= FOS.FOS_DONTADDTORECENT;
                    dialog.SetOptions(options);
                    SetFileTypeFilter(dialog, fileTypeFilter);
                    try
                    {
                        ((HRESULT)dialog.Show(parentWindow)).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
                    {
                        return [];
                    }
                    dialog.GetResults(out shellArray);
                    shellArray.GetCount(out uint count);
                    List<string> names = new List<string>((int)count);
                    for (int i = 0; i < count; i++)
                    {
                        shellArray.GetItemAt((uint)i, out IShellItem shellItem);
                        shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var name);
                        names.Add(name);
                    }
                    return names;
                }
                finally
                {
                    if (dialog != null) Marshal.FinalReleaseComObject(dialog);
                    if (shellArray != null) Marshal.FinalReleaseComObject(shellArray);
                }
            }).ConfigureAwait(false);
        }
    }


    public static async Task<List<string>> PickMultipleFilesAsync(XamlRoot xamlRoot, params (string Name, string Extension)[] fileTypeFilter)
    {
        return await PickMultipleFilesAsync((nint)xamlRoot.ContentIslandEnvironment.AppWindowId.Value, fileTypeFilter);
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
                    dialog.GetOptions(out var options);
                    options |= FOS.FOS_NOREADONLYRETURN;
                    options |= FOS.FOS_DONTADDTORECENT;
                    options |= FOS.FOS_OVERWRITEPROMPT;
                    dialog.SetOptions(options);
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


    public static async Task<string?> OpenSaveFileDialogAsync(XamlRoot xamlRoot, string? fileName = null, params (string Name, string Extension)[] fileTypeFilter)
    {
        return await OpenSaveFileDialogAsync((nint)xamlRoot.ContentIslandEnvironment.AppWindowId.Value, fileName, fileTypeFilter);
    }


    private static COMDLG_FILTERSPEC[] SetFileTypeFilter(in IFileDialog dialog, params (string Name, string Spec)[] fileTypeFilter)
    {
        uint count = (uint)fileTypeFilter.Length;
        COMDLG_FILTERSPEC[] types;
        if (fileTypeFilter == null || count == 0)
        {
            count++;
            types = [new COMDLG_FILTERSPEC { pszName = "All", pszSpec = "*" }];
        }
        else
        if (count == 1)
        {
            types = [new COMDLG_FILTERSPEC { pszName = fileTypeFilter[0].Name, pszSpec = "*" + fileTypeFilter[0].Spec }];
        }
        else
        {
            count++;
            types = new COMDLG_FILTERSPEC[count];
            types[0] = new COMDLG_FILTERSPEC { pszName = "All", pszSpec = string.Join(';', fileTypeFilter.Select(x => $"*{x.Spec}")) };
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
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add("*");
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
                    dialog.GetOptions(out var options);
                    options |= FOS.FOS_NOREADONLYRETURN;
                    options |= FOS.FOS_DONTADDTORECENT;
                    options |= FOS.FOS_PICKFOLDERS;
                    dialog.SetOptions(options);
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


    public static async Task<string?> PickFolderAsync(XamlRoot xamlRoot)
    {
        return await PickFolderAsync((nint)xamlRoot.ContentIslandEnvironment.AppWindowId.Value);
    }



}
