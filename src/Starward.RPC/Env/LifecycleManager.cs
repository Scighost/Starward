using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Starward.RPC.Env;

internal static class LifecycleManager
{


    private static Process? _parentProcess;

    private static bool _keepRunning;

    private static bool _noLongerChange;

    public static event EventHandler<Process>? ParentProcessExited;


    public static void SetParentProcess(int processId, bool keepRunning, bool noLongerChange = false)
    {
        try
        {
            if (_noLongerChange || processId < 0)
            {
                return;
            }
            _keepRunning = keepRunning;
            if (_parentProcess is not null && _parentProcess.Id == processId)
            {
                return;
            }
            else if (processId > 0)
            {
                if (_parentProcess is not null)
                {
                    _parentProcess.Exited -= _parentProcess_Exited;
                    ParentProcessExited?.Invoke(null, _parentProcess);
                    _parentProcess.Dispose();
                    _parentProcess = null;
                }
                _parentProcess = Process.GetProcessById(processId);
                _parentProcess.EnableRaisingEvents = true;
                _parentProcess.Exited += _parentProcess_Exited;
                _noLongerChange = _noLongerChange || noLongerChange;
                Log.Information("Set parent process: {ProcessId}, keepRunning: {keepRunning}, noLongerChange: {noLongerChange}", processId, keepRunning, noLongerChange);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set parent process.");
        }
    }



    public static (Process? Process, bool KeepRunning, bool NoLongerChange) GetParentProcess()
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
                ParentProcessExited?.Invoke(null, _parentProcess);
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
