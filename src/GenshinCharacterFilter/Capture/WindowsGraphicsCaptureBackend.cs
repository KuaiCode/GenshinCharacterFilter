using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Windows.Graphics.Capture backend for HWND-based window frame acquisition.
/// </summary>
public sealed class WindowsGraphicsCaptureBackend : IGameCaptureBackend
{
    private const int DefaultFramePoolBufferCount = 1;
    private const int D3d11SdkVersion = 7;
    private const int D3d11CreateDeviceBgraSupport = 0x20;
    private const int D3dDriverTypeHardware = 1;
    private const int D3dDriverTypeWarp = 5;
    private const int SwRestore = 9;

    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IdxgiDeviceGuid = new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
    private static readonly int[] D3dFeatureLevels =
    [
        0xb100,
        0xb000,
        0xa100,
        0xa000,
        0x9300,
        0x9200,
        0x9100
    ];

    private readonly int _captureTimeoutMs;
    private readonly TextWriter _log;

    public WindowsGraphicsCaptureBackend(int captureTimeoutMs = CaptureBackendOptions.DefaultCaptureTimeoutMs, TextWriter? log = null)
    {
        if (captureTimeoutMs is < CaptureBackendOptions.MinCaptureTimeoutMs or > CaptureBackendOptions.MaxCaptureTimeoutMs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(captureTimeoutMs),
                $"Capture timeout must be between {CaptureBackendOptions.MinCaptureTimeoutMs} and {CaptureBackendOptions.MaxCaptureTimeoutMs} ms.");
        }

