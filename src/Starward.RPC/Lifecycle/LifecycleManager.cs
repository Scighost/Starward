using Serilog;
using System.Diagnostics;

namespace Starward.RPC.Lifecycle;

internal static class LifecycleManager
{


    private static Process? _parentProcess;

    private static bool _keepRunning;

    private static bool _noLongerChange;


    public static void AssociateProcesses(int processId, bool keepRunning, bool noLongerChange = false)
    {
        try
        {
            if (_noLongerChange || processId < 0)
            {
                return;
            }
            _keepRunning = keepRunning;
            if (_parentProcess is not null)
            {
                _parentProcess.Exited -= _parentProcess_Exited;
                _parentProcess.Dispose();
                _parentProcess = null;
            }
            if (processId > 0)
            {
                _parentProcess = Process.GetProcessById(processId);
                _parentProcess.EnableRaisingEvents = true;
                _parentProcess.Exited += _parentProcess_Exited;
            }
            _noLongerChange = _noLongerChange || noLongerChange;
            Log.Information("Associated parent process: {ProcessId}, keepRunning: {keepRunning}, noLongerChange: {noLongerChange}", processId, keepRunning, noLongerChange);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to associate parent process.");
        }
    }



    public static (Process? Process, bool KeepRunning, bool NoLongerChange) GetAssociatedProcess()
    {
        return (_parentProcess, _keepRunning, _noLongerChange);
    }



    private static async void _parentProcess_Exited(object? sender, EventArgs e)
    {
        try
        {
            if (_parentProcess is not null)
            {
                _parentProcess.Exited -= _parentProcess_Exited;
                _parentProcess.Dispose();
                _parentProcess = null;
            }
            if (!_keepRunning)
            {
                Environment.Exit(0);
            }
            await Task.Delay(1000);
            GC.Collect();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Parent process exited.");
        }
    }






}
