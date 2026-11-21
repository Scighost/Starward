using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Starward.Features.Hypergryph;

internal sealed class EndfieldLaunchSession : IDisposable
{
    private SafeFileHandle? _job;
    private SafeProcessHandle? _parent;
    private SafeProcessHandle? _game;
    private bool _terminateGameOnDispose = true;
    public int GameProcessId { get; private set; }

    public static EndfieldLaunchSession Start(string parentPath, string gamePath, string arguments)
    {
        var session = new EndfieldLaunchSession();
        nint attributes = 0, parentValue = 0;
        bool attributesInitialized = false;
        try
        {
            session._job = CreateJobObjectW(0, null);
            if (session._job.IsInvalid) throw new Win32Exception();
            var limits = new ExtendedLimits();
            limits.Basic.LimitFlags = 0x2000 | 0x800; // KILL_ON_JOB_CLOSE | BREAKAWAY_OK (parent only)
            if (!SetInformationJobObject(session._job, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>()))
                throw new Win32Exception();

            var startup = new StartupInfo { Size = (uint)Marshal.SizeOf<StartupInfo>() };
            if (!CreateParent(parentPath, new StringBuilder($"\"{parentPath}\""), 0, 0, false,
                4, 0, Path.GetDirectoryName(parentPath)!, ref startup, out ProcessInformation parent))
                throw new Win32Exception();
            session._parent = new SafeProcessHandle(parent.Process, true);
            CloseHandle(parent.Thread);
            if (!AssignProcessToJobObject(session._job, session._parent)) throw new Win32Exception();

            nuint size = 0;
            InitializeProcThreadAttributeList(0, 1, 0, ref size);
            if (size == 0) throw new Win32Exception();
            attributes = Marshal.AllocHGlobal(checked((nint)size));
            if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size)) throw new Win32Exception();
            attributesInitialized = true;
            parentValue = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(parentValue, session._parent.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(attributes, 0, 0x00020000, parentValue, (nuint)nint.Size, 0, 0))
                throw new Win32Exception();

