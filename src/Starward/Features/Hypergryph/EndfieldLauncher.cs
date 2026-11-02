using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Hypergryph;

internal static partial class EndfieldLauncher
{
    internal const string Command = "endfield-launch-helper";
    private const string PipePrefix = "Starward.Endfield.";

    private sealed record LaunchResult(int ProcessId, string? Error);

    [JsonSerializable(typeof(LaunchResult))]
    private partial class LaunchJsonContext : JsonSerializerContext;

    public static async Task<Process> StartAsync(string gamePath, string? arguments)
    {
        gamePath = ValidateGamePath(gamePath);
        string pipeName = PipePrefix + Guid.NewGuid().ToString("N");
        await using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var start = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        start.ArgumentList.Add(Command);
        start.ArgumentList.Add(gamePath);
        start.ArgumentList.Add(arguments ?? "");
        start.ArgumentList.Add(pipeName);
        using Process helper = Process.Start(start) ?? throw new InvalidOperationException("Could not start the Endfield launch helper.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            Task connected = pipe.WaitForConnectionAsync(timeout.Token);
            Task exited = helper.WaitForExitAsync(timeout.Token);
            if (await Task.WhenAny(connected, exited) == exited && !pipe.IsConnected)
            {
                await exited;
                throw new InvalidOperationException($"Endfield launch helper exited before connecting (code {helper.ExitCode}).");
            }
            await connected;
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            string? line = await reader.ReadLineAsync(timeout.Token);
            LaunchResult? result = line is null ? null : JsonSerializer.Deserialize(line, LaunchJsonContext.Default.LaunchResult);
            if (result is null || result.ProcessId <= 0)
            {
                throw new InvalidOperationException(result?.Error ?? "The Endfield launch helper returned no result.");
            }
            return Process.GetProcessById(result.ProcessId);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Timed out waiting for the Endfield launch helper.");
        }
    }

    // Runs before WinUI/App initialization, independently of the main Starward window.
    internal static int RunHelper(string[] args)
    {
        if (args.Length != 4 || !args[3].StartsWith(PipePrefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(args[3][PipePrefix.Length..], "N", out _))
        {
            return 2;
        }
        using var pipe = new NamedPipeClientStream(".", args[3], PipeDirection.Out);
        try { pipe.Connect(15000); }
        catch { return 2; }
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        bool reported = false;
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            {
                throw new InvalidOperationException("Endfield requires administrator privileges.");
            }
            string gamePath = ValidateGamePath(args[1]);
            // Serialize helpers in this session, without terminating another instance's parent.
            using var mutex = new Mutex(false, @"Local\Starward.Endfield.Launch");
            bool ownsMutex;
            try { ownsMutex = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { ownsMutex = true; }
            if (!ownsMutex) throw new InvalidOperationException("An Endfield launch session is already active.");
            try
            {
                foreach (Process process in Process.GetProcessesByName("Endfield"))
                {
                    using (process)
                    {
                        if (process.SessionId == Process.GetCurrentProcess().SessionId)
                            throw new InvalidOperationException("Endfield is already running.");
                    }
                }
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Starward", "EndfieldLauncher", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                try
                {
                    string parentPath = Path.Combine(directory, "games.exe");
                    PrepareParent(Path.GetDirectoryName(gamePath)!, parentPath);
                    using var session = EndfieldLaunchSession.Start(parentPath, gamePath, args[2]);
                    LogHelper($"Started game PID {session.GameProcessId}; auxiliary image: {parentPath}");
                    writer.WriteLine(JsonSerializer.Serialize(new LaunchResult(session.GameProcessId, null), LaunchJsonContext.Default.LaunchResult));
                    reported = true;
                    session.ReleaseGameOwnership();
                    session.WaitForStartupGracePeriod(LogHelper);
                }
                finally
                {
                    // This is a unique directory created by this invocation, never the game directory.
                    try
                    {
                        File.Delete(Path.Combine(directory, "games.exe"));
                        Directory.Delete(directory);
                        LogHelper("Auxiliary process and temporary image cleaned up.");
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        LogHelper($"Temporary image cleanup failed: {ex.Message}");
                    }
                }
            }
            finally { mutex.ReleaseMutex(); }
            return 0;
        }
        catch (Exception ex)
        {
            LogHelper(ex.ToString());
            if (!reported)
            {
                try { writer.WriteLine(JsonSerializer.Serialize(new LaunchResult(0, ex.Message), LaunchJsonContext.Default.LaunchResult)); }
                catch (IOException) { }
            }
            return 1;
        }
    }

    private static void LogHelper(string message)
    {
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward", "log");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "EndfieldLauncher.log"),
                $"[{DateTimeOffset.Now:O}] [{Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static string ValidateGamePath(string path)
    {
        path = Path.GetFullPath(path);
        if (!string.Equals(Path.GetFileName(path), "Endfield.exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            throw new FileNotFoundException("Endfield.exe was not found.", path);
        return path;
    }

    internal static void PrepareParent(string gameDirectory, string destination)
    {
        foreach (string name in new[] { "PlatformProcess.exe", "UnityCrashHandler64.exe" })
        {
            string source = Path.Combine(gameDirectory, name);
            if (!File.Exists(source)) continue;
            // Always copy, never hard-link or modify the installed game files.
            File.Copy(source, destination, true);
            if (IsTrustedParent(destination)) return;
        }
        throw new InvalidOperationException("No supported, valid Hypergryph-signed helper was found in the game directory. Repair the game files before retrying.");
    }

    internal static bool IsTrustedParent(string path)
    {
        if (!EndfieldLaunchSession.VerifySignature(path)) return false;
        try
        {
            // X509CertificateLoader reads certificates, not Authenticode-signed PE files.
#pragma warning disable SYSLIB0057
            using var certificate = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            string thumbprint = Convert.ToHexString(SHA1.HashData(certificate.GetRawCertData()));
            return thumbprint is "2B366F397E64A9494A553B70004E8CA1AAE02B9D" or "FF4AF264C36A27F0C805936708EF8EC67B396225";
        }
        catch (CryptographicException) { return false; }
    }
}
