using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Captures a Windows target process main window using Win32 APIs.
/// </summary>
public sealed class WindowsGameWindowCapture : IGameWindowCapture, IGameWindowCaptureSessionFactory
{
    private const int Srccopy = 0x00CC0020;
    private const int Captureblt = 0x40000000;
    private const int DwmwaExtendedFrameBounds = 9;
    private const int SwRestore = 9;
    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);

    private readonly TextWriter _log;

    public WindowsGameWindowCapture(TextWriter? log = null)
    {
        _log = log ?? TextWriter.Null;
    }

    public async Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Window capture is only supported on Windows.");
        }

        EnsureDpiAwareness();
        options.Validate();
        string normalizedProcessName = WindowCaptureOptions.NormalizeProcessName(options.TargetProcessName);
        _log.WriteLine($"Looking for target window from process '{normalizedProcessName}'.");

        IntPtr windowHandle = FindTargetWindow(normalizedProcessName);
        await RestoreAndActivateTargetWindowAsync(windowHandle, normalizedProcessName, options.CaptureDelayMs, cancellationToken);

        return CaptureWindow(windowHandle, normalizedProcessName, options);
    }

    public async Task<string> CaptureForegroundWindowAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Window capture is only supported on Windows.");
        }

        EnsureDpiAwareness();
        options.Validate();
        string normalizedProcessName = WindowCaptureOptions.NormalizeProcessName(options.TargetProcessName);
        _log.WriteLine($"Capturing current foreground window; expected process '{normalizedProcessName}'.");

        IntPtr windowHandle = GetForegroundWindow();
        if (windowHandle == IntPtr.Zero)
        {
            throw new WindowCaptureException("No foreground window is currently available for manual calibration capture.");
        }

        string foregroundProcessName = GetWindowProcessName(windowHandle);
        WindowCaptureProcessValidator.ValidateForegroundProcess(normalizedProcessName, foregroundProcessName);
        _log.WriteLine($"Foreground window belongs to target process '{normalizedProcessName}'.");

        if (IsIconic(windowHandle))
        {
            throw WindowCaptureException.TargetWindowMinimizedCannotRestore(normalizedProcessName);
        }

        if (options.CaptureDelayMs > 0)
        {
            _log.WriteLine($"Foreground target window visible; waiting {options.CaptureDelayMs} ms before capture.");
            await Task.Delay(options.CaptureDelayMs, cancellationToken);
        }

        if (IsIconic(windowHandle))
        {
            throw WindowCaptureException.TargetWindowMinimizedCannotRestore(normalizedProcessName);
        }

        return CaptureWindow(windowHandle, normalizedProcessName, options);
    }

    public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureDpiAwareness();
        options.Validate();
        return new WindowsGameWindowCaptureSession(
            this,
            options,
            initialWindowHandle: default,
            allowReacquire: true,
            captureModePrefix: "process");
    }

    public IGameWindowCaptureSession CreateForegroundSession(WindowCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureDpiAwareness();
        options.Validate();
        string normalizedProcessName = WindowCaptureOptions.NormalizeProcessName(options.TargetProcessName);
        _log.WriteLine($"Creating live capture session from current foreground window; expected process '{normalizedProcessName}'.");

        IntPtr windowHandle = GetForegroundWindow();
        if (windowHandle == IntPtr.Zero)
        {
            throw new WindowCaptureException("No foreground window is currently available for manual detection startup.");
        }

        string foregroundProcessName = GetWindowProcessName(windowHandle);
        WindowCaptureProcessValidator.ValidateForegroundProcess(normalizedProcessName, foregroundProcessName);
        _log.WriteLine($"Foreground window belongs to target process '{normalizedProcessName}'.");

        if (IsIconic(windowHandle))
        {
            throw WindowCaptureException.TargetWindowMinimizedCannotRestore(normalizedProcessName);
        }

        return new WindowsGameWindowCaptureSession(
            this,
            options,
            windowHandle,
            allowReacquire: false,
            captureModePrefix: "foreground");
    }

    private string CaptureWindow(IntPtr windowHandle, string normalizedProcessName, WindowCaptureOptions options)
    {
        WindowRect windowRect = GetCaptureBounds(windowHandle, normalizedProcessName);
        int windowWidth = windowRect.Width;
        int windowHeight = windowRect.Height;
        if (windowWidth <= 0 || windowHeight <= 0)
        {
            throw new WindowCaptureException($"Target window for process '{normalizedProcessName}' has invalid size {windowWidth}x{windowHeight}.");
        }

        CaptureRegion region = options.CaptureRegion ?? new CaptureRegion(0, 0, windowWidth, windowHeight);
        try
        {
            region.ValidateWithin(windowWidth, windowHeight);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new WindowCaptureException($"Capture region {region} does not fit within window size {windowWidth}x{windowHeight}.", exception);
        }

        string outputPath = options.GetCaptureOutputPath();
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new WindowCaptureException("Capture output directory could not be resolved.");
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new WindowCaptureException($"Could not create capture output directory '{outputDirectory}': {exception.Message}", exception);
        }

        CaptureScreenRegion(windowRect.Left + region.X, windowRect.Top + region.Y, region.Width, region.Height, outputPath);
        if (options.SaveDebugImage)
        {
            _log.WriteLine($"Debug screenshot saved: {outputPath}");
        }

        return outputPath;
    }

    private static void EnsureDpiAwareness()
    {
        try
        {
            // Win32/DWM bounds are physical pixels; set DPI awareness before reading them.
            SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private async Task RestoreAndActivateTargetWindowAsync(
        IntPtr windowHandle,
        string processName,
        int captureDelayMs,
        CancellationToken cancellationToken)
    {
        if (IsIconic(windowHandle))
        {
            _log.WriteLine($"Target window for process '{processName}' is minimized; attempting to restore it.");
            ShowWindow(windowHandle, SwRestore);
        }

        bool foregroundActivated = SetForegroundWindow(windowHandle);
        if (!foregroundActivated)
        {
            _log.WriteLine("Warning: could not activate target window. v0.3 uses visible screen pixels; manually bring the target window to the front and keep it uncovered if the screenshot includes other windows.");
        }
        else
        {
            _log.WriteLine($"Target window activated; waiting {captureDelayMs} ms before capture.");
        }

        if (captureDelayMs > 0)
        {
            await Task.Delay(captureDelayMs, cancellationToken);
        }

        if (IsIconic(windowHandle))
        {
            throw WindowCaptureException.TargetWindowMinimizedCannotRestore(processName);
        }
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

    private static bool IsWindowHandleValid(IntPtr windowHandle)
    {
        return windowHandle != IntPtr.Zero && IsWindow(windowHandle);
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

    private static string GetWindowProcessName(IntPtr windowHandle)
    {
        _ = GetWindowThreadProcessId(windowHandle, out int processId);
        if (processId <= 0)
        {
            throw new WindowCaptureException("Could not determine the process for the current foreground window.");
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception)
        {
            throw new WindowCaptureException($"Could not inspect foreground window process {processId}: {exception.Message}", exception);
        }
    }

    private static WindowRect GetCaptureBounds(IntPtr windowHandle, string processName)
    {
        if (TryGetExtendedFrameBounds(windowHandle, out WindowRect bounds))
        {
            return bounds;
        }

        return GetWindowRectOrThrow(windowHandle, processName);
    }

    private static bool TryGetExtendedFrameBounds(IntPtr windowHandle, out WindowRect bounds)
    {
        int result = DwmGetWindowAttribute(
            windowHandle,
            DwmwaExtendedFrameBounds,
            out bounds,
            Marshal.SizeOf<WindowRect>());

        return result == 0 && bounds.Width > 0 && bounds.Height > 0;
    }

    private static WindowRect GetWindowRectOrThrow(IntPtr windowHandle, string processName)
    {
        if (!GetWindowRect(windowHandle, out WindowRect rect))
        {
            throw new WindowCaptureException($"Could not read target window bounds for process '{processName}'.");
        }

        return rect;
    }

    private static void CaptureScreenRegion(int screenX, int screenY, int width, int height, string outputPath)
    {
        IntPtr screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            throw new WindowCaptureException("Could not acquire screen device context.");
        }

        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmapHandle = IntPtr.Zero;
        IntPtr oldObject = IntPtr.Zero;

        try
        {
            memoryDc = CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create compatible device context.");
            }

            bitmapHandle = CreateCompatibleBitmap(screenDc, width, height);
            if (bitmapHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create compatible bitmap.");
            }

            oldObject = SelectObject(memoryDc, bitmapHandle);
            if (oldObject == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not select bitmap into device context.");
            }

            if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, screenX, screenY, Srccopy | Captureblt))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Window capture BitBlt failed.");
            }

            using Image image = Image.FromHbitmap(bitmapHandle);
            image.Save(outputPath, ImageFormat.Png);
        }
        catch (Exception exception) when (exception is Win32Exception or ExternalException or IOException or UnauthorizedAccessException)
        {
            throw new WindowCaptureException($"Failed to save debug screenshot: {exception.Message}", exception);
        }
        finally
        {
            if (oldObject != IntPtr.Zero && memoryDc != IntPtr.Zero)
            {
                SelectObject(memoryDc, oldObject);
            }

            if (bitmapHandle != IntPtr.Zero)
            {
                DeleteObject(bitmapHandle);
            }

            if (memoryDc != IntPtr.Zero)
            {
                DeleteDC(memoryDc);
            }

            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out WindowRect lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out WindowRect pvAttribute, int cbAttribute);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;

        public readonly int Height => Bottom - Top;
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

    private sealed class WindowsGameWindowCaptureSession : IGameWindowCaptureSession, IGameWindowCaptureSessionMetadata
    {
        private readonly WindowsGameWindowCapture _owner;
        private readonly WindowCaptureOptions _options;
        private readonly string _normalizedProcessName;
        private readonly bool _allowReacquire;
        private readonly string _captureModePrefix;
        private IntPtr _windowHandle;
        private bool _disposed;

        public WindowsGameWindowCaptureSession(
            WindowsGameWindowCapture owner,
            WindowCaptureOptions options,
            IntPtr initialWindowHandle,
            bool allowReacquire,
            string captureModePrefix)
        {
            _owner = owner;
            _options = new WindowCaptureOptions
            {
                TargetProcessName = options.TargetProcessName,
                CaptureRegion = options.CaptureRegion,
                OutputDirectory = options.OutputDirectory,
                OutputFileName = options.OutputFileName,
                CaptureDelayMs = options.CaptureDelayMs,
                SaveDebugImage = options.SaveDebugImage
            };
            _normalizedProcessName = WindowCaptureOptions.NormalizeProcessName(_options.TargetProcessName);
            _allowReacquire = allowReacquire;
            _captureModePrefix = string.IsNullOrWhiteSpace(captureModePrefix) ? "process" : captureModePrefix;
            _windowHandle = initialWindowHandle;
            _owner._log.WriteLine(initialWindowHandle == IntPtr.Zero
                ? $"Live capture session initialized for process: {_normalizedProcessName}"
                : $"Live capture session initialized from foreground window for process: {_normalizedProcessName}");
        }

        public string CaptureModePrefix => _captureModePrefix;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsWindowHandleValid(_windowHandle))
            {
                await ReacquireAsync("initializing live capture session", cancellationToken);
            }
        }

        public async Task<string> CaptureAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            return await CaptureCoreAsync(null, cancellationToken);
        }

        public async Task<WindowCaptureFrameInfo> GetFrameInfoAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            await EnsureReadyAsync(cancellationToken, logReuse: false);
            WindowRect bounds = GetCaptureBounds(_windowHandle, _normalizedProcessName);
            return new WindowCaptureFrameInfo(bounds.Width, bounds.Height);
        }

        public async Task<string> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            return await CaptureCoreAsync(region, cancellationToken);
        }

        private async Task<string> CaptureCoreAsync(CaptureRegion? region, CancellationToken cancellationToken)
        {
            await EnsureReadyAsync(cancellationToken, logReuse: true);

            try
            {
                return _owner.CaptureWindow(_windowHandle, _normalizedProcessName, CreateCaptureOptions(region));
            }
            catch (WindowCaptureException exception)
            {
                if (!_allowReacquire)
                {
                    throw new WindowCaptureException(
                        $"Foreground capture session for process '{_normalizedProcessName}' failed and will not reacquire by process name: {exception.Message}",
                        exception);
                }

                await ReacquireAsync(exception.Message, cancellationToken);
                return _owner.CaptureWindow(_windowHandle, _normalizedProcessName, CreateCaptureOptions(region));
            }
        }

        private async Task EnsureReadyAsync(CancellationToken cancellationToken, bool logReuse)
        {
            if (!IsWindowHandleValid(_windowHandle))
            {
                if (!_allowReacquire)
                {
                    throw new WindowCaptureException(
                        $"Foreground capture session for process '{_normalizedProcessName}' is no longer valid. Keep the target window visible and restart detection.");
                }

                await ReacquireAsync("no cached target window is available", cancellationToken);
            }
            else if (IsIconic(_windowHandle))
            {
                if (!_allowReacquire)
                {
                    throw new WindowCaptureException(
                        $"Foreground capture session for process '{_normalizedProcessName}' is minimized. Keep the target visible; this visible-pixel capture path will not restore or reacquire by process name.");
                }

                await ReacquireAsync("cached target window is minimized", cancellationToken);
            }
            else if (logReuse)
            {
                _owner._log.WriteLine("Reusing target window handle.");
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private WindowCaptureOptions CreateCaptureOptions(CaptureRegion? region)
        {
            return new WindowCaptureOptions
            {
                TargetProcessName = _options.TargetProcessName,
                CaptureRegion = region ?? _options.CaptureRegion,
                OutputDirectory = _options.OutputDirectory,
                OutputFileName = _options.OutputFileName,
                CaptureDelayMs = _options.CaptureDelayMs,
                SaveDebugImage = _options.SaveDebugImage
            };
        }

        private async Task ReacquireAsync(string reason, CancellationToken cancellationToken)
        {
            _owner._log.WriteLine($"Reacquiring target window because {reason}.");
            _owner._log.WriteLine($"Looking for target window from process '{_normalizedProcessName}'.");
            _windowHandle = FindTargetWindow(_normalizedProcessName);
            await _owner.RestoreAndActivateTargetWindowAsync(
                _windowHandle,
                _normalizedProcessName,
                _options.CaptureDelayMs,
                cancellationToken);
        }
    }
}
