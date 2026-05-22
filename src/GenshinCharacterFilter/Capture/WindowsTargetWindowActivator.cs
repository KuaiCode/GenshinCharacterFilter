using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GenshinCharacterFilter.Capture;

public sealed class WindowsTargetWindowActivator : ITargetWindowActivator
{
    private const int SwRestore = 9;
    private const uint KeyeventfKeyup = 0x0002;
    private const ushort VkMenu = 0x12;
    private const ushort VkTab = 0x09;

    private readonly TextWriter _log;

    public WindowsTargetWindowActivator(TextWriter? log = null)
    {
        _log = log ?? TextWriter.Null;
    }

    public async Task<TargetWindowActivationResult> TryActivateTargetWindowAsync(
        string processName,
        int delayMs,
        TargetWindowActivationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.UnknownError,
                "Target window activation is only supported on Windows.");
        }

        string normalizedProcessName = WindowCaptureOptions.NormalizeProcessName(processName);
        try
        {
            IntPtr windowHandle = FindTargetWindow(normalizedProcessName);
            TargetWindowActivationResult win32Result = await TryWin32ActivateAsync(
                windowHandle,
                normalizedProcessName,
                delayMs,
                options.VerifyForegroundProcess,
                cancellationToken);

            if (win32Result.Success || !options.EnableInputForegroundFallback)
            {
                return win32Result;
            }

            _log.WriteLine("Input foreground fallback enabled; attempting to switch target window to foreground.");
            if (!SendAltTabPulse())
            {
                return TargetWindowActivationResult.Failed(
                    TargetWindowActivationFailureReason.UnknownError,
                    $"Input foreground fallback failed before retrying activation. SendInput error: {Marshal.GetLastWin32Error()}.",
                    inputFallbackAttempted: true);
            }

            await Task.Delay(Math.Min(Math.Max(delayMs, 100), 500), cancellationToken);
            IntPtr inputWindowHandle = FindTargetWindow(normalizedProcessName);
            TargetWindowActivationResult inputResult = await TryWin32ActivateAsync(
                inputWindowHandle,
                normalizedProcessName,
                delayMs,
                options.VerifyForegroundProcess,
                cancellationToken);

            return inputResult.Success
                ? TargetWindowActivationResult.Succeeded(TargetWindowActivationMethod.InputFallback)
                : TargetWindowActivationResult.Failed(
                    inputResult.FailureReason,
                    inputResult.UserMessage,
                    inputFallbackAttempted: true);
        }
        catch (WindowCaptureException exception) when (exception.Message.Contains("No running process", StringComparison.OrdinalIgnoreCase))
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.TargetNotFound,
                exception.Message);
        }
        catch (WindowCaptureException exception) when (exception.Message.Contains("no main window", StringComparison.OrdinalIgnoreCase))
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.TargetNotFound,
                exception.Message);
        }
        catch (Exception exception) when (exception is WindowCaptureException or Win32Exception or InvalidOperationException)
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.UnknownError,
                $"Target window activation failed: {exception.Message}");
        }
    }

    private async Task<TargetWindowActivationResult> TryWin32ActivateAsync(
        IntPtr windowHandle,
        string processName,
        int delayMs,
        bool verifyForegroundProcess,
        CancellationToken cancellationToken)
    {
        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.TargetNotFound,
                $"No usable target window for process '{processName}' was found.");
        }

        if (IsIconic(windowHandle))
        {
            _log.WriteLine($"Target window for process '{processName}' is minimized; attempting to restore it.");
            ShowWindow(windowHandle, SwRestore);
        }

        BringWindowToTop(windowHandle);
        TryAttachThreadInputAndSetForeground(windowHandle);
        bool setForeground = SetForegroundWindow(windowHandle);

        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }

        if (IsIconic(windowHandle))
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.StillMinimized,
                $"Target window for process '{processName}' is still minimized after activation.");
        }

        if (!IsWindowVisible(windowHandle))
        {
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.ActivationDenied,
                $"Target window for process '{processName}' is not visible after activation.");
        }

        if (verifyForegroundProcess && GetForegroundWindow() != windowHandle)
        {
            string foregroundProcess = TryGetForegroundProcessName();
            return TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.ForegroundMismatch,
                $"Foreground window is '{foregroundProcess}', not '{processName}'.");
        }

        return setForeground || verifyForegroundProcess
            ? TargetWindowActivationResult.Succeeded(TargetWindowActivationMethod.Win32)
            : TargetWindowActivationResult.Failed(
                TargetWindowActivationFailureReason.ActivationDenied,
                $"Windows denied foreground activation for process '{processName}'.");
    }

    private static IntPtr FindTargetWindow(string normalizedProcessName)
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(normalizedProcessName);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            throw new WindowCaptureException($"Could not query processes named '{normalizedProcessName}': {exception.Message}", exception);
        }

        if (processes.Length == 0)
        {
            throw new WindowCaptureException($"No running process named '{normalizedProcessName}' was found.");
        }

        using CompositeProcessDisposer disposer = new(processes);
        Process? processWithWindow = processes.FirstOrDefault(HasMainWindow);
        if (processWithWindow is null)
        {
            throw new WindowCaptureException($"Process '{normalizedProcessName}' is running, but no main window was found.");
        }

        return processWithWindow.MainWindowHandle;
    }

    private static bool HasMainWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle != IntPtr.Zero;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    private static void TryAttachThreadInputAndSetForeground(IntPtr windowHandle)
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        uint currentThreadId = GetCurrentThreadId();
        uint targetThreadId = GetWindowThreadProcessId(windowHandle, out _);
        uint foregroundThreadId = foregroundWindow == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);

        if (targetThreadId == 0)
        {
            return;
        }

        bool attachedToTarget = targetThreadId != currentThreadId &&
            AttachThreadInput(currentThreadId, targetThreadId, true);
        bool attachedToForeground = foregroundThreadId != 0 &&
            foregroundThreadId != currentThreadId &&
            foregroundThreadId != targetThreadId &&
            AttachThreadInput(currentThreadId, foregroundThreadId, true);

        try
        {
            SetForegroundWindow(windowHandle);
            BringWindowToTop(windowHandle);
        }
        finally
        {
            if (attachedToForeground)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }

            if (attachedToTarget)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }

    private static bool SendAltTabPulse()
    {
        Input[] inputs =
        [
            Input.Keyboard(VkMenu, keyUp: false),
            Input.Keyboard(VkTab, keyUp: false),
            Input.Keyboard(VkTab, keyUp: true),
            Input.Keyboard(VkMenu, keyUp: true)
        ];

        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        return sent == inputs.Length;
    }

    private static string TryGetForegroundProcessName()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return "(none)";
        }

        _ = GetWindowThreadProcessId(foregroundWindow, out int processId);
        if (processId <= 0)
        {
            return "(unknown)";
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "(unknown)";
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint cInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;

        public static Input Keyboard(ushort virtualKey, bool keyUp)
        {
            return new Input
            {
                Type = 1,
                Union = new InputUnion
                {
                    KeyboardInput = new KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        ScanCode = 0,
                        Flags = keyUp ? KeyeventfKeyup : 0,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput KeyboardInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    private sealed class CompositeProcessDisposer : IDisposable
    {
        private readonly IReadOnlyList<Process> _processes;

        public CompositeProcessDisposer(IReadOnlyList<Process> processes)
        {
            _processes = processes;
        }

        public void Dispose()
        {
            foreach (Process process in _processes)
            {
                process.Dispose();
            }
        }
    }
}
