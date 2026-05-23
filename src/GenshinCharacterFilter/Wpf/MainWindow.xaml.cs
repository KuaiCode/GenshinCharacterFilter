using GenshinCharacterFilter.Gui;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfControl = System.Windows.Controls.Control;

namespace GenshinCharacterFilter.Wpf;

/// <summary>
/// Modern WPF shell over the existing GUI command service.
/// </summary>
public partial class MainWindow : Window
{
    private readonly GuiCommandService _commandService = new();
    private readonly AppSettingsLoader _settingsLoader = new();
    private readonly GuiStateController _stateController = new();
    private readonly GuiRuntimeStatus _runtimeStatus = new();
    private readonly WpfAppTheme _theme;
    private static readonly TimeSpan ManualForegroundRetryDelay = TimeSpan.FromSeconds(8);
    private CancellationTokenSource? _operationCancellation;
    private Task? _operationTask;
    private bool _closeAfterOperationStops;
    private bool _syncingGuardedRealAudioCheckBoxes;
    private DetectionRunContext? _resumeContext;

    private enum DetectionOperationKind
    {
        DryRun,
        SimulatedAudio,
        GuardedRealAudio
    }

    private readonly record struct GuiDetectionTuningInput(
        bool RunUntilStop,
        string LoopCount,
        string LoopIntervalMs,
        string CaptureDelayMs,
        string MatchThreshold,
        string MissThreshold,
        bool SaveDebugImages,
        bool SaveOcrFailureSamples,
        bool EnableInputForegroundFallback,
        CaptureBackend CaptureBackend,
        bool AllowCaptureBackendFallback);

    private readonly record struct DetectionLaunchInput(
        string? ConfigPath,
        string? OcrInputPath,
        bool UseFixedImageForDetection,
        OcrEngine OcrEngine,
        GuiDetectionTuningOptions TuningOptions);

    private readonly record struct DetectionRunContext(
        DetectionOperationKind OperationKind,
        DetectionLaunchInput Launch);

    public MainWindow(string? initialConfigPath, WpfAppTheme theme)
    {
        _theme = theme;
        InitializeComponent();
        WpfWindowBackdrop.TryApply(this, theme);

        ConfigPathTextBox.Text = string.IsNullOrWhiteSpace(initialConfigPath)
            ? GuiCommandService.GetDefaultConfigPath()
            : initialConfigPath;
        OcrEngineComboBox.ItemsSource = Enum.GetValues<OcrEngine>().Select(engine => engine.ToString()).ToArray();
        CaptureBackendComboBox.ItemsSource = Enum.GetValues<CaptureBackend>().Select(backend => backend.ToString()).ToArray();
        OcrInputPathTextBox.Text = GuiCommandService.GetDefaultOcrInputPath();
        UseFixedImageForDetectionCheckBox.IsChecked = false;
        SaveDebugImagesCheckBox.IsChecked = false;
        SaveOcrFailureSamplesCheckBox.IsChecked = false;
        RunUntilStopCheckBox.IsChecked = true;
        LoopCountTextBox.Text = string.Empty;
        LoopIntervalTextBox.Text = GuiDetectionTuningOptions.DefaultLoopIntervalMs.ToString();
        CaptureDelayTextBox.Text = GuiDetectionTuningOptions.DefaultCaptureDelayMs.ToString();
        MatchThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMatchThreshold.ToString();
        MissThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMissThreshold.ToString();
        RefreshOcrEngineSelectionFromConfig();
        RefreshCaptureBackendSelectionFromConfig();
        bool inputForegroundFallbackEnabled = TryLoadSettings()?.Detection.EnableInputForegroundFallback ?? false;
        EnableInputForegroundFallbackCheckBox.IsChecked = inputForegroundFallbackEnabled;

        Closing += OnWindowClosing;
        ConfigPathTextBox.TextChanged += (_, _) => RefreshStatus();
        UseFixedImageForDetectionCheckBox.Checked += (_, _) => RefreshStatus();
        UseFixedImageForDetectionCheckBox.Unchecked += (_, _) => RefreshStatus();
        EnableGuardedRealAudioCheckBox.Checked += (_, _) => RefreshStatus();
        EnableGuardedRealAudioCheckBox.Unchecked += (_, _) => RefreshStatus();
        DockEnableGuardedRealAudioCheckBox.Checked += (_, _) => SyncGuardedRealAudioEnablement(fromDock: true);
        DockEnableGuardedRealAudioCheckBox.Unchecked += (_, _) => SyncGuardedRealAudioEnablement(fromDock: true);
        EnableGuardedRealAudioCheckBox.Checked += (_, _) => SyncGuardedRealAudioEnablement(fromDock: false);
        EnableGuardedRealAudioCheckBox.Unchecked += (_, _) => SyncGuardedRealAudioEnablement(fromDock: false);
        EnableInputForegroundFallbackCheckBox.Checked += (_, _) => RefreshStatus();
        EnableInputForegroundFallbackCheckBox.Unchecked += (_, _) => RefreshStatus();
        AllowCaptureBackendFallbackCheckBox.Checked += CaptureFallbackChanged;
        AllowCaptureBackendFallbackCheckBox.Unchecked += CaptureFallbackChanged;
        RunUntilStopCheckBox.Checked += (_, _) => UpdateLoopCountInputState();
        RunUntilStopCheckBox.Unchecked += (_, _) => UpdateLoopCountInputState();

        ShowPage(OverviewPage);
        ApplyUiState(_stateController.Current);
        ApplyRuntimeSnapshot(_runtimeStatus.Snapshot);
        RefreshStatus();
        AppendLogLine($"WPF GUI shell started with {_theme.ToString().ToLowerInvariant()} theme.");
    }

    private void ShowOverviewPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(OverviewPage);

    private void ShowConfigPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(ConfigPage);

    private void ShowOcrPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(OcrPage);

    private void ShowDetectionPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(DetectionPage);

    private void ShowAudioPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(AudioPage);

    private void ShowLogsPage(object? sender, RoutedEventArgs eventArgs) => ShowPage(LogsPage);

    private void ShowPage(UIElement page)
    {
        OverviewPage.Visibility = Visibility.Collapsed;
        ConfigPage.Visibility = Visibility.Collapsed;
        OcrPage.Visibility = Visibility.Collapsed;
        DetectionPage.Visibility = Visibility.Collapsed;
        AudioPage.Visibility = Visibility.Collapsed;
        LogsPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
        if (page == LogsPage)
        {
            LogTextBox.Focus();
        }
    }

