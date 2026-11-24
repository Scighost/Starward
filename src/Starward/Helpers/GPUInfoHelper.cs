using Microsoft.Win32;
using Starward.Core.HoYoPlay;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starward.Helpers;


public static class GPUInfoHelper
{

    private const string DisplayClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";


    public static List<GPUInfo> GetGPUInfos()
    {
        var gpuInfos = new List<GPUInfo>();
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(DisplayClassKey);
            if (key is not null)
            {
                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    // 显卡实例的子键名是 0000、0001…
                    if (subKeyName.Length != 4 || !int.TryParse(subKeyName, out _))
                    {
                        continue;
                    }
                    using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("DriverDesc") is not string name || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }
                    gpuInfos.Add(new GPUInfo
                    {
                        Name = name.Trim(),
                        DriverVersion = (subKey.GetValue("DriverVersion") as string)?.Trim() ?? "",
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        return gpuInfos;
    }


}
