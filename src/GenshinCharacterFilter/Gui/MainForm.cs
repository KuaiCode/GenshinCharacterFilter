using System.Drawing;
using System.Windows.Forms;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Minimal WinForms control panel for explicit local commands.
/// </summary>
public sealed class MainForm : Form
{
    private readonly GuiCommandService _commandService = new();
    private readonly TextBox _configPathTextBox = new();
    private readonly TextBox _ocrInputPathTextBox = new();
    private readonly TextBox _logTextBox = new();
    private readonly Button _validateConfigButton = new();
    private readonly Button _printEffectiveConfigButton = new();
    private readonly Button _calibrateOcrRegionButton = new();
    private readonly Button _testOcrOnceButton = new();
    private readonly Button _startDryRunButton = new();
    private readonly Button _startSimulatedAudioButton = new();
    private readonly Button _stopButton = new();
    private CancellationTokenSource? _operationCancellation;
    private Task? _operationTask;
    private bool _closeAfterOperationStops;

    public MainForm(string? initialConfigPath)
    {
        Text = "GenshinCharacterFilter";
        MinimumSize = new Size(920, 620);
        Size = new Size(1040, 720);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout(initialConfigPath);
        FormClosing += OnFormClosing;
    }

    private void BuildLayout(string? initialConfigPath)
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildPathRow(
            "Config file",
            _configPathTextBox,
            string.IsNullOrWhiteSpace(initialConfigPath) ? GuiCommandService.GetDefaultConfigPath() : initialConfigPath,
            BrowseConfigFile), 0, 0);
        root.Controls.Add(BuildPathRow(
            "OCR input image",
            _ocrInputPathTextBox,
            GuiCommandService.GetDefaultOcrInputPath(),
            BrowseOcrInputImage), 0, 1);
        root.Controls.Add(BuildButtonPanel(), 0, 2);

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Vertical;
        _logTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        root.Controls.Add(_logTextBox, 0, 3);

        Controls.Add(root);
    }

    private static Control BuildPathRow(
        string label,
        TextBox textBox,
        string? initialValue,
        EventHandler browseHandler)
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        Label pathLabel = new()
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        textBox.Dock = DockStyle.Fill;
        textBox.Text = initialValue ?? string.Empty;

        Button browseButton = new()
        {
            Text = "Browse",
            Dock = DockStyle.Fill
        };
        browseButton.Click += browseHandler;

        panel.Controls.Add(pathLabel, 0, 0);
        panel.Controls.Add(textBox, 1, 0);
        panel.Controls.Add(browseButton, 2, 0);
        return panel;
    }

    private Control BuildButtonPanel()
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        ConfigureButton(_validateConfigButton, "Validate Config", async (_, _) => await RunOperationAsync(ValidateConfigAsync));
        ConfigureButton(_printEffectiveConfigButton, "Print Effective Config", async (_, _) => await RunOperationAsync(PrintEffectiveConfigAsync));
        ConfigureButton(_calibrateOcrRegionButton, "Calibrate OCR Region", async (_, _) => await RunOperationAsync(CalibrateOcrRegionAsync));
        ConfigureButton(_testOcrOnceButton, "Test OCR Once", async (_, _) => await RunOperationAsync(TestOcrOnceAsync));
        ConfigureButton(_startDryRunButton, "Start Dry-run Detection", async (_, _) => await RunOperationAsync(StartDryRunDetectionAsync));
        ConfigureButton(_startSimulatedAudioButton, "Start Simulated Detection Audio", async (_, _) => await RunOperationAsync(StartSimulatedDetectionAudioAsync));

        _stopButton.Text = "Stop";
        _stopButton.Width = 130;
        _stopButton.Enabled = false;
        _stopButton.Click += (_, _) => CancelCurrentOperation();

        Button guardedRealAudioButton = new()
        {
            Text = "Guarded Real Audio (disabled)",
            Width = 190,
            Enabled = false
        };

        panel.Controls.Add(_validateConfigButton);
        panel.Controls.Add(_printEffectiveConfigButton);
        panel.Controls.Add(_calibrateOcrRegionButton);
        panel.Controls.Add(_testOcrOnceButton);
        panel.Controls.Add(_startDryRunButton);
        panel.Controls.Add(_startSimulatedAudioButton);
        panel.Controls.Add(_stopButton);
        panel.Controls.Add(guardedRealAudioButton);
        return panel;
    }

    private static void ConfigureButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Width = 220;
        button.Height = 32;
        button.Margin = new Padding(0, 0, 8, 8);
        button.Click += handler;
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
        await _commandService.CalibrateOcrRegionAsync(GetConfigPath(), CreateLogWriter(), cancellationToken);
    }

    private async Task TestOcrOnceAsync(CancellationToken cancellationToken)
    {
        string ocrInputPath = GetRequiredOcrInputPath();
        await _commandService.OcrOnceAsync(GetConfigPath(), ocrInputPath, CreateLogWriter(), cancellationToken);
    }

    private async Task StartDryRunDetectionAsync(CancellationToken cancellationToken)
    {
        await _commandService.RunDetectionLoopAsync(
            GetConfigPath(),
            GetOptionalOcrInputPath(),
            false,
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task StartSimulatedDetectionAudioAsync(CancellationToken cancellationToken)
    {
        await _commandService.RunDetectionLoopAsync(
            GetConfigPath(),
            GetOptionalOcrInputPath(),
            true,
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task RunOperationAsync(Func<CancellationToken, Task> operation)
    {
        if (_operationTask is { IsCompleted: false })
        {
            AppendLogLine("Another operation is already running.");
            return;
        }

        SetRunningState(true);
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendLogLine("Operation cancelled.");
        }
        catch (Exception exception)
        {
            AppendLogLine($"Error: {exception.Message}");
        }
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            SetRunningState(false);
        }
    }

    private void CancelCurrentOperation()
    {
        if (_operationCancellation is null)
        {
            return;
        }

        AppendLogLine("Stop requested.");
        _operationCancellation.Cancel();
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_closeAfterOperationStops || _operationTask is not { IsCompleted: false })
        {
            return;
        }

        eventArgs.Cancel = true;
        CancelCurrentOperation();
        try
        {
            await _operationTask;
        }
        finally
        {
            _closeAfterOperationStops = true;
            Close();
        }
    }

    private void SetRunningState(bool isRunning)
    {
        _validateConfigButton.Enabled = !isRunning;
        _printEffectiveConfigButton.Enabled = !isRunning;
        _calibrateOcrRegionButton.Enabled = !isRunning;
        _testOcrOnceButton.Enabled = !isRunning;
        _startDryRunButton.Enabled = !isRunning;
        _startSimulatedAudioButton.Enabled = !isRunning;
        _stopButton.Enabled = isRunning;
    }

    private TextWriter CreateLogWriter()
    {
        return new UiTextWriter(AppendLog);
    }

    private string? GetConfigPath()
    {
        string value = _configPathTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private string GetRequiredOcrInputPath()
    {
        string value = _ocrInputPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("OCR input image path is required for Test OCR Once.");
        }

        return value;
    }

    private string? GetOptionalOcrInputPath()
    {
        string value = _ocrInputPathTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void BrowseConfigFile(object? sender, EventArgs eventArgs)
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = _configPathTextBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _configPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseOcrInputImage(object? sender, EventArgs eventArgs)
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            FileName = _ocrInputPathTextBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ocrInputPathTextBox.Text = dialog.FileName;
        }
    }

    private void AppendLogLine(string message)
    {
        AppendLog(message + Environment.NewLine);
    }

    private void AppendLog(string text)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => AppendLog(text)));
            }
            catch (InvalidOperationException)
            {
                // 窗口关闭过程中可能已销毁句柄，日志无需再写入 UI。
            }

            return;
        }

        _logTextBox.AppendText(text);
    }
}