        _captureTimeoutMs = captureTimeoutMs;
        _log = log ?? TextWriter.Null;
    }

    public CaptureBackend Backend => CaptureBackend.WindowsGraphicsCapture;

    public string StatusLabel => CheckAvailability().Available ? "Ready" : "Unavailable";

    public CaptureBackendAvailability CheckAvailability()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.UnsupportedOS,
                "Windows.Graphics.Capture is only available on Windows.");
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
        {
            return CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.UnsupportedOS,
                "Windows.Graphics.Capture requires Windows 10 version 1903 or newer.");
        }

        try
        {
            return GraphicsCaptureSession.IsSupported()
                ? CaptureBackendAvailability.Ready("Windows.Graphics.Capture is supported on this system.")
                : CaptureBackendAvailability.Unavailable(
                    CaptureBackendFailureReason.BackendUnavailable,
                    "Windows.Graphics.Capture is not supported or is disabled on this system.");
        }
        catch (Exception exception) when (exception is TypeLoadException or EntryPointNotFoundException or COMException)
        {
            return CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.ApiUnavailable,
                $"Windows.Graphics.Capture API is unavailable: {exception.Message}");
        }
    }

    public async Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureAvailable();

        using WindowsGraphicsCaptureSession session = new(this, options, _log);
        await session.InitializeAsync(cancellationToken);
        string outputPath;
        if (options.CaptureRegion is not null)
        {
            outputPath = await session.CaptureRegionAsync(options.CaptureRegion.Value, cancellationToken);
        }
        else
        {
            outputPath = await session.CaptureAsync(cancellationToken);
        }

        _log.WriteLine($"Screenshot captured via WindowsGraphicsCapture: {outputPath}");
        return outputPath;
    }

    public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
    {
        EnsureAvailable();
        return new WindowsGraphicsCaptureSession(this, options, _log);
    }

    private void EnsureAvailable()
    {
        CaptureBackendAvailability availability = CheckAvailability();
        if (availability.Available)
        {
            return;
        }

        _log.WriteLine($"WindowsGraphicsCapture unavailable: {availability.Message}");
        throw new CaptureBackendException(
            CaptureBackend.WindowsGraphicsCapture,
            availability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
            availability.Message);
    }

    private static TargetWindow ResolveTargetWindow(WindowCaptureOptions options)
    {
        string processName = WindowCaptureOptions.NormalizeProcessName(options.TargetProcessName);
        using Process currentProcess = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (process.Id == currentProcess.Id || process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                return new TargetWindow(process.MainWindowHandle, processName);
            }
            finally
            {
                process.Dispose();
            }
        }

        throw new CaptureBackendException(
            CaptureBackend.WindowsGraphicsCapture,
            CaptureBackendFailureReason.TargetWindowInvalid,
            $"No visible main window was found for process '{processName}'.");
    }

    private async Task<string> CaptureWindowAsync(
        GraphicsCaptureItem item,
        WindowCaptureOptions options,
        CaptureRegion? region,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        using Direct3D11CaptureFrame frame = await AcquireFrameAsync(item, cancellationToken);
        SizeInt32 contentSize = frame.ContentSize;
        string capturePath = options.GetCaptureOutputPath();
        Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);

        if (region is null)
        {
            await SaveFrameSurfaceAsync(frame.Surface, capturePath, cancellationToken);
        }
        else
        {
            string fullFramePath = Path.Combine(
                WindowCaptureOptions.GetTempCaptureDirectory(),
                $"wgc-full-{Guid.NewGuid():N}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(fullFramePath)!);
            await SaveFrameSurfaceAsync(frame.Surface, fullFramePath, cancellationToken);
            try
            {
                SaveCroppedPng(fullFramePath, capturePath, region.Value);
            }
            finally
            {
                TryDeleteFile(fullFramePath);
            }
        }

        stopwatch.Stop();
        _log.WriteLine($"WGC frame acquired: {contentSize.Width}x{contentSize.Height}, elapsed: {stopwatch.ElapsedMilliseconds} ms.");
        if (options.SaveDebugImage)
        {
            _log.WriteLine($"Debug screenshot saved: {capturePath}");
        }
        else
        {
            _log.WriteLine($"WindowsGraphicsCapture temporary frame saved: {capturePath}");
        }

        return capturePath;
    }

    private async Task<Direct3D11CaptureFrame> AcquireFrameAsync(
        GraphicsCaptureItem item,
        CancellationToken cancellationToken)
    {
        IDirect3DDevice device = CreateDirect3DDevice();
        Direct3D11CaptureFramePool? framePool = null;
        GraphicsCaptureSession? session = null;
        Direct3D11CaptureFrame? frame = null;
        TaskCompletionSource<Direct3D11CaptureFrame> frameCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TypedEventHandler<Direct3D11CaptureFramePool, object>? frameArrivedHandler = null;
        using CancellationTokenSource timeoutSource = new(_captureTimeoutMs);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        CancellationTokenRegistration registration = linkedSource.Token.Register(() =>
        {
            CaptureBackendFailureReason reason = timeoutSource.IsCancellationRequested
                ? CaptureBackendFailureReason.FrameTimeout
                : CaptureBackendFailureReason.UnknownError;
            frameCompletion.TrySetException(new CaptureBackendException(
                CaptureBackend.WindowsGraphicsCapture,
                reason,
                timeoutSource.IsCancellationRequested
                    ? $"Timed out waiting {_captureTimeoutMs} ms for a Windows.Graphics.Capture frame."
                    : "Windows.Graphics.Capture frame acquisition was cancelled."));
        });

        try
        {
            framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DefaultFramePoolBufferCount,
                item.Size);
            session = framePool.CreateCaptureSession(item);
            frameArrivedHandler = (_, _) =>
            {
                try
                {
                    frame = framePool.TryGetNextFrame();
                    if (frame is null)
                    {
                        return;
                    }

                    frameCompletion.TrySetResult(frame);
                }
                catch (Exception exception)
                {
                    frameCompletion.TrySetException(WrapBackendException(
                        exception,
                        CaptureBackendFailureReason.UnknownError,
                        "Windows.Graphics.Capture failed while reading the next frame."));
                }
            };
            framePool.FrameArrived += frameArrivedHandler;

            session.StartCapture();
            return await frameCompletion.Task;
        }
        catch (CaptureBackendException)
        {
            frame?.Dispose();
            throw;
        }
        catch (COMException exception)
        {
            frame?.Dispose();
            throw WrapBackendException(
                exception,
                CaptureBackendFailureReason.AccessDenied,
                "Windows.Graphics.Capture failed to start. The target may deny capture or the capture API may be unavailable.");
        }
        catch (InvalidOperationException exception)
        {
            frame?.Dispose();
            throw WrapBackendException(
                exception,
                CaptureBackendFailureReason.UnknownError,
                "Windows.Graphics.Capture failed to start for the target window.");
        }
        finally
        {
            registration.Dispose();
            if (framePool is not null && frameArrivedHandler is not null)
            {
                try
                {
                    framePool.FrameArrived -= frameArrivedHandler;
                }
                catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
                {
                }
            }

            session?.Dispose();
            framePool?.Dispose();
        }
    }

    private static async Task SaveFrameSurfaceAsync(
        IDirect3DSurface surface,
        string outputPath,
        CancellationToken cancellationToken)
    {
        using SoftwareBitmap softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
            surface,
            BitmapAlphaMode.Premultiplied).AsTask(cancellationToken);
        using SoftwareBitmap converted = SoftwareBitmap.Convert(
            softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);
        using InMemoryRandomAccessStream stream = new();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).AsTask(cancellationToken);
        encoder.SetSoftwareBitmap(converted);
        await encoder.FlushAsync().AsTask(cancellationToken);

        stream.Seek(0);
        await using Stream input = stream.AsStreamForRead();
        await using FileStream output = File.Create(outputPath);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static void SaveCroppedPng(string sourcePath, string outputPath, CaptureRegion region)
    {
        using Image source = Image.FromFile(sourcePath);
        CaptureRegion validated = ValidateRegion(region, source.Width, source.Height);
        using Bitmap cropped = new(validated.Width, validated.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(cropped))
        {
            graphics.DrawImage(
                source,
                new Rectangle(0, 0, validated.Width, validated.Height),
                new Rectangle(validated.X, validated.Y, validated.Width, validated.Height),
                GraphicsUnit.Pixel);
        }

        cropped.Save(outputPath, ImageFormat.Png);
    }

    private static CaptureRegion ValidateRegion(CaptureRegion region, int imageWidth, int imageHeight)
    {
        if (region.X < 0 ||
            region.Y < 0 ||
            region.Width <= 0 ||
            region.Height <= 0 ||
            region.X + region.Width > imageWidth ||
            region.Y + region.Height > imageHeight)
        {
            throw new CaptureBackendException(
                CaptureBackend.WindowsGraphicsCapture,
                CaptureBackendFailureReason.TargetWindowInvalid,
                $"Requested OCR capture region {region} is outside WGC frame bounds {imageWidth}x{imageHeight}.");
        }

        return region;
    }

    private static GraphicsCaptureItem CreateCaptureItem(IntPtr windowHandle, string processName)
    {
        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
        {
            throw new CaptureBackendException(
                CaptureBackend.WindowsGraphicsCapture,
                CaptureBackendFailureReason.TargetWindowInvalid,
                $"Target window for process '{processName}' is no longer valid.");
        }

        if (IsIconic(windowHandle))
        {
            throw new CaptureBackendException(
                CaptureBackend.WindowsGraphicsCapture,
                CaptureBackendFailureReason.TargetMinimized,
                $"Target window for process '{processName}' is minimized. Windows.Graphics.Capture cannot acquire a frame from this minimized HWND in this mode.");
        }

        IntPtr hstring = IntPtr.Zero;
        IntPtr activationFactory = IntPtr.Zero;
        IntPtr itemPtr = IntPtr.Zero;
        try
        {
            int hr = WindowsCreateString(
                "Windows.Graphics.Capture.GraphicsCaptureItem",
                (uint)"Windows.Graphics.Capture.GraphicsCaptureItem".Length,
                out hstring);
            ThrowBackendHr(hr, CaptureBackendFailureReason.ApiUnavailable, "Failed to create WinRT class name for GraphicsCaptureItem.");

            hr = RoGetActivationFactory(hstring, typeof(IGraphicsCaptureItemInterop).GUID, out activationFactory);
            ThrowBackendHr(hr, CaptureBackendFailureReason.ApiUnavailable, "Failed to get IGraphicsCaptureItemInterop activation factory.");

            IGraphicsCaptureItemInterop interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(activationFactory);
            Guid itemGuid = GraphicsCaptureItemGuid;
            hr = interop.CreateForWindow(windowHandle, ref itemGuid, out itemPtr);
            ThrowBackendHr(hr, CaptureBackendFailureReason.CreateCaptureItemFailed, "Failed to create GraphicsCaptureItem for the target HWND.");

            GraphicsCaptureItem item = GraphicsCaptureItem.FromAbi(itemPtr);
            itemPtr = IntPtr.Zero;
            return item;
        }
        catch (COMException exception)
        {
            throw WrapBackendException(
                exception,
                CaptureBackendFailureReason.CreateCaptureItemFailed,
                $"Failed to create GraphicsCaptureItem for process '{processName}'.");
        }
        finally
        {
            if (itemPtr != IntPtr.Zero)
            {
                Marshal.Release(itemPtr);
            }

            if (activationFactory != IntPtr.Zero)
            {
                Marshal.Release(activationFactory);
            }

            if (hstring != IntPtr.Zero)
            {
                WindowsDeleteString(hstring);
            }
        }
    }

    private static IDirect3DDevice CreateDirect3DDevice()
    {
        IntPtr d3dDevice = IntPtr.Zero;
        IntPtr d3dContext = IntPtr.Zero;
        IntPtr dxgiDevice = IntPtr.Zero;
        IntPtr direct3DDevice = IntPtr.Zero;
        try
        {
            int hr = D3D11CreateDevice(
                IntPtr.Zero,
                D3dDriverTypeHardware,
                IntPtr.Zero,
                D3d11CreateDeviceBgraSupport,
                D3dFeatureLevels,
                D3dFeatureLevels.Length,
                D3d11SdkVersion,
                out d3dDevice,
                out _,
                out d3dContext);
            if (hr < 0)
            {
                hr = D3D11CreateDevice(
                    IntPtr.Zero,
                    D3dDriverTypeWarp,
                    IntPtr.Zero,
                    D3d11CreateDeviceBgraSupport,
                    D3dFeatureLevels,
                    D3dFeatureLevels.Length,
                    D3d11SdkVersion,
                    out d3dDevice,
                    out _,
                    out d3dContext);
            }

            ThrowBackendHr(hr, CaptureBackendFailureReason.Direct3DDeviceCreationFailed, "Failed to create a Direct3D11 device for Windows.Graphics.Capture.");

            Guid dxgiGuid = IdxgiDeviceGuid;
            hr = Marshal.QueryInterface(d3dDevice, ref dxgiGuid, out dxgiDevice);
            ThrowBackendHr(hr, CaptureBackendFailureReason.Direct3DDeviceCreationFailed, "Failed to query IDXGIDevice from the Direct3D11 device.");

            hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out direct3DDevice);
            ThrowBackendHr(hr, CaptureBackendFailureReason.Direct3DDeviceCreationFailed, "Failed to create a WinRT Direct3D device.");

            IDirect3DDevice device = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(direct3DDevice);
            direct3DDevice = IntPtr.Zero;
            return device;
        }
        catch (COMException exception)
        {
            throw WrapBackendException(
                exception,
                CaptureBackendFailureReason.Direct3DDeviceCreationFailed,
                "Windows.Graphics.Capture Direct3D device creation failed.");
        }
        finally
        {
            if (direct3DDevice != IntPtr.Zero)
            {
                Marshal.Release(direct3DDevice);
            }

            if (dxgiDevice != IntPtr.Zero)
            {
                Marshal.Release(dxgiDevice);
            }

            if (d3dContext != IntPtr.Zero)
            {
                Marshal.Release(d3dContext);
            }

            if (d3dDevice != IntPtr.Zero)
            {
                Marshal.Release(d3dDevice);
            }
        }
    }

    private static void ThrowBackendHr(int hr, CaptureBackendFailureReason reason, string message)
    {
        if (hr >= 0)
        {
            return;
        }

        Exception exception = Marshal.GetExceptionForHR(hr) ?? new Win32Exception(hr);
        throw new CaptureBackendException(
            CaptureBackend.WindowsGraphicsCapture,
            reason,
            $"{message} HRESULT=0x{hr:X8}. {exception.Message}",
            exception);
    }

    private static CaptureBackendException WrapBackendException(
        Exception exception,
        CaptureBackendFailureReason reason,
        string message)
    {
        if (exception is CaptureBackendException backendException)
        {
            return backendException;
        }

        return new CaptureBackendException(
            CaptureBackend.WindowsGraphicsCapture,
            reason,
            $"{message} {exception.Message}",
            exception);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int RoGetActivationFactory(IntPtr activatableClassId, [In] Guid iid, out IntPtr factory);

    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        uint length,
        out IntPtr hstring);

    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(
        IntPtr adapter,
        int driverType,
        IntPtr software,
        uint flags,
        [In] int[]? featureLevels,
        int featureLevelsCount,
        uint sdkVersion,
        out IntPtr device,
        out int featureLevel,
        out IntPtr immediateContext);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        [PreserveSig]
        int CreateForWindow(IntPtr window, ref Guid iid, out IntPtr result);
    }

    private sealed record TargetWindow(IntPtr Handle, string ProcessName);

    private sealed class WindowsGraphicsCaptureSession : IGameWindowCaptureSession, IGameWindowCaptureSessionMetadata
    {
        private readonly WindowsGraphicsCaptureBackend _backend;
        private readonly WindowCaptureOptions _options;
        private readonly TextWriter _log;
        private GraphicsCaptureItem? _item;

        public WindowsGraphicsCaptureSession(
            WindowsGraphicsCaptureBackend backend,
            WindowCaptureOptions options,
            TextWriter log)
        {
            _backend = backend;
            _options = options;
            _log = log;
        }

        public string CaptureModePrefix => "wgc-window";

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _options.Validate();
            cancellationToken.ThrowIfCancellationRequested();

            string processName = WindowCaptureOptions.NormalizeProcessName(_options.TargetProcessName);
            _log.WriteLine($"Initializing WindowsGraphicsCapture session for process: {processName}");
            TargetWindow targetWindow = ResolveTargetWindow(_options);
            if (IsIconic(targetWindow.Handle))
            {
                _log.WriteLine($"Target window for process '{processName}' is minimized; attempting to restore it before WGC capture.");
                ShowWindow(targetWindow.Handle, SwRestore);
                if (_options.CaptureDelayMs > 0)
                {
                    await Task.Delay(_options.CaptureDelayMs, cancellationToken);
                }
            }

            _log.WriteLine("Creating GraphicsCaptureItem for target window.");
            _item = CreateCaptureItem(targetWindow.Handle, targetWindow.ProcessName);
            _log.WriteLine($"WindowsGraphicsCapture session initialized for process: {processName}");
        }

        public async Task<string> CaptureAsync(CancellationToken cancellationToken)
        {
            GraphicsCaptureItem item = EnsureInitialized();
            return await _backend.CaptureWindowAsync(item, _options, region: null, cancellationToken);
        }

        public Task<WindowCaptureFrameInfo> GetFrameInfoAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GraphicsCaptureItem item = EnsureInitialized();
            return Task.FromResult(new WindowCaptureFrameInfo(item.Size.Width, item.Size.Height));
        }

        public async Task<string> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken)
        {
            GraphicsCaptureItem item = EnsureInitialized();
            return await _backend.CaptureWindowAsync(item, _options, region, cancellationToken);
        }

        public void Dispose()
        {
            _item = null;
        }

        private GraphicsCaptureItem EnsureInitialized()
        {
            if (_item is null)
            {
                throw new CaptureBackendException(
                    CaptureBackend.WindowsGraphicsCapture,
                    CaptureBackendFailureReason.TargetWindowInvalid,
                    "WindowsGraphicsCapture session has not been initialized.");
            }

            return _item;
        }
    }
}