    private void BrowseConfig(object? sender, RoutedEventArgs eventArgs)
    {
        RunBrowseAction("Config browse", () =>
        {
            OpenFileDialog dialog = new()
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = ConfigPathTextBox.Text
            };

            if (dialog.ShowDialog(this) == true)
            {
                ConfigPathTextBox.Text = dialog.FileName;
                RefreshOcrEngineSelectionFromConfig();
            }
        });
    }

    private void BrowseOcrInput(object? sender, RoutedEventArgs eventArgs)
    {
        RunBrowseAction("OCR input browse", () =>
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                FileName = OcrInputPathTextBox.Text
            };

            if (dialog.ShowDialog(this) == true)
            {
                OcrInputPathTextBox.Text = dialog.FileName;
            }
        });
    }

    private async void ValidateConfig(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(ValidateConfigAsync, cancellable: false);
    }

    private async void PrintEffectiveConfig(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(PrintEffectiveConfigAsync, cancellable: false);
    }

    private async void CalibrateOcrRegion(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(CalibrateOcrRegionAsync, cancellable: false);
    }

    private async void TestOcrOnce(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(TestOcrOnceAsync, cancellable: true);
    }

    private async void WarmUpOcrBackend(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(WarmUpOcrBackendAsync, cancellable: true);
    }

    private async void StartDryRunDetection(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(StartDryRunDetectionAsync, cancellable: true);
    }

    private async void StartSimulatedDetectionAudio(object? sender, RoutedEventArgs eventArgs)
    {
        await RunOperationAsync(StartSimulatedDetectionAudioAsync, cancellable: true);
    }

    private async void StartGuardedRealAudio(object? sender, RoutedEventArgs eventArgs)
    {
        await StartGuardedRealAudioWithConfirmationAsync();
    }

    private void OcrEngineSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        RefreshOcrBackendStatus();
        RefreshStatus();
    }

    private void StopOperation(object? sender, RoutedEventArgs eventArgs)
    {
        CancelCurrentOperation();
    }

    private async void ResumeReconnect(object? sender, RoutedEventArgs eventArgs)
    {
        await ResumeReconnectAsync();
    }

    private void ClearLog(object? sender, RoutedEventArgs eventArgs)
    {
        RunOnUiThread(() => LogTextBox.Clear());
    }

    private async void CopyLog(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            string logText = await RunOnUiThreadAsync(() => LogTextBox.Text);
            if (string.IsNullOrEmpty(logText))
            {
                AppendLogLine("Log is empty; nothing to copy.");
                return;
            }

            bool copied = await RunOnUiThreadAsync(() =>
                WpfClipboardRetry.TrySetTextAsync(logText, System.Windows.Clipboard.SetText));
            AppendLogLine(copied
                ? "Log copied to clipboard."
                : "Copy log failed because clipboard is busy. Try again.");
        }
        catch (Exception exception)
        {
            AppendLogLine($"Copy log error: {exception.Message}");
            ApplyUiState(_stateController.FailOperation());
        }
    }

    private Task ValidateConfigAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _commandService.ValidateConfig(GetConfigPath(), CreateLogWriter());
        return Task.CompletedTask;
    }

    private Task PrintEffectiveConfigAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CaptureBackendOptions selectedCaptureOptions = GetSelectedCaptureBackendOptions();
        CaptureBackend configCaptureBackend = TryLoadSettings()?.Capture.Backend ?? CaptureBackend.VisiblePixels;
        AppendLogLine("Current run settings:");
        AppendLogLine($"Config capture backend: {configCaptureBackend}");
        AppendLogLine($"GUI selected capture backend: {selectedCaptureOptions.Backend}");
        AppendLogLine($"Current run requested backend: {selectedCaptureOptions.Backend}");
        AppendLogLine($"Allow backend fallback: {selectedCaptureOptions.AllowBackendFallback}");
        AppendLogLine($"Actual backend last used: {_runtimeStatus.Snapshot.ActualCaptureBackend}");
        if (configCaptureBackend != selectedCaptureOptions.Backend)
        {
            AppendLogLine("GUI capture backend override active.");
        }

        _commandService.PrintEffectiveConfig(
            GetConfigPath(),
            GetSelectedOcrEngine(),
            selectedCaptureOptions,
            CreateLogWriter());
        return Task.CompletedTask;
    }

    private async Task CalibrateOcrRegionAsync(CancellationToken cancellationToken)
    {
        CaptureBackendOptions captureBackendOptions = GetSelectedCaptureBackendOptions();
        LogGuiSelectedCaptureBackend(captureBackendOptions);
        ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
            captureBackendOptions.Backend.ToString(),
            captureBackendOptions.Backend == CaptureBackend.WindowsGraphicsCapture ? "(pending)" : captureBackendOptions.Backend.ToString(),
            FormatSelectedCaptureBackendStatus(captureBackendOptions.Backend)));
        if (captureBackendOptions.Backend == CaptureBackend.WindowsGraphicsCapture)
        {
            AppendLogLine("WindowsGraphicsCapture selected for calibration; using selected capture backend without foreground activation.");
            await _commandService.CalibrateOcrRegionAsync(
                GetConfigPath(),
                captureBackendOptions,
                CreateLogWriter(),
                cancellationToken);
            RefreshStatus();
            return;
        }

        TargetWindowActivationResult activationResult = await RunAutomaticForegroundActivationAsync(
            "calibration",
            GetInputForegroundFallbackEnabled(),
            cancellationToken);

        if (activationResult.Success)
        {
            try
            {
                await _commandService.CalibrateOcrRegionFromForegroundWindowAsync(
                    GetConfigPath(),
                    CreateLogWriter(),
                    cancellationToken);
            }
            finally
            {
                await RestoreAfterManualForegroundCalibrationAsync(WindowState.Normal);
            }

            RefreshStatus();
            return;
        }

        await RestoreAfterManualForegroundCalibrationAsync(WindowState.Normal);
        if (GuiForegroundActivationPolicy.ShouldFailImmediately(activationResult))
        {
            throw new WindowCaptureException(activationResult.UserMessage);
        }

        AppendLogLine("Falling back to manual foreground calibration.");
        bool retry = await PromptManualForegroundRetryAsync(activationResult.UserMessage, cancellationToken);
        if (!retry)
        {
            throw new OperationCanceledException("Manual foreground calibration was cancelled.");
        }

        await RunManualForegroundCalibrationFallbackAsync(cancellationToken);
        RefreshStatus();
    }

    private async Task TestOcrOnceAsync(CancellationToken cancellationToken)
    {
        await _commandService.OcrOnceAsync(
            GetConfigPath(),
            GetRequiredOcrInputPath(),
            GetSelectedOcrEngine(),
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task WarmUpOcrBackendAsync(CancellationToken cancellationToken)
    {
        await SetOcrBackendStatusAsync("Warming up");
        try
        {
            GuiOcrWarmupResult result = await _commandService.WarmUpOcrBackendAsync(
                GetConfigPath(),
                GetSelectedOcrEngine(),
                CreateLogWriter(),
                cancellationToken);
            await SetOcrBackendStatusAsync(result.IsWarm ? "Ready" : "Not initialized");
        }
        catch
        {
            await SetOcrBackendStatusAsync("Failed");
            throw;
        }
    }

    private async Task StartDryRunDetectionAsync(CancellationToken cancellationToken)
    {
        ApplyRuntimeSnapshot(_runtimeStatus.MarkDetecting());
        DetectionRunContext context = new(DetectionOperationKind.DryRun, GetDetectionLaunchInput());
        _resumeContext = context;
        await RunDetectionRunContextAsync(context, cancellationToken);
    }

    private async Task StartSimulatedDetectionAudioAsync(CancellationToken cancellationToken)
    {
        ApplyRuntimeSnapshot(_runtimeStatus.MarkDetecting());
        DetectionRunContext context = new(DetectionOperationKind.SimulatedAudio, GetDetectionLaunchInput());
        _resumeContext = context;
        await RunDetectionRunContextAsync(context, cancellationToken);
    }

    private async Task StartGuardedRealAudioDetectionAsync(CancellationToken cancellationToken)
    {
        ApplyRuntimeSnapshot(_runtimeStatus.MarkDetecting());
        DetectionRunContext context = new(DetectionOperationKind.GuardedRealAudio, GetDetectionLaunchInput());
        _resumeContext = context;
        await RunDetectionRunContextAsync(context, cancellationToken);
    }

    private Task RunDetectionRunContextAsync(DetectionRunContext context, CancellationToken cancellationToken)
    {
        DetectionLaunchInput launch = context.Launch;
        return context.OperationKind switch
        {
            DetectionOperationKind.DryRun => RunDetectionWithForegroundStartupAsync(
                launch,
                "dry-run detection",
            normalOperation: token => _commandService.RunDetectionLoopAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                false,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunDetectionLoopFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                false,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
                cancellationToken),
            DetectionOperationKind.SimulatedAudio => RunDetectionWithForegroundStartupAsync(
                launch,
                "simulated detection audio",
            normalOperation: token => _commandService.RunDetectionLoopAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                true,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunDetectionLoopFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                true,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
                cancellationToken),
            DetectionOperationKind.GuardedRealAudio => RunDetectionWithForegroundStartupAsync(
                launch,
                "guarded real audio detection",
            normalOperation: token => _commandService.RunGuardedRealAudioDetectionAsync(
                launch.ConfigPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunGuardedRealAudioDetectionFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                launch.OcrEngine,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token,
                OnDetectionIterationCompleted),
                cancellationToken),
            _ => throw new InvalidOperationException("Unknown detection operation.")
        };
    }

    private async Task RunDetectionWithForegroundStartupAsync(
        DetectionLaunchInput launch,
        string operationName,
        Func<CancellationToken, Task> normalOperation,
        Func<Func<Task>, CancellationToken, Task> foregroundOperation,
        CancellationToken cancellationToken)
    {
        LogGuiSelectedCaptureBackend(launch.TuningOptions.CaptureBackendOptions);
        ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
            launch.TuningOptions.CaptureBackendOptions.Backend.ToString(),
            launch.TuningOptions.CaptureBackendOptions.Backend == CaptureBackend.WindowsGraphicsCapture ? "(pending)" : launch.TuningOptions.CaptureBackendOptions.Backend.ToString(),
            FormatSelectedCaptureBackendStatus(launch.TuningOptions.CaptureBackendOptions.Backend)));

        if (!GuiForegroundActivationPolicy.ShouldUseForegroundStartup(
            launch.TuningOptions.CaptureBackendOptions.Backend,
            launch.UseFixedImageForDetection))
        {
            if (launch.TuningOptions.CaptureBackendOptions.Backend == CaptureBackend.WindowsGraphicsCapture &&
                !launch.UseFixedImageForDetection)
            {
                AppendLogLine("WindowsGraphicsCapture selected; starting through capture backend without foreground activation.");
            }

            await normalOperation(cancellationToken);
            return;
        }

        TargetWindowActivationResult activationResult = await RunAutomaticForegroundActivationAsync(
            "detection startup",
            launch.TuningOptions.EnableInputForegroundFallback,
            cancellationToken);

        if (activationResult.Success)
        {
            AppendLogLine("Activation succeeded; creating foreground capture session.");
            try
            {
                await foregroundOperation(
                    () =>
                    {
                        AppendLogLine($"Foreground capture session started for {operationName}; WPF shell will remain minimized until detection stops.");
                        return Task.CompletedTask;
                    },
                    cancellationToken);
            }
            finally
            {
                await RestoreAfterManualForegroundCalibrationAsync(WindowState.Normal);
            }

            return;
        }

        await RestoreAfterManualForegroundCalibrationAsync(WindowState.Normal);
        if (GuiForegroundActivationPolicy.ShouldFailImmediately(activationResult))
        {
            throw new WindowCaptureException(activationResult.UserMessage);
        }

        AppendLogLine("Falling back to manual foreground startup.");
        bool retry = await PromptManualForegroundDetectionRetryAsync(activationResult.UserMessage, cancellationToken);
        if (!retry)
        {
            throw new OperationCanceledException($"Manual foreground startup for {operationName} was cancelled.");
        }

        await RunManualForegroundDetectionFallbackAsync(operationName, foregroundOperation, cancellationToken);
    }

    private async Task<TargetWindowActivationResult> RunAutomaticForegroundActivationAsync(
        string purpose,
        bool inputFallbackEnabled,
        CancellationToken cancellationToken)
    {
        WindowState previousState = await MinimizeForManualForegroundCalibrationAsync();
        try
        {
            AppendLogLine($"Trying Win32 activation for {purpose}.");
            TargetWindowActivationResult result = await _commandService.TryActivateTargetWindowAsync(
                GetConfigPath(),
                GetCaptureDelayMsForActivation(),
                inputFallbackEnabled,
                CreateLogWriter(),
                cancellationToken);

            if (result.Success)
            {
                if (result.Method == TargetWindowActivationMethod.InputFallback)
                {
                    AppendLogLine($"Trying input fallback for {purpose}.");
                    AppendLogLine("Input foreground fallback succeeded.");
                    AppendLogLine($"Activation succeeded: {result.UserMessage}");
                    return result;
                }

                AppendLogLine($"Win32 activation succeeded: {result.UserMessage}");
                return result;
            }

            AppendLogLine($"Win32 activation failed: {result.FailureReason}. {result.UserMessage}");
            if (inputFallbackEnabled)
            {
                if (result.InputFallbackAttempted)
                {
                    AppendLogLine($"Trying input fallback for {purpose}.");
                    AppendLogLine($"Input foreground fallback failed: {result.FailureReason}. {result.UserMessage}");
                }
                else
                {
                    AppendLogLine("Input fallback was enabled but was not attempted.");
                }
            }
            else
            {
                AppendLogLine("Input fallback disabled; falling back to manual foreground.");
            }

            if (GuiManualForegroundFallbackFlow.ShouldRestoreAfterSessionFailure)
            {
                await RestoreAfterManualForegroundCalibrationAsync(previousState);
            }

            return result;
        }
        catch
        {
            if (GuiManualForegroundFallbackFlow.ShouldRestoreAfterSessionFailure)
            {
                await RestoreAfterManualForegroundCalibrationAsync(previousState);
            }

            throw;
        }
    }

    private async Task StartGuardedRealAudioWithConfirmationAsync()
    {
        try
        {
            GuardedRealAudioUiEligibility eligibility = GetGuardedRealAudioEligibility();
            if (!eligibility.CanRequestConfirmation)
            {
                AppendLogLine($"Guarded real audio is not ready: {eligibility.DisabledReason}");
                RefreshStatus();
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                "This will control the configured target process audio using stable OCR detection only.\n\n" +
                "Start with reduce-volume mode before using mute.\n" +
                "Stop or closing this window will try to restore audio.\n" +
                "If Windows audio sessions change or restore fails, you may still need to restore the system volume mixer manually.\n\n" +
                "Start guarded real audio now?",
                "Confirm Guarded Real Audio",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                AppendLogLine("Guarded real audio start cancelled by user.");
                return;
            }

            await RunOperationAsync(StartGuardedRealAudioDetectionAsync, cancellable: true);
        }
        catch (Exception exception)
        {
            AppendLogLine($"Guarded real audio error: {exception.Message}");
            ApplyUiState(_stateController.FailOperation());
        }
    }

    private async Task ResumeReconnectAsync()
    {
        DetectionRunContext? context = _resumeContext;
        if (context is null || !CanResumeReconnect())
        {
            AppendLogLine("Resume/Reconnect is not available. Start detection again from the normal controls.");
            return;
        }

        AppendLogLine("Resume requested.");
        if (context.Value.OperationKind == DetectionOperationKind.GuardedRealAudio)
        {
            GuardedRealAudioUiEligibility eligibility = GetGuardedRealAudioEligibility();
            if (!eligibility.CanRequestConfirmation)
            {
                AppendLogLine($"Resume blocked: guarded real audio is not ready: {eligibility.DisabledReason}");
                RefreshStatus();
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                "Resume guarded real audio detection with the previous run settings?\n\n" +
                "This still controls the configured target process audio only after stable OCR detection. Stop/close will try to restore audio.",
                "Confirm Guarded Real Audio Resume",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                AppendLogLine("Resume cancelled by user.");
                return;
            }
        }

        await RunOperationAsync(
            async token =>
            {
                ApplyRuntimeSnapshot(_runtimeStatus.MarkReconnecting());
                AppendLogLine("Reconnecting foreground capture session.");
                await RunDetectionRunContextAsync(context.Value, token);
                AppendLogLine("Resume succeeded.");
            },
            cancellable: true);
    }

    private async Task<bool> PromptManualForegroundRetryAsync(string failureMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBoxResult result = await Dispatcher.InvokeAsync(() => System.Windows.MessageBox.Show(
            this,
            failureMessage + "\n\n" +
            $"Click OK, then manually restore and switch to the target window within {ManualForegroundRetryDelay.TotalSeconds:0} seconds. Keep it visible and uncovered while the retry runs.\n\n" +
            "Windowed or borderless window mode is recommended. Exclusive fullscreen may minimize or block visible-pixel capture when focus changes.",
            "Target Window Capture Needs Manual Foreground",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.OK));

        return result == MessageBoxResult.OK;
    }

    private async Task<bool> PromptManualForegroundDetectionRetryAsync(string failureMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBoxResult result = await Dispatcher.InvokeAsync(() => System.Windows.MessageBox.Show(
            this,
            failureMessage + "\n\n" +
            "Need manual switch to target window.\n\n" +
            "The current capture path can only capture visible windows, and automatic restore failed.\n" +
            $"Click OK to start manual switching. This WPF window will minimize, then you have {ManualForegroundRetryDelay.TotalSeconds:0} seconds to switch to the target window manually.\n\n" +
            "Keep the target visible and uncovered. Windowed or borderless window mode is recommended.\n\n" +
            "After detection starts, the WPF shell will stay minimized until detection stops or fails. If you switch back to WPF, the game may minimize again and detection may stop.",
            "Need Manual Switch To Target Window",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.OK));

        return result == MessageBoxResult.OK;
    }

    private async Task RunManualForegroundCalibrationFallbackAsync(CancellationToken cancellationToken)
    {
        WindowState previousState = await MinimizeForManualForegroundCalibrationAsync();
        try
        {
            AppendLogLine("WPF window minimized for manual foreground calibration fallback.");
            AppendLogLine($"Waiting {ManualForegroundRetryDelay.TotalSeconds:0} seconds for you to manually switch to the target window.");
            await Task.Delay(ManualForegroundRetryDelay, cancellationToken);
            AppendLogLine("Capturing current foreground window for calibration.");
            await _commandService.CalibrateOcrRegionFromForegroundWindowAsync(
                GetConfigPath(),
                CreateLogWriter(),
                cancellationToken);
        }
        finally
        {
            await RestoreAfterManualForegroundCalibrationAsync(previousState);
        }
    }

    private async Task RunManualForegroundDetectionFallbackAsync(
        string operationName,
        Func<Func<Task>, CancellationToken, Task> foregroundOperation,
        CancellationToken cancellationToken)
    {
        WindowState previousState = await MinimizeForManualForegroundCalibrationAsync();
        try
        {
            AppendLogLine($"WPF window minimized for manual foreground {operationName} startup.");
            AppendLogLine($"Waiting {ManualForegroundRetryDelay.TotalSeconds:0} seconds for you to manually switch to the target window.");
            await Task.Delay(ManualForegroundRetryDelay, cancellationToken);
            AppendLogLine($"Creating foreground capture session for {operationName}.");

            await foregroundOperation(
                () =>
                {
                    if (GuiManualForegroundFallbackFlow.ShouldRestoreAfterSessionReady)
                    {
                        return RestoreAfterManualForegroundCalibrationAsync(previousState);
                    }

                    AppendLogLine("Manual foreground capture session started; WPF shell will remain minimized until detection stops.");
                    return Task.CompletedTask;
                },
                cancellationToken);
        }
        finally
        {
            if (GuiManualForegroundFallbackFlow.ShouldRestoreAfterOperationCompleted)
            {
                await RestoreAfterManualForegroundCalibrationAsync(previousState);
            }
        }
    }

    private async Task<WindowState> MinimizeForManualForegroundCalibrationAsync()
    {
        return await Dispatcher.InvokeAsync(() =>
        {
            WindowState previousState = WindowState;
            WindowState = WindowState.Minimized;
            return previousState;
        });
    }

    private async Task RestoreAfterManualForegroundCalibrationAsync(WindowState previousState)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (!IsVisible)
            {
                Show();
            }

            WindowState = previousState == WindowState.Minimized
                ? WindowState.Normal
                : previousState;
            Activate();
        });
    }

    private async Task RunOperationAsync(Func<CancellationToken, Task> operation, bool cancellable)
    {
        if (_operationTask is { IsCompleted: false })
        {
            AppendLogLine("Another operation is already running.");
            return;
        }

        ApplyUiState(_stateController.StartOperation(cancellable));
        ApplyRuntimeSnapshot(_runtimeStatus.MarkStarting());
        _operationCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _operationCancellation.Token;
        _operationTask = RunOperationCoreAsync(operation, cancellationToken);
        await _operationTask;
    }

    private async Task RunOperationCoreAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        try
        {
            AppendLogLine($"> {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            await Task.Run(async () => await operation(cancellationToken), cancellationToken);
            AppendLogLine("Operation completed.");
            _resumeContext = null;
            ApplyUiState(_stateController.CompleteOperation());
            ApplyRuntimeSnapshot(_runtimeStatus.MarkIdle());
        }
        catch (OperationCanceledException)
        {
            AppendLogLine("Operation cancelled.");
            _resumeContext = null;
            ApplyUiState(_stateController.CompleteOperation());
            ApplyRuntimeSnapshot(_runtimeStatus.MarkIdle());
        }
        catch (WindowCaptureException exception) when (WindowCaptureException.IsForegroundCaptureLost(exception))
        {
            AppendLogLine($"Capture lost: {exception.Message}");
            GuiAudioState previousAudioState = _runtimeStatus.Snapshot.AudioState;
            if (previousAudioState is GuiAudioState.Reduced or GuiAudioState.Muted)
            {
                AppendLogLine("Capture lost; restore requested.");
            }
            else
            {
                AppendLogLine("Capture lost while audio was already restored.");
            }

            AppendLogLine("Target window is no longer visible. Detection stopped safely.");
            ApplyUiState(_stateController.FailOperation());
            ApplyRuntimeSnapshot(_runtimeStatus.MarkCaptureLost());
        }
        catch (CaptureBackendException exception)
        {
            AppendLogLine($"Capture backend error: {exception.Message}");
            AppendLogLine($"Requested capture backend: {exception.Backend}");
            AppendLogLine("Actual capture backend: (none)");
            ApplyUiState(_stateController.FailOperation());
            ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                exception.Backend.ToString(),
                "(none)",
                $"Error: {exception.Reason}"));
        }
        catch (Exception exception)
        {
            AppendLogLine($"Error: {exception.Message}");
            ApplyUiState(_stateController.FailOperation());
            ApplyRuntimeSnapshot(_runtimeStatus.MarkError());
        }
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            RefreshStatus();
            DockResumeReconnectButton.IsEnabled = CanResumeReconnect();
        }
    }

    private void CancelCurrentOperation()
    {
        if (_operationCancellation is null)
        {
            return;
        }

        AppendLogLine("Stop requested.");
        ApplyUiState(_stateController.RequestStop());
        ApplyRuntimeSnapshot(_runtimeStatus.MarkStopping());
        _operationCancellation.Cancel();
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs eventArgs)
    {
        if (_closeAfterOperationStops || _operationTask is not { IsCompleted: false })
        {
            return;
        }

        eventArgs.Cancel = true;
        CancelCurrentOperation();
        try
        {
            Task completedTask = await Task.WhenAny(_operationTask, Task.Delay(TimeSpan.FromSeconds(10)));
            if (completedTask != _operationTask)
            {
                AppendLogLine("Close is continuing after waiting for cancellation cleanup.");
            }
        }
        finally
        {
            _closeAfterOperationStops = true;
            Close();
        }
    }

    private void ApplyUiState(GuiUiState uiState)
    {
        RunOnUiThread(() => ApplyUiStateCore(uiState));
    }

    private void ApplyUiStateCore(GuiUiState uiState)
    {
        bool commandsEnabled = uiState.CommandButtonsEnabled;
        SetControlsEnabled(commandsEnabled,
            BrowseConfigButton,
            ConfigPathTextBox,
            ValidateConfigButton,
            OverviewValidateConfigButton,
            PrintEffectiveConfigButton,
            BrowseOcrImageButton,
            OcrInputPathTextBox,
            CalibrateOcrRegionButton,
            TestOcrOnceButton,
            OverviewTestOcrOnceButton,
            UseFixedImageForDetectionCheckBox,
            SaveDebugImagesCheckBox,
            SaveOcrFailureSamplesCheckBox,
            OcrEngineComboBox,
            WarmUpOcrBackendButton,
            CaptureBackendComboBox,
            AllowCaptureBackendFallbackCheckBox,
            RunUntilStopCheckBox,
            LoopIntervalTextBox,
            CaptureDelayTextBox,
            MatchThresholdTextBox,
            MissThresholdTextBox,
            EnableInputForegroundFallbackCheckBox,
            StartDryRunButton,
            OverviewStartDryRunButton,
            StartSimulatedAudioButton,
            OverviewStartSimulatedButton,
            DockEnableGuardedRealAudioCheckBox,
            EnableGuardedRealAudioCheckBox);

        UpdateLoopCountInputState();
        HeaderStopButton.IsEnabled = uiState.StopButtonEnabled;
        DockStopButton.IsEnabled = uiState.StopButtonEnabled;
        DockResumeReconnectButton.IsEnabled = CanResumeReconnect();
        DockRestoreButton.IsEnabled = uiState.StopButtonEnabled;
        DetectionStopButton.IsEnabled = uiState.StopButtonEnabled;
        AudioStopButton.IsEnabled = uiState.StopButtonEnabled;
        RefreshGuardedRealAudioStatus();
        RefreshOcrBackendStatus();
    }

    private void ApplyRuntimeSnapshot(GuiStatusSnapshot snapshot)
    {
        RunOnUiThread(() => ApplyRuntimeSnapshotCore(snapshot));
    }

    private void ApplyRuntimeSnapshotCore(GuiStatusSnapshot snapshot)
    {
        RunStateStatusTextBlock.Text = snapshot.RunState.ToString();
        OverviewRunStateTextBlock.Text = $"Run state: {snapshot.RunState}";
        DockAudioStateTextBlock.Text = snapshot.AudioState.ToString();
        DockCaptureBackendTextBlock.Text = $"Requested: {snapshot.RequestedCaptureBackend}; actual: {snapshot.ActualCaptureBackend}";
        DockCaptureStatusTextBlock.Text = snapshot.CaptureStatus;
        DockLastOcrTextBlock.Text = snapshot.LastOcrText;
        DockLastDetectedSpeakerTextBlock.Text = snapshot.LastDetectedSpeaker;
        DockLastAudioActionTextBlock.Text = snapshot.LastAudioAction;
        DockResumeReconnectButton.IsEnabled = CanResumeReconnect();
    }

    private void OnDetectionIterationCompleted(DetectionDryRunResult result)
    {
        GuiLastObservation observation = GuiRuntimeStatus.FromDetectionResult(result);
        ApplyRuntimeSnapshot(_runtimeStatus.ApplyObservation(observation));
    }

    private static void SetControlsEnabled(bool enabled, params WpfControl[] controls)
    {
        foreach (WpfControl control in controls)
        {
            control.IsEnabled = enabled;
        }
    }

    private void RefreshStatus()
    {
        RunOnUiThread(RefreshStatusCore);
    }

    private void RefreshStatusCore()
    {
        AppSettings? settings = TryLoadSettings();
        ConfigStatusTextBlock.Text = string.IsNullOrWhiteSpace(GetConfigPath()) ? "(default)" : GetConfigPath();
        CaptureModeStatusTextBlock.Text = UseFixedImageForDetectionCheckBox.IsChecked == true
            ? "Fixed image"
            : $"Live capture ({GetSelectedCaptureBackendOptions().Backend})";
        OverviewCaptureModeTextBlock.Text = $"Capture mode: {CaptureModeStatusTextBlock.Text}";

        if (settings is null)
        {
            TargetProcessStatusTextBlock.Text = "(config error)";
            TargetSpeakersStatusTextBlock.Text = "(config error)";
            RegionSourceSummaryTextBlock.Text = "OCR region source: unavailable until config loads.";
            OverviewTargetProcessTextBlock.Text = "Target process: (config error)";
            OverviewTargetSpeakersTextBlock.Text = "Target speakers: (config error)";
            OverviewRegionSourceTextBlock.Text = "OCR region source: (config error)";
        }
        else
        {
            string speakers = string.Join(", ", settings.TargetSpeakers);
            string regionSource = FormatRegionSource(settings.Ocr.GetOcrRegionSourceOptions());
            TargetProcessStatusTextBlock.Text = settings.TargetProcessName;
            TargetSpeakersStatusTextBlock.Text = speakers;
            RegionSourceSummaryTextBlock.Text = $"OCR region source: {regionSource}";
            OverviewTargetProcessTextBlock.Text = $"Target process: {settings.TargetProcessName}";
            OverviewTargetSpeakersTextBlock.Text = $"Target speakers: {speakers}";
            OverviewRegionSourceTextBlock.Text = $"OCR region source: {regionSource}";
        }

        RealAudioStatusTextBlock.Text = IsGuardedRealAudioEnabled()
            ? "Armed, confirmation required"
            : "Disabled";
        OverviewRealAudioTextBlock.Text = $"Real audio: {RealAudioStatusTextBlock.Text}";
        RefreshOcrBackendStatusCore();
        RefreshCaptureBackendStatusCore();
        RefreshGuardedRealAudioStatus();
    }

    private void RefreshGuardedRealAudioStatus()
    {
        RunOnUiThread(RefreshGuardedRealAudioStatusCore);
    }

    private void RefreshGuardedRealAudioStatusCore()
    {
        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath(), GetSelectedOcrEngine());
        GuardedRealAudioUiEligibility eligibility = new(
            EnableGuardedRealAudioCheckBox.IsChecked == true,
            _stateController.OperationActive,
            UseFixedImageForDetectionCheckBox.IsChecked == true,
            status.PreflightPassed,
            status.HasOcrRegionSource,
            !string.IsNullOrWhiteSpace(status.TargetProcessName));

        RealAudioTargetTextBlock.Text = string.IsNullOrWhiteSpace(status.TargetProcessName)
            ? "Target process: (missing)"
            : $"Target process: {status.TargetProcessName}";
        RealAudioFilterTextBlock.Text = $"Audio filter: {status.AudioMode}, {status.VolumePercent}%";
        DockAudioFilterTextBlock.Text = $"{status.AudioMode}, {status.VolumePercent}%";
        GuardedReadinessTextBlock.Text = eligibility.CanRequestConfirmation
            ? "Ready for confirmation."
            : BuildGuardedRealAudioStatusText(eligibility, status);
        DockGuardedReadinessTextBlock.Text = GuardedReadinessTextBlock.Text;
        StartGuardedRealAudioButton.IsEnabled = eligibility.CanRequestConfirmation;
        DockStartGuardedRealAudioButton.IsEnabled = eligibility.CanRequestConfirmation;
    }

    private GuardedRealAudioUiEligibility GetGuardedRealAudioEligibility()
    {
        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath(), GetSelectedOcrEngine());
        return new GuardedRealAudioUiEligibility(
            IsGuardedRealAudioEnabled(),
            _stateController.OperationActive,
            IsFixedImageForDetectionEnabled(),
            status.PreflightPassed,
            status.HasOcrRegionSource,
            !string.IsNullOrWhiteSpace(status.TargetProcessName));
    }

    private static string BuildGuardedRealAudioStatusText(
        GuardedRealAudioUiEligibility eligibility,
        GuardedRealAudioStatus status)
    {
        if (status.Issues.Count == 0)
        {
            return eligibility.DisabledReason;
        }

        return $"{eligibility.DisabledReason} {string.Join(" ", status.Issues)}";
    }

    private void UpdateLoopCountInputState()
    {
        RunOnUiThread(UpdateLoopCountInputStateCore);
    }

    private void UpdateLoopCountInputStateCore()
    {
        LoopCountTextBox.IsEnabled = RunUntilStopCheckBox.IsChecked != true && _stateController.Current.CommandButtonsEnabled;
    }

    private AppSettings? TryLoadSettings()
    {
        try
        {
            string? configPath = GetConfigPath();
            return string.IsNullOrWhiteSpace(configPath)
                ? _settingsLoader.LoadDefault()
                : _settingsLoader.LoadFromFile(configPath);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatRegionSource(OcrRegionSourceOptions options)
    {
        if (options.AbsoluteRegion is not null)
        {
            return "absolute pixels";
        }

        if (!string.IsNullOrWhiteSpace(options.CalibrationFilePath))
        {
            return $"calibration file: {options.CalibrationFilePath}";
        }

        if (options.Preset is not null && options.Preset != OcrRegionPreset.None)
        {
            return $"preset: {options.Preset}";
        }

        return "none/full image";
    }

    private TextWriter CreateLogWriter()
    {
        return new UiTextWriter(AppendLog);
    }

    private string? GetConfigPath()
    {
        return RunOnUiThread(() =>
        {
            string value = ConfigPathTextBox.Text.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        });
    }

    private OcrEngine GetSelectedOcrEngine()
    {
        return RunOnUiThread(() => GuiOcrEngineSelection.Parse(OcrEngineComboBox.SelectedItem?.ToString()));
    }

    private void RefreshOcrEngineSelectionFromConfig()
    {
        RunOnUiThread(() =>
        {
            AppSettings? settings = TryLoadSettings();
            OcrEngine engine = settings?.Ocr.Engine ?? OcrEngine.TesseractCli;
            OcrEngineComboBox.SelectedItem = engine.ToString();
            RefreshOcrBackendStatusCore();
        });
    }

    private void RefreshCaptureBackendSelectionFromConfig()
    {
        RunOnUiThread(() =>
        {
            AppSettings? settings = TryLoadSettings();
            CaptureBackend backend = settings?.Capture.Backend ?? CaptureBackend.VisiblePixels;
            CaptureBackendComboBox.SelectedItem = backend.ToString();
            AllowCaptureBackendFallbackCheckBox.IsChecked = settings?.Capture.AllowBackendFallback ?? false;
            RefreshCaptureBackendStatusCore();
        });
    }

    private void CaptureBackendSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        RefreshCaptureBackendStatus();
        RefreshStatus();
    }

    private void CaptureFallbackChanged(object? sender, RoutedEventArgs eventArgs)
    {
        RefreshCaptureBackendStatus();
        RefreshStatus();
    }

    private void RefreshOcrBackendStatus()
    {
        RunOnUiThread(RefreshOcrBackendStatusCore);
    }

    private void RefreshOcrBackendStatusCore()
    {
        try
        {
            OcrEngine engine = GetSelectedOcrEngine();
            bool isWarm = _commandService.IsOcrBackendWarm(GetConfigPath(), engine);
            string warmStatus = isWarm ? "Ready" : "Not initialized";
            OcrBackendStatusTextBlock.Text = $"Backend status: {warmStatus}";
            DockOcrEngineTextBlock.Text = engine.ToString();
            DockOcrWarmStatusTextBlock.Text = warmStatus;
        }
        catch
        {
            OcrBackendStatusTextBlock.Text = "Backend status: Not initialized";
            DockOcrEngineTextBlock.Text = "(unknown)";
            DockOcrWarmStatusTextBlock.Text = "Not initialized";
        }
    }

    private Task SetOcrBackendStatusAsync(string status)
    {
        return RunOnUiThreadAsync(() =>
        {
            OcrBackendStatusTextBlock.Text = $"Backend status: {status}";
            DockOcrWarmStatusTextBlock.Text = status;
        });
    }

    private void RefreshCaptureBackendStatus()
    {
        RunOnUiThread(RefreshCaptureBackendStatusCore);
    }

    private void LogGuiSelectedCaptureBackend(CaptureBackendOptions options)
    {
        AppendLogLine($"GUI selected capture backend: {options.Backend}");
        AppendLogLine($"GUI allow backend fallback: {options.AllowBackendFallback}");
    }

    private static string FormatSelectedCaptureBackendStatus(CaptureBackend backend)
    {
        return backend == CaptureBackend.WindowsGraphicsCapture
            ? "WindowsGraphicsCapture selected."
            : "VisiblePixels requires target window to stay visible.";
    }

    private void RefreshCaptureBackendStatusCore()
    {
        try
        {
            CaptureBackendOptions options = GetSelectedCaptureBackendOptions();
            IGameCaptureBackend backend = options.Backend == CaptureBackend.WindowsGraphicsCapture
                ? new WindowsGraphicsCaptureBackend(options.CaptureTimeoutMs)
                : new VisiblePixelsCaptureBackend();
            CaptureBackendAvailability availability = backend.CheckAvailability();
            string status = availability.Available
                ? "Ready"
                : $"Unavailable: {availability.FailureReason}";
            CaptureBackendStatusTextBlock.Text =
                $"Capture backend: {options.Backend}; fallback: {options.AllowBackendFallback}. Status: {status}. {availability.Message}";
            ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                options.Backend.ToString(),
                availability.Available ? options.Backend.ToString() : "(none)",
                $"{status}. {FormatSelectedCaptureBackendStatus(options.Backend)}"));
        }
        catch (Exception exception)
        {
            CaptureBackendStatusTextBlock.Text = $"Capture backend status: {exception.Message}";
            ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend("(invalid)", "Error"));
        }
    }

    private CaptureBackend GetSelectedCaptureBackend()
    {
        return RunOnUiThread(() =>
        {
            string value = CaptureBackendComboBox.SelectedItem?.ToString() ?? CaptureBackend.VisiblePixels.ToString();
            if (TryParseCaptureBackend(value, out CaptureBackend backend))
            {
                return backend;
            }

            throw new ArgumentException("Capture backend must be 'VisiblePixels' or 'WindowsGraphicsCapture'.");
        });
    }

    private static bool TryParseCaptureBackend(string value, out CaptureBackend result)
    {
        foreach (CaptureBackend backend in Enum.GetValues<CaptureBackend>())
        {
            if (string.Equals(value, backend.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                result = backend;
                return true;
            }
        }

        result = CaptureBackend.VisiblePixels;
        return false;
    }

    private CaptureBackendOptions GetSelectedCaptureBackendOptions()
    {
        return RunOnUiThread(() => new CaptureBackendOptions
        {
            Backend = GetSelectedCaptureBackend(),
            AllowBackendFallback = AllowCaptureBackendFallbackCheckBox.IsChecked == true
        });
    }

    private string GetRequiredOcrInputPath()
    {
        return RunOnUiThread(() =>
        {
            string value = OcrInputPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("OCR input image path is required for Test OCR Once.");
            }

            return value;
        });
    }

    private string? GetOptionalOcrInputPath()
    {
        return RunOnUiThread(() =>
        {
            string value = OcrInputPathTextBox.Text.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        });
    }

    private bool IsFixedImageForDetectionEnabled()
    {
        return RunOnUiThread(() => UseFixedImageForDetectionCheckBox.IsChecked == true);
    }

    private bool IsGuardedRealAudioEnabled()
    {
        return RunOnUiThread(() =>
            EnableGuardedRealAudioCheckBox.IsChecked == true ||
            DockEnableGuardedRealAudioCheckBox.IsChecked == true);
    }

    private bool CanResumeReconnect()
    {
        return _resumeContext is not null &&
            !_stateController.OperationActive &&
            _runtimeStatus.Snapshot.RunState is GuiRuntimeRunState.CaptureLost or GuiRuntimeRunState.Error;
    }

    private void SyncGuardedRealAudioEnablement(bool fromDock)
    {
        if (_syncingGuardedRealAudioCheckBoxes)
        {
            return;
        }

        RunOnUiThread(() =>
        {
            _syncingGuardedRealAudioCheckBoxes = true;
            try
            {
                bool value = fromDock
                    ? DockEnableGuardedRealAudioCheckBox.IsChecked == true
                    : EnableGuardedRealAudioCheckBox.IsChecked == true;
                DockEnableGuardedRealAudioCheckBox.IsChecked = value;
                EnableGuardedRealAudioCheckBox.IsChecked = value;
            }
            finally
            {
                _syncingGuardedRealAudioCheckBoxes = false;
            }

            RefreshStatusCore();
        });
    }

    private GuiDetectionTuningOptions ParseGuiDetectionTuning()
    {
        GuiDetectionTuningInput input = GetDetectionTuningInput();
        return GuiDetectionTuningOptions.Parse(
            input.RunUntilStop,
            input.LoopCount,
            input.LoopIntervalMs,
            input.CaptureDelayMs,
            input.MatchThreshold,
            input.MissThreshold,
            input.SaveDebugImages,
            input.SaveOcrFailureSamples,
            input.EnableInputForegroundFallback,
            input.CaptureBackend,
            input.AllowCaptureBackendFallback);
    }

    private DetectionLaunchInput GetDetectionLaunchInput()
    {
        return new DetectionLaunchInput(
            GetConfigPath(),
            GetOptionalOcrInputPath(),
            IsFixedImageForDetectionEnabled(),
            GetSelectedOcrEngine(),
            ParseGuiDetectionTuning());
    }

    private GuiDetectionTuningInput GetDetectionTuningInput()
    {
        return RunOnUiThread(() => new GuiDetectionTuningInput(
            RunUntilStopCheckBox.IsChecked == true,
            LoopCountTextBox.Text,
            LoopIntervalTextBox.Text,
            CaptureDelayTextBox.Text,
            MatchThresholdTextBox.Text,
            MissThresholdTextBox.Text,
            SaveDebugImagesCheckBox.IsChecked == true,
            SaveOcrFailureSamplesCheckBox.IsChecked == true,
            EnableInputForegroundFallbackCheckBox.IsChecked == true,
            GetSelectedCaptureBackend(),
            AllowCaptureBackendFallbackCheckBox.IsChecked == true));
    }

    private bool GetInputForegroundFallbackEnabled()
    {
        return RunOnUiThread(() => EnableInputForegroundFallbackCheckBox.IsChecked == true);
    }

    private int GetCaptureDelayMsForActivation()
    {
        try
        {
            return ParseGuiDetectionTuning().CaptureDelayMs ?? GuiDetectionTuningOptions.DefaultCaptureDelayMs;
        }
        catch
        {
            return GuiDetectionTuningOptions.DefaultCaptureDelayMs;
        }
    }

    private void RunBrowseAction(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            AppendLogLine($"{label} error: {exception.Message}");
            ApplyUiState(_stateController.FailOperation());
        }
    }

    private void AppendLogLine(string message)
    {
        AppendLog(message + Environment.NewLine);
    }

    private void AppendLog(string text)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => AppendLog(text)));
            return;
        }

        LogTextBox.AppendText(text);
        LogTextBox.CaretIndex = LogTextBox.Text.Length;
        LogTextBox.ScrollToEnd();
        UpdateCaptureBackendStatusFromLog(text);
    }

    private void UpdateCaptureBackendStatusFromLog(string text)
    {
        using StringReader reader = new(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("Requested capture backend: ", StringComparison.OrdinalIgnoreCase))
            {
                string requested = line["Requested capture backend: ".Length..].Trim();
                ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                    requested,
                    _runtimeStatus.Snapshot.ActualCaptureBackend,
                    _runtimeStatus.Snapshot.CaptureStatus));
                continue;
            }

            if (line.StartsWith("Actual capture backend: ", StringComparison.OrdinalIgnoreCase))
            {
                string actual = line["Actual capture backend: ".Length..].Trim();
                string status = _runtimeStatus.Snapshot.CaptureStatus.StartsWith("Fallback", StringComparison.OrdinalIgnoreCase)
                    ? _runtimeStatus.Snapshot.CaptureStatus
                    : actual.Equals("(none)", StringComparison.OrdinalIgnoreCase)
                        ? "Error"
                        : "Ready";
                ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                    _runtimeStatus.Snapshot.RequestedCaptureBackend,
                    actual,
                    status));
                continue;
            }

            if (line.StartsWith("Fallback reason: ", StringComparison.OrdinalIgnoreCase))
            {
                string reason = line["Fallback reason: ".Length..].Trim();
                ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                    _runtimeStatus.Snapshot.RequestedCaptureBackend,
                    _runtimeStatus.Snapshot.ActualCaptureBackend,
                    $"Fallback to VisiblePixels: {reason}"));
                continue;
            }

            if (line.StartsWith("WGC failed: ", StringComparison.OrdinalIgnoreCase))
            {
                string reason = line["WGC failed: ".Length..].Trim();
                ApplyRuntimeSnapshot(_runtimeStatus.SetCaptureBackend(
                    _runtimeStatus.Snapshot.RequestedCaptureBackend,
                    "(none)",
                    $"WGC unavailable: {reason}"));
            }
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.Invoke(action);
    }

    private T RunOnUiThread<T>(Func<T> action)
    {
        return Dispatcher.CheckAccess()
            ? action()
            : Dispatcher.Invoke(action);
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return Dispatcher.InvokeAsync(action).Task;
    }

    private Task<T> RunOnUiThreadAsync<T>(Func<T> action)
    {
        if (Dispatcher.CheckAccess())
        {
            return Task.FromResult(action());
        }

        return Dispatcher.InvokeAsync(action).Task;
    }

    private async Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> action)
    {
        if (Dispatcher.CheckAccess())
        {
            return await action();
        }

        Task<T> task = await Dispatcher.InvokeAsync(action);
        return await task;
    }
}