            var extended = new StartupInfoEx
            {
                Startup = new StartupInfo { Size = (uint)Marshal.SizeOf<StartupInfoEx>() },
                Attributes = attributes,
            };
            var commandLine = new StringBuilder($"\"{gamePath}\" {arguments}");
            // Create suspended so every failure can clean up before game code executes.
            if (!CreateGame(gamePath, commandLine, 0, 0, false, 0x01080004,
                0, Path.GetDirectoryName(gamePath)!, ref extended, out ProcessInformation game))
                throw new Win32Exception();
            session._game = new SafeProcessHandle(game.Process, true);
            try
            {
                if (!IsProcessInJob(session._game, session._job, out bool inJob)) throw new Win32Exception();
                if (inJob) throw new InvalidOperationException("Endfield could not leave the auxiliary process Job.");
                session.GameProcessId = checked((int)game.ProcessId);
                if (ResumeThread(game.Thread) == uint.MaxValue) throw new Win32Exception();
            }
            finally { CloseHandle(game.Thread); }
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
        finally
        {
            if (parentValue != 0) Marshal.FreeHGlobal(parentValue);
            if (attributesInitialized) DeleteProcThreadAttributeList(attributes);
            if (attributes != 0) Marshal.FreeHGlobal(attributes);
        }
    }

    public void ReleaseGameOwnership() => _terminateGameOnDispose = false;

    public void WaitForStartupGracePeriod(Action<string>? log = null)
    {
        using var process = Process.GetProcessById(GameProcessId);
        var startup = Stopwatch.StartNew();
        Stopwatch? grace = null;
        while (true)
        {
            uint result = WaitForSingleObject(_game!, 500);
            if (result == 0)
            {
                log?.Invoke("Game exited; cleaning up auxiliary parent immediately.");
                return;
            }
            if (result == uint.MaxValue) throw new Win32Exception();
            process.Refresh();
            if (grace is null && process.MainWindowHandle != 0 && process.Responding)
            {
                grace = Stopwatch.StartNew();
                log?.Invoke("Responsive game window detected; starting 30-second grace period.");
            }
            if (grace?.Elapsed >= TimeSpan.FromSeconds(30))
            {
                log?.Invoke("30-second grace period complete; releasing auxiliary parent, leaving game running.");
                return;
            }
            if (grace is null && startup.Elapsed >= TimeSpan.FromMinutes(2))
                throw new TimeoutException("Endfield did not create a responsive game window within two minutes.");
        }
    }

    public void Dispose()
    {
        // Also cover failures before Job assignment. Only handles created by this instance are terminated.
        if (_terminateGameOnDispose && _game is { IsClosed: false, IsInvalid: false }) TerminateProcess(_game, 0);
        if (_parent is { IsClosed: false, IsInvalid: false }) TerminateProcess(_parent, 0);
        _job?.Dispose();
        if (_parent is { IsClosed: false, IsInvalid: false }) WaitForSingleObject(_parent, 5000);
        _game?.Dispose();
        _parent?.Dispose();
    }

    internal static bool VerifySignature(string path)
    {
        nint filePath = Marshal.StringToCoTaskMemUni(path);
        nint fileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<TrustFileInfo>());
        var data = new TrustData
        {
            Size = (uint)Marshal.SizeOf<TrustData>(),
            UIChoice = 2, UnionChoice = 1, File = fileInfo, StateAction = 1,
            ProviderFlags = 0x1000, // WTD_CACHE_ONLY_URL_RETRIEVAL; do not block launch on network access.
        };
        Guid action = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
        try
        {
            Marshal.StructureToPtr(new TrustFileInfo
            {
                Size = (uint)Marshal.SizeOf<TrustFileInfo>(), Path = filePath,
            }, fileInfo, false);
            return WinVerifyTrust(new nint(-1), ref action, ref data) == 0;
        }
        finally
        {
            data.StateAction = 2;
            WinVerifyTrust(new nint(-1), ref action, ref data);
            Marshal.FreeHGlobal(fileInfo);
            Marshal.FreeCoTaskMem(filePath);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public uint Size;
        public nint Reserved, Desktop, Title;
        public uint X, Y, Width, Height, XChars, YChars, Fill, Flags;
        public ushort ShowWindow, ReservedSize;
        public nint ReservedData, StdInput, StdOutput, StdError;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx { public StartupInfo Startup; public nint Attributes; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation { public nint Process, Thread; public uint ProcessId, ThreadId; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long ProcessTime, JobTime;
        public uint LimitFlags;
        public nuint MinWorkingSet, MaxWorkingSet;
        public uint ActiveProcesses;
        public nuint Affinity;
        public uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
        public nuint ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct TrustFileInfo { public uint Size; public nint Path, File, KnownSubject; }
    [StructLayout(LayoutKind.Sequential)]
    private struct TrustData
    {
        public uint Size;
        public nint PolicyCallback, SIPClient;
        public uint UIChoice, RevocationChecks, UnionChoice;
        public nint File;
        public uint StateAction;
        public nint StateData, URLReference;
        public uint ProviderFlags, UIContext;
        public nint SignatureSettings;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateParent(string app, StringBuilder command, nint processSecurity, nint threadSecurity,
        bool inherit, uint flags, nint environment, string directory, ref StartupInfo startup, out ProcessInformation process);
    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateGame(string app, StringBuilder command, nint processSecurity, nint threadSecurity,
        bool inherit, uint flags, nint environment, string directory, ref StartupInfoEx startup, out ProcessInformation process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObjectW(nint attributes, string? name);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int infoClass, ref ExtendedLimits info, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeProcessHandle process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(SafeProcessHandle process, SafeFileHandle job, out bool result);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(nint list, int count, uint flags, ref nuint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute, nint value, nuint size, nint previous, nint returned);
    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(nint list);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(nint thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(SafeProcessHandle process, uint exitCode);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern int WinVerifyTrust(nint window, ref Guid action, ref TrustData data);
}
