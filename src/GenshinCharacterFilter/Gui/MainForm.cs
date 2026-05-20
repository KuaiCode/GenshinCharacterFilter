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
    private readonly Label _statusValueLabel = new();
    private readonly Button _browseConfigButton = new();
    private readonly Button _browseOcrInputButton = new();
    private readonly Button _validateConfigButton = new();
    private readonly Button _printEffectiveConfigButton = new();
    private readonly Button _calibrateOcrRegionButton = new();
    private readonly Button _testOcrOnceButton = new();
    private readonly Button _startDryRunButton = new();
    private readonly Button _startSimulatedAudioButton = new();
    private readonly CheckBox _useFixedImageForDetectionCheckBox = new();
    private readonly CheckBox _runUntilStopCheckBox = new();
    private readonly TextBox _loopCountTextBox = new();
    private readonly TextBox _loopIntervalTextBox = new();
    private readonly TextBox _captureDelayTextBox = new();
    private readonly TextBox _matchThresholdTextBox = new();
    private readonly TextBox _missThresholdTextBox = new();
    private readonly CheckBox _saveDebugImagesCheckBox = new();
    private readonly CheckBox _enableGuardedRealAudioCheckBox = new();
    private readonly Label _guardedRealAudioTargetLabel = new();
    private readonly Label _guardedRealAudioAudioLabel = new();
    private readonly Label _guardedRealAudioStatusLabel = new();
    private readonly Button _startGuardedRealAudioButton = new();
    private readonly Button _stopButton = new();
    private readonly GuiStateController _stateController = new();
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

    private readonly record struct MainFormPlacement(
        Rectangle Bounds,
        Rectangle RestoreBounds,
        Size ClientSize,
        Size MinimumSize,
        FormWindowState WindowState);

    public MainForm(string? initialConfigPath)
    {
        Text = "GenshinCharacterFilter";
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1280, 820);
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
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildMainSplitPanel(initialConfigPath), 0, 1);

        Controls.Add(root);
        _configPathTextBox.TextChanged += (_, _) => RefreshGuardedRealAudioStatus();
        _ocrInputPathTextBox.TextChanged += (_, _) => RefreshGuardedRealAudioStatus();
        _useFixedImageForDetectionCheckBox.CheckedChanged += (_, _) => RefreshGuardedRealAudioStatus();
        ApplyUiState(_stateController.Current);
        RefreshGuardedRealAudioStatus();
    }

    private Control BuildHeaderPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            ColumnCount = 5,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Label titleLabel = new()
        {
            Text = "GenshinCharacterFilter",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 5, 16, 0)
        };

        Label statusLabel = new()
        {
            Text = "Status",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 8, 0)
        };

        _statusValueLabel.AutoSize = true;
        _statusValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusValueLabel.Margin = new Padding(0, 6, 16, 0);

        _stopButton.Text = "Stop";
        _stopButton.AutoSize = false;
        _stopButton.Width = 140;
        _stopButton.Height = 34;
        _stopButton.Enabled = false;
        _stopButton.Margin = new Padding(8, 0, 0, 0);
        _stopButton.Click += (_, _) => CancelCurrentOperation();

        panel.Controls.Add(titleLabel, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = "Explicit local control panel. Real audio remains disabled until guarded confirmation.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 7, 0, 0)
        }, 1, 0);
        panel.Controls.Add(statusLabel, 2, 0);
        panel.Controls.Add(_statusValueLabel, 3, 0);
        panel.Controls.Add(_stopButton, 4, 0);
        return panel;
    }

    private Control BuildMainSplitPanel(string? initialConfigPath)
    {
        SplitContainer splitContainer = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            IsSplitterFixed = false,
            Orientation = Orientation.Vertical,
            SplitterDistance = 520,
            SplitterWidth = 8
        };

        splitContainer.Panel1.Controls.Add(BuildControlTabs(initialConfigPath));
        splitContainer.Panel2.Controls.Add(BuildLogPanel());
        return splitContainer;
    }

    private Control BuildControlTabs(string? initialConfigPath)
    {
        TabControl tabs = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Point(16, 6)
        };

        tabs.TabPages.Add(CreateTabPage("Config", BuildConfigGroup(initialConfigPath)));
        tabs.TabPages.Add(CreateTabPage("OCR", BuildOcrGroup()));
        tabs.TabPages.Add(CreateTabPage("Detection", BuildDetectionGroup()));
        tabs.TabPages.Add(CreateTabPage("Audio", BuildAudioGroup()));
        return tabs;
    }

    private static TabPage CreateTabPage(string title, Control content)
    {
        TabPage tabPage = new(title)
        {
            Padding = new Padding(10)
        };
        tabPage.Controls.Add(content);
        return tabPage;
    }

    private static TableLayoutPanel CreateStackPanel()
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 0,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            AutoScroll = true
        };
    }

    private Control BuildConfigGroup(string? initialConfigPath)
    {
        TableLayoutPanel stack = CreateStackPanel();
        stack.Controls.Add(BuildPathRow(
            "Config file",
            _configPathTextBox,
            _browseConfigButton,
            string.IsNullOrWhiteSpace(initialConfigPath) ? GuiCommandService.GetDefaultConfigPath() : initialConfigPath,
            BrowseConfigFile), 0, 0);
        stack.Controls.Add(BuildConfigActions(), 0, 1);

        GroupBox groupBox = CreateGroupBox("Config", stack);
        return groupBox;
    }

    private Control BuildOcrGroup()
    {
        TableLayoutPanel stack = CreateStackPanel();
        stack.Controls.Add(BuildPathRow(
            "OCR input image",
            _ocrInputPathTextBox,
            _browseOcrInputButton,
            GuiCommandService.GetDefaultOcrInputPath(),
            BrowseOcrInputImage), 0, 0);
        stack.Controls.Add(BuildDetectionInputRow(), 0, 1);
        stack.Controls.Add(BuildOcrOptionsRow(), 0, 2);
        stack.Controls.Add(BuildOcrActions(), 0, 3);

        return CreateGroupBox("OCR", stack);
    }

    private Control BuildDetectionGroup()
    {
        TableLayoutPanel stack = CreateStackPanel();
        stack.Controls.Add(BuildDetectionTuningRow(), 0, 0);
        stack.Controls.Add(BuildDetectionActions(), 0, 1);
        return CreateGroupBox("Detection", stack);
    }

    private Control BuildAudioGroup()
    {
        TableLayoutPanel stack = CreateStackPanel();
        stack.Controls.Add(BuildSimulatedAudioPanel(), 0, 0);
        stack.Controls.Add(BuildGuardedRealAudioPanel(), 0, 1);
        return stack;
    }

    private Control BuildLogPanel()
    {
        GroupBox groupBox = new()
        {
            Text = "Logs",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        _logTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        groupBox.Controls.Add(_logTextBox);
        return groupBox;
    }

    private static GroupBox CreateGroupBox(string title, Control content)
    {
        GroupBox groupBox = new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        groupBox.Controls.Add(content);
        return groupBox;
    }

    private static Control BuildPathRow(
        string label,
        TextBox textBox,
        Button browseButton,
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

        browseButton.Text = "Browse";
        browseButton.Dock = DockStyle.Fill;
        browseButton.Click += browseHandler;

        panel.Controls.Add(pathLabel, 0, 0);
        panel.Controls.Add(textBox, 1, 0);
        panel.Controls.Add(browseButton, 2, 0);
        return panel;
    }

    private Control BuildConfigActions()
    {
        FlowLayoutPanel panel = CreateActionPanel();
        ConfigureButton(_validateConfigButton, "Validate Config", async (_, _) => await RunOperationAsync(ValidateConfigAsync, cancellable: false));
        ConfigureButton(_printEffectiveConfigButton, "Print Effective Config", async (_, _) => await RunOperationAsync(PrintEffectiveConfigAsync, cancellable: false));
        panel.Controls.Add(_validateConfigButton);
        panel.Controls.Add(_printEffectiveConfigButton);
        return panel;
    }

    private Control BuildOcrActions()
    {
        FlowLayoutPanel panel = CreateActionPanel();
        ConfigureButton(_calibrateOcrRegionButton, "Calibrate OCR Region", async (_, _) => await RunOperationAsync(CalibrateOcrRegionAsync, cancellable: false));
        ConfigureButton(_testOcrOnceButton, "Test OCR Once", async (_, _) => await RunOperationAsync(TestOcrOnceAsync, cancellable: true));
        panel.Controls.Add(_calibrateOcrRegionButton);
        panel.Controls.Add(_testOcrOnceButton);
        return panel;
    }

    private Control BuildDetectionActions()
    {
        FlowLayoutPanel panel = CreateActionPanel();
        ConfigureButton(_startDryRunButton, "Start Dry-run Detection", async (_, _) => await RunOperationAsync(StartDryRunDetectionAsync, cancellable: true));
        panel.Controls.Add(_startDryRunButton);
        return panel;
    }

    private Control BuildSimulatedAudioPanel()
    {
        GroupBox groupBox = new()
        {
            Text = "Simulated Audio",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 12)
        };

        FlowLayoutPanel panel = CreateActionPanel();
        ConfigureButton(_startSimulatedAudioButton, "Start Simulated Detection Audio", async (_, _) => await RunOperationAsync(StartSimulatedDetectionAudioAsync, cancellable: true));
        panel.Controls.Add(_startSimulatedAudioButton);
        groupBox.Controls.Add(panel);
        return groupBox;
    }

    private static FlowLayoutPanel CreateActionPanel()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 0)
        };
    }

    private Control BuildDetectionInputRow()
    {
        _useFixedImageForDetectionCheckBox.Text = "Use fixed image for detection loop (debug only)";
        _useFixedImageForDetectionCheckBox.AutoSize = true;
        _useFixedImageForDetectionCheckBox.Checked = false;

        Label helpLabel = new()
        {
            Text = "Unchecked: detection loops capture the target process live each iteration. Checked: dry-run/simulated loops OCR the image path above.",
            AutoSize = true,
            MaximumSize = new Size(760, 0)
        };

        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.Controls.Add(_useFixedImageForDetectionCheckBox);
        panel.Controls.Add(helpLabel);
        return panel;
    }

    private Control BuildOcrOptionsRow()
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };

        _saveDebugImagesCheckBox.Text = "Save debug images";
        _saveDebugImagesCheckBox.AutoSize = true;
        _saveDebugImagesCheckBox.Checked = false;

        Label helpLabel = new()
        {
            Text = "Save debug images writes per-iteration debug files and can slow realtime detection. Leave unchecked for live GUI runs.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = SystemColors.GrayText
        };

        panel.Controls.Add(_saveDebugImagesCheckBox);
        panel.Controls.Add(helpLabel);
        return panel;
    }

    private Control BuildDetectionTuningRow()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            ColumnCount = 8,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        for (int i = 0; i < 8; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(i % 2 == 0 ? SizeType.AutoSize : SizeType.Absolute, i % 2 == 0 ? 0 : 90));
        }

        _runUntilStopCheckBox.Text = "Run until Stop";
        _runUntilStopCheckBox.AutoSize = true;
        _runUntilStopCheckBox.Checked = true;
        _runUntilStopCheckBox.CheckedChanged += (_, _) => UpdateLoopCountInputState();

        _loopCountTextBox.Text = string.Empty;
        _loopCountTextBox.Enabled = false;
        _loopIntervalTextBox.Text = GuiDetectionTuningOptions.DefaultLoopIntervalMs.ToString();
        _captureDelayTextBox.Text = GuiDetectionTuningOptions.DefaultCaptureDelayMs.ToString();
        _matchThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMatchThreshold.ToString();
        _missThresholdTextBox.Text = GuiDetectionTuningOptions.DefaultMissThreshold.ToString();

        Label helpLabel = new()
        {
            Text = "Runtime tuning below overrides config.local.json for this run only and is not saved. Run until Stop checked ignores config LoopCount.",
            AutoSize = true,
            MaximumSize = new Size(980, 0)
        };

        panel.Controls.Add(_runUntilStopCheckBox, 0, 0);
        panel.SetColumnSpan(_runUntilStopCheckBox, 8);
        AddLabeledTuningTextBox(panel, "Loop count", _loopCountTextBox, 0, 1);
        AddLabeledTuningTextBox(panel, "Loop interval ms", _loopIntervalTextBox, 2, 1);
        AddLabeledTuningTextBox(panel, "Capture delay ms", _captureDelayTextBox, 4, 1);
        AddLabeledTuningTextBox(panel, "Match threshold", _matchThresholdTextBox, 0, 2);
        AddLabeledTuningTextBox(panel, "Miss threshold", _missThresholdTextBox, 2, 2);
        panel.Controls.Add(helpLabel, 0, 3);
        panel.SetColumnSpan(helpLabel, 8);
        return panel;
    }

    private static void AddLabeledTuningTextBox(
        TableLayoutPanel panel,
        string label,
        TextBox textBox,
        int column,
        int row)
    {
        Label inputLabel = new()
        {
            Text = label,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 6, 0)
        };
        textBox.Width = 80;
        textBox.Margin = new Padding(0, 0, 12, 4);
        panel.Controls.Add(inputLabel, column, row);
        panel.Controls.Add(textBox, column + 1, row);
    }

    private Control BuildGuardedRealAudioPanel()
    {
        GroupBox groupBox = new()
        {
            Text = "Guarded Real Audio Danger Zone",
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.MistyRose,
            ForeColor = Color.DarkRed,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8)
        };

        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            BackColor = Color.MistyRose
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label warningLabel = new()
        {
            Text = "Warning: guarded real audio controls target process audio only after explicit confirmation. Test reduce mode first; Stop/close will try to restore, but manual mixer recovery may still be needed.",
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            ForeColor = Color.DarkRed,
            Font = new Font(Font, FontStyle.Bold)
        };

        _enableGuardedRealAudioCheckBox.Text = "Enable guarded real audio";
        _enableGuardedRealAudioCheckBox.AutoSize = true;
        _enableGuardedRealAudioCheckBox.CheckedChanged += (_, _) => RefreshGuardedRealAudioStatus();

        _guardedRealAudioTargetLabel.AutoSize = true;
        _guardedRealAudioAudioLabel.AutoSize = true;
        _guardedRealAudioStatusLabel.AutoSize = true;
        _guardedRealAudioStatusLabel.MaximumSize = new Size(760, 0);

        _startGuardedRealAudioButton.Text = "Start Guarded Real Audio";
        _startGuardedRealAudioButton.Width = 220;
        _startGuardedRealAudioButton.Height = 32;
        _startGuardedRealAudioButton.Enabled = false;
        _startGuardedRealAudioButton.Click += async (_, _) => await StartGuardedRealAudioWithConfirmationAsync();

        panel.Controls.Add(warningLabel, 0, 0);
        panel.SetColumnSpan(warningLabel, 2);
        panel.Controls.Add(_enableGuardedRealAudioCheckBox, 0, 1);
        panel.SetColumnSpan(_enableGuardedRealAudioCheckBox, 2);
        panel.Controls.Add(new Label { Text = "Target process", AutoSize = true }, 0, 2);
        panel.Controls.Add(_guardedRealAudioTargetLabel, 1, 2);
        panel.Controls.Add(new Label { Text = "Audio filter", AutoSize = true }, 0, 3);
        panel.Controls.Add(_guardedRealAudioAudioLabel, 1, 3);
        panel.Controls.Add(new Label { Text = "Readiness", AutoSize = true }, 0, 4);
        panel.Controls.Add(_guardedRealAudioStatusLabel, 1, 4);
        panel.Controls.Add(_startGuardedRealAudioButton, 0, 5);
        panel.SetColumnSpan(_startGuardedRealAudioButton, 2);

        groupBox.Controls.Add(panel);
        return groupBox;
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
        MainFormPlacement placement = GetCurrentPlacement();
        try
        {
            await _commandService.CalibrateOcrRegionAsync(GetConfigPath(), CreateLogWriter(), cancellationToken);
        }
        finally
        {
            RestoreMainWindowPlacement(placement, scheduleSecondPass: true);
        }
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
            _useFixedImageForDetectionCheckBox.Checked,
            ParseGuiDetectionTuning(),
            false,
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task StartSimulatedDetectionAudioAsync(CancellationToken cancellationToken)
    {
        await _commandService.RunDetectionLoopAsync(
            GetConfigPath(),
            GetOptionalOcrInputPath(),
            _useFixedImageForDetectionCheckBox.Checked,
            ParseGuiDetectionTuning(),
            true,
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task StartGuardedRealAudioDetectionAsync(CancellationToken cancellationToken)
    {
        await _commandService.RunGuardedRealAudioDetectionAsync(
            GetConfigPath(),
            _useFixedImageForDetectionCheckBox.Checked,
            ParseGuiDetectionTuning(),
            CreateLogWriter(),
            cancellationToken);
    }

    private async Task StartGuardedRealAudioWithConfirmationAsync()
    {
        try
        {
            GuardedRealAudioUiEligibility eligibility = GetGuardedRealAudioEligibility();
            if (!eligibility.CanRequestConfirmation)
            {
                AppendLogLine($"Guarded real audio is not ready: {eligibility.DisabledReason}");
                RefreshGuardedRealAudioStatus();
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "This will control the configured target process audio using stable OCR detection only.\n\n" +
                "Start with reduce-volume mode before using mute.\n" +
                "Stop or closing this window will try to restore audio.\n" +
                "If Windows audio sessions change or restore fails, you may still need to restore the system volume mixer manually.\n\n" +
                "Start guarded real audio now?",
                "Confirm Guarded Real Audio",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => ApplyUiState(uiState)));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            return;
        }

        _statusValueLabel.Text = uiState.RunState.ToString();
        _browseConfigButton.Enabled = uiState.CommandButtonsEnabled;
        _browseOcrInputButton.Enabled = uiState.CommandButtonsEnabled;
        _validateConfigButton.Enabled = uiState.CommandButtonsEnabled;
        _printEffectiveConfigButton.Enabled = uiState.CommandButtonsEnabled;
        _calibrateOcrRegionButton.Enabled = uiState.CommandButtonsEnabled;
        _testOcrOnceButton.Enabled = uiState.CommandButtonsEnabled;
        _startDryRunButton.Enabled = uiState.CommandButtonsEnabled;
        _startSimulatedAudioButton.Enabled = uiState.CommandButtonsEnabled;
        _stopButton.Enabled = uiState.StopButtonEnabled;
        _useFixedImageForDetectionCheckBox.Enabled = uiState.CommandButtonsEnabled;
        _runUntilStopCheckBox.Enabled = uiState.CommandButtonsEnabled;
        UpdateLoopCountInputState(uiState.CommandButtonsEnabled);
        _loopIntervalTextBox.Enabled = uiState.CommandButtonsEnabled;
        _captureDelayTextBox.Enabled = uiState.CommandButtonsEnabled;
        _matchThresholdTextBox.Enabled = uiState.CommandButtonsEnabled;
        _missThresholdTextBox.Enabled = uiState.CommandButtonsEnabled;
        _saveDebugImagesCheckBox.Enabled = uiState.CommandButtonsEnabled;
        _enableGuardedRealAudioCheckBox.Enabled = uiState.CommandButtonsEnabled;
        RefreshGuardedRealAudioStatus();
    }

    private void RefreshGuardedRealAudioStatus()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(RefreshGuardedRealAudioStatus));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            return;
        }

        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath());
        GuardedRealAudioUiEligibility eligibility = new(
            _enableGuardedRealAudioCheckBox.Checked,
            _stateController.OperationActive,
            _useFixedImageForDetectionCheckBox.Checked,
            status.PreflightPassed,
            status.HasOcrRegionSource,
            !string.IsNullOrWhiteSpace(status.TargetProcessName));

        _guardedRealAudioTargetLabel.Text = string.IsNullOrWhiteSpace(status.TargetProcessName)
            ? "(missing)"
            : status.TargetProcessName;
        _guardedRealAudioAudioLabel.Text = $"{status.AudioMode}, {status.VolumePercent}%";
        _guardedRealAudioStatusLabel.Text = eligibility.CanRequestConfirmation
            ? "Ready for confirmation."
            : BuildGuardedRealAudioStatusText(eligibility, status);
        _startGuardedRealAudioButton.Enabled = eligibility.CanRequestConfirmation;
    }

    private GuardedRealAudioUiEligibility GetGuardedRealAudioEligibility()
    {
        GuardedRealAudioStatus status = _commandService.GetGuardedRealAudioStatus(GetConfigPath());
        return new GuardedRealAudioUiEligibility(
            _enableGuardedRealAudioCheckBox.Checked,
            _stateController.OperationActive,
            _useFixedImageForDetectionCheckBox.Checked,
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

    private GuiDetectionTuningInput GetDetectionTuningInput()
    {
        if (!InvokeRequired)
        {
            return new GuiDetectionTuningInput(
                _runUntilStopCheckBox.Checked,
                _loopCountTextBox.Text,
                _loopIntervalTextBox.Text,
                _captureDelayTextBox.Text,
                _matchThresholdTextBox.Text,
                _missThresholdTextBox.Text,
                _saveDebugImagesCheckBox.Checked);
        }

        return (GuiDetectionTuningInput)Invoke(new Func<GuiDetectionTuningInput>(GetDetectionTuningInput));
    }

    private MainFormPlacement GetCurrentPlacement()
    {
        if (!InvokeRequired)
        {
            return new MainFormPlacement(Bounds, RestoreBounds, ClientSize, MinimumSize, WindowState);
        }

        return (MainFormPlacement)Invoke(new Func<MainFormPlacement>(GetCurrentPlacement));
    }

    private void RestoreMainWindowPlacement(MainFormPlacement placement, bool scheduleSecondPass)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => RestoreMainWindowPlacement(placement, scheduleSecondPass)));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            return;
        }

        ApplyMainFormPlacement(placement);
        if (!scheduleSecondPass)
        {
            return;
        }

        BeginInvoke(new Action(() => RestoreMainWindowPlacement(placement, scheduleSecondPass: false)));
    }

    private void ApplyMainFormPlacement(MainFormPlacement placement)
    {
        SuspendLayout();
        try
        {
            AutoScaleMode = AutoScaleMode.None;
            AutoSize = false;
            MinimumSize = placement.MinimumSize;

            FormWindowState restoredState = placement.WindowState == FormWindowState.Minimized
                ? FormWindowState.Normal
                : placement.WindowState;

            WindowState = FormWindowState.Normal;
            if (restoredState == FormWindowState.Maximized)
            {
                if (!placement.RestoreBounds.IsEmpty)
                {
                    Bounds = placement.RestoreBounds;
                }
            }
            else
            {
                Bounds = placement.Bounds;
                ClientSize = placement.ClientSize;
                Bounds = placement.Bounds;
            }

            WindowState = restoredState;
        }
        finally
        {
            ResumeLayout(true);
            PerformLayout();
        }
    }

    private void UpdateLoopCountInputState()
    {
        UpdateLoopCountInputState(_stateController.Current.CommandButtonsEnabled);
    }

    private void UpdateLoopCountInputState(bool commandButtonsEnabled)
    {
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => UpdateLoopCountInputState(commandButtonsEnabled)));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            return;
        }

        _loopCountTextBox.Enabled = commandButtonsEnabled && !_runUntilStopCheckBox.Checked;
    }

    private void BrowseConfigFile(object? sender, EventArgs eventArgs)
    {
        RunBrowseAction("Config browse", () =>
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
        });
    }

    private void BrowseOcrInputImage(object? sender, EventArgs eventArgs)
    {
        RunBrowseAction("OCR input browse", () =>
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
        });
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
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
    }
}
