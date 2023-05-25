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
using static Vanara.PInvoke.Shell32;

namespace Starward.Helpers;

internal static class FileDialogHelper
{


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
                    dialog = new IFileOpenDialog();
                    dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_NOREADONLYRETURN | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT);

                    SetFileTypeFilter(dialog, fileTypeFilter);

                    try
                    {
                        dialog.Show(parentWindow).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                    {
                        return null;
                    }


                    shell = dialog.GetResult();
                    return shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
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
                    dialog = new IFileSaveDialog();
                    dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_NOREADONLYRETURN | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT | FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT);

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        dialog.SetFileName(fileName);
                    }

                    var types = SetFileTypeFilter(dialog, fileTypeFilter);

                    try
                    {
                        dialog.Show(parentWindow).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                    {
                        return null;
                    }

                    shell = dialog.GetResult();
                    var name = shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
                    var index = dialog.GetFileTypeIndex();
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
                    dialog = new IFileOpenDialog();
                    dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_NOREADONLYRETURN | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT | FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

                    try
                    {
                        dialog.Show(parentWindow).ThrowIfFailed();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                    {
                        return null;
                    }

                    shell = dialog.GetResult();
                    return shell.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
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
