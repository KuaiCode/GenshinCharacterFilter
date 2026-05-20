using GenshinCharacterFilter.Gui;
using GenshinCharacterFilter.Capture;
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
    private readonly WpfAppTheme _theme;
    private static readonly TimeSpan ManualForegroundRetryDelay = TimeSpan.FromSeconds(8);
    private CancellationTokenSource? _operationCancellation;
    private Task? _operationTask;
    private bool _closeAfterOperationStops;

    private readonly record struct GuiDetectionTuningInput(
        bool RunUntilStop,
        string LoopCount,
        string LoopIntervalMs,
        string CaptureDelayMs,
        string MatchThreshold,
        string MissThreshold,
        bool SaveDebugImages);

    private readonly record struct DetectionLaunchInput(
        string? ConfigPath,
        string? OcrInputPath,
        bool UseFixedImageForDetection,
        GuiDetectionTuningOptions TuningOptions);

    public MainWindow(string? initialConfigPath, WpfAppTheme theme)
    {
        _theme = theme;
        InitializeComponent();
        WpfWindowBackdrop.TryApply(this, theme);

        ConfigPathTextBox.Text = string.IsNullOrWhiteSpace(initialConfigPath)
            ? GuiCommandService.GetDefaultConfigPath()
            : initialConfigPath;
        OcrInputPathTextBox.Text = GuiCommandService.GetDefaultOcrInputPath();
        UseFixedImageForDetectionCheckBox.IsChecked = false;
        SaveDebugImagesCheckBox.IsChecked = false;
        RunUntilStopCheckBox.IsChecked = true;
        LoopCountTextBox.Text = string.Empty;
        LoopIntervalTextBox.Text = GuiDetectionTuningOptions.DefaultLoopIntervalMs.ToString();
        CaptureDelayTextBox.Text = GuiDetectionTuningOptions.DefaultCaptureDelayMs.ToString();
        MatchThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMatchThreshold.ToString();
        MissThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMissThreshold.ToString();

        Closing += OnWindowClosing;
        ConfigPathTextBox.TextChanged += (_, _) => RefreshStatus();
        UseFixedImageForDetectionCheckBox.Checked += (_, _) => RefreshStatus();
        UseFixedImageForDetectionCheckBox.Unchecked += (_, _) => RefreshStatus();
        EnableGuardedRealAudioCheckBox.Checked += (_, _) => RefreshStatus();
        EnableGuardedRealAudioCheckBox.Unchecked += (_, _) => RefreshStatus();
        RunUntilStopCheckBox.Checked += (_, _) => UpdateLoopCountInputState();
        RunUntilStopCheckBox.Unchecked += (_, _) => UpdateLoopCountInputState();

        ShowPage(OverviewPage);
        ApplyUiState(_stateController.Current);
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

    private void StopOperation(object? sender, RoutedEventArgs eventArgs)
    {
        CancelCurrentOperation();
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
        _commandService.PrintEffectiveConfig(GetConfigPath(), CreateLogWriter());
        return Task.CompletedTask;
    }

    private async Task CalibrateOcrRegionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _commandService.CalibrateOcrRegionAsync(GetConfigPath(), CreateLogWriter(), cancellationToken);
        }
        catch (WindowCaptureException exception)
            when (exception.Reason == WindowCaptureFailureReason.TargetWindowMinimizedCannotRestore)
        {
            AppendLogLine(exception.Message);
            bool retry = await PromptManualForegroundRetryAsync(exception.Message, cancellationToken);
            if (!retry)
            {
                throw;
            }

            await RunManualForegroundCalibrationFallbackAsync(cancellationToken);
        }

        RefreshStatus();
    }

    private async Task TestOcrOnceAsync(CancellationToken cancellationToken)
    {
        await _commandService.OcrOnceAsync(
            GetConfigPath(),
            GetRequiredOcrInputPath(),
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task StartDryRunDetectionAsync(CancellationToken cancellationToken)
    {
        DetectionLaunchInput launch = GetDetectionLaunchInput();
        await RunDetectionWithManualForegroundFallbackAsync(
            launch,
            "dry-run detection",
            normalOperation: token => _commandService.RunDetectionLoopAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                false,
                CreateLogWriter(),
                token),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunDetectionLoopFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                false,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token),
            cancellationToken);
    }

    private async Task StartSimulatedDetectionAudioAsync(CancellationToken cancellationToken)
    {
        DetectionLaunchInput launch = GetDetectionLaunchInput();
        await RunDetectionWithManualForegroundFallbackAsync(
            launch,
            "simulated detection audio",
            normalOperation: token => _commandService.RunDetectionLoopAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                true,
                CreateLogWriter(),
                token),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunDetectionLoopFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.OcrInputPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                true,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token),
            cancellationToken);
    }

    private async Task StartGuardedRealAudioDetectionAsync(CancellationToken cancellationToken)
    {
        DetectionLaunchInput launch = GetDetectionLaunchInput();
        await RunDetectionWithManualForegroundFallbackAsync(
            launch,
            "guarded real audio detection",
            normalOperation: token => _commandService.RunGuardedRealAudioDetectionAsync(
                launch.ConfigPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                CreateLogWriter(),
                token),
            foregroundOperation: (afterForegroundSessionReady, token) => _commandService.RunGuardedRealAudioDetectionFromForegroundWindowAsync(
                launch.ConfigPath,
                launch.UseFixedImageForDetection,
                launch.TuningOptions,
                afterForegroundSessionReady,
                CreateLogWriter(),
                token),
            cancellationToken);
    }

    private async Task RunDetectionWithManualForegroundFallbackAsync(
        DetectionLaunchInput launch,
        string operationName,
        Func<CancellationToken, Task> normalOperation,
        Func<Func<Task>, CancellationToken, Task> foregroundOperation,
        CancellationToken cancellationToken)
    {
        try
        {
            await normalOperation(cancellationToken);
        }
        catch (Exception exception) when (GuiManualForegroundFallbackPolicy.ShouldPromptForDetection(
            exception,
            launch.UseFixedImageForDetection))
        {
            AppendLogLine(exception.Message);
            bool retry = await PromptManualForegroundDetectionRetryAsync(exception.Message, cancellationToken);
            if (!retry)
            {
                throw new OperationCanceledException($"Manual foreground startup for {operationName} was cancelled.");
            }

            await RunManualForegroundDetectionFallbackAsync(operationName, foregroundOperation, cancellationToken);
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
            ApplyUiState(_stateController.CompleteOperation());
        }
        catch (OperationCanceledException)
        {
            AppendLogLine("Operation cancelled.");
            ApplyUiState(_stateController.CompleteOperation());
        }
        catch (Exception exception)
        {
            AppendLogLine($"Error: {exception.Message}");
            ApplyUiState(_stateController.FailOperation());
        }
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            RefreshStatus();
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
        RunStateStatusTextBlock.Text = uiState.RunState.ToString();
        OverviewRunStateTextBlock.Text = $"Run state: {uiState.RunState}";

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
            RunUntilStopCheckBox,
            LoopIntervalTextBox,
            CaptureDelayTextBox,
            MatchThresholdTextBox,
            MissThresholdTextBox,
            StartDryRunButton,
            OverviewStartDryRunButton,
            StartSimulatedAudioButton,
            OverviewStartSimulatedButton,
            EnableGuardedRealAudioCheckBox);

        UpdateLoopCountInputState();
        HeaderStopButton.IsEnabled = uiState.StopButtonEnabled;
        DetectionStopButton.IsEnabled = uiState.StopButtonEnabled;
        AudioStopButton.IsEnabled = uiState.StopButtonEnabled;
        RefreshGuardedRealAudioStatus();
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
            : "Live capture";
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

        RealAudioStatusTextBlock.Text = EnableGuardedRealAudioCheckBox.IsChecked == true
            ? "Armed, confirmation required"
            : "Disabled";
        OverviewRealAudioTextBlock.Text = $"Real audio: {RealAudioStatusTextBlock.Text}";
        RefreshGuardedRealAudioStatus();
    }

    private void RefreshGuardedRealAudioStatus()
    {
        RunOnUiThread(RefreshGuardedRealAudioStatusCore);
    }

    private void RefreshGuardedRealAudioStatusCore()
    {
        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath());
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
        GuardedReadinessTextBlock.Text = eligibility.CanRequestConfirmation
            ? "Ready for confirmation."
            : BuildGuardedRealAudioStatusText(eligibility, status);
        StartGuardedRealAudioButton.IsEnabled = eligibility.CanRequestConfirmation;
    }

    private GuardedRealAudioUiEligibility GetGuardedRealAudioEligibility()
    {
        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath());
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
        return RunOnUiThread(() => EnableGuardedRealAudioCheckBox.IsChecked == true);
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
            input.SaveDebugImages);
    }

    private DetectionLaunchInput GetDetectionLaunchInput()
    {
        return new DetectionLaunchInput(
            GetConfigPath(),
            GetOptionalOcrInputPath(),
            IsFixedImageForDetectionEnabled(),
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
            SaveDebugImagesCheckBox.IsChecked == true));
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
