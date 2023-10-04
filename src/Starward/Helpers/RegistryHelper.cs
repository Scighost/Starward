using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Text;

namespace Starward.Helpers;

internal static class RegistryHelper
{



    public static void SetValue(params (string keyName, string? valueName, object value, RegistryValueKind valueKind)[] items)
    {
        if (AppConfig.MsixPackaged)
        {
            var sb = new StringBuilder();
            foreach ((string keyName, string? valueName, object value, RegistryValueKind valueKind) in items)
            {
                if (valueKind is RegistryValueKind.Binary && value is byte[] bytes)
                {
                    string base64 = Convert.ToBase64String(bytes);
                    sb.AppendLine($"[byte[]] $array = [System.Convert]::FromBase64String('{base64}');");
                    sb.AppendLine($"Set-ItemProperty -Path '{keyName.Replace("HKEY_CURRENT_USER", "HKCU:")}' -Name '{valueName}' -Value $array;");
                }
                if (valueKind is RegistryValueKind.DWord)
                {
                    sb.AppendLine($"Set-ItemProperty -Path '{keyName.Replace("HKEY_CURRENT_USER", "HKCU:")}' -Name '{valueName}' -Value {value};");
                }
                if (valueKind is RegistryValueKind.String)
                {
                    sb.AppendLine($"Set-ItemProperty -Path '{keyName.Replace("HKEY_CURRENT_USER", "HKCU:")}' -Name '{valueName}' -Value '{value}';");
                }
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = "PowerShell",
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Arguments = sb.ToString(),
            })?.WaitForExit(1000);
        }
        else
        {
            foreach ((string keyName, string? valueName, object value, RegistryValueKind valueKind) in items)
            {
                Registry.SetValue(keyName, valueName, value, valueKind);
            }
        }
    }






}
