using System.Drawing;
using System.Windows.Forms;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Captures a target window and opens a minimal selection window for OCR region calibration.
/// </summary>
public sealed class WindowsOcrRegionCalibrator
{
    private readonly IGameWindowCapture _windowCapture;
    private readonly OcrRegionCalibrationFile _calibrationFile;
    private readonly CalibrationRegionPreviewSaver _previewSaver;
    private readonly TextWriter _log;

    public WindowsOcrRegionCalibrator(
        IGameWindowCapture windowCapture,
        TextWriter? log = null,
        OcrRegionCalibrationFile? calibrationFile = null,
        CalibrationRegionPreviewSaver? previewSaver = null)
    {
        _windowCapture = windowCapture;
        _log = log ?? TextWriter.Null;
        _calibrationFile = calibrationFile ?? new OcrRegionCalibrationFile();
        _previewSaver = previewSaver ?? new CalibrationRegionPreviewSaver();
    }

    /// <summary>
    /// Runs one manual OCR region calibration session.
    /// </summary>
    public async Task<OcrRegionCalibrationResult> CalibrateAsync(
        OcrRegionCalibrationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        cancellationToken.ThrowIfCancellationRequested();
        _log.WriteLine("OCR region calibration mode; this run does not run OCR or control real system audio.");

        string screenshotPath = await _windowCapture.CaptureOnceAsync(options.ToWindowCaptureOptions(), cancellationToken);
        return CalibrateFromScreenshot(options, screenshotPath, cancellationToken);
    }

    public OcrRegionCalibrationResult CalibrateFromScreenshot(
        OcrRegionCalibrationOptions options,
        string screenshotPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (string.IsNullOrWhiteSpace(screenshotPath))
        {
            throw new ArgumentException("Calibration screenshot path cannot be empty.", nameof(screenshotPath));
        }

        cancellationToken.ThrowIfCancellationRequested();
        _log.WriteLine($"Calibration screenshot captured: {screenshotPath}");

        using Bitmap screenshot = new(screenshotPath);
        OcrRegion? selectedRegion = SelectRegion(screenshot);
        if (selectedRegion is null)
        {
            _log.WriteLine("Calibration cancelled.");
            throw new CalibrationException("OCR region calibration was cancelled.");
        }

        selectedRegion.Value.ValidateWithin(screenshot.Width, screenshot.Height);
        OcrRegionCalibrationResult result = OcrRegionCalibrationResult.FromPixelRegion(
            screenshot.Width,
            screenshot.Height,
            selectedRegion.Value,
            WindowCaptureOptions.NormalizeProcessName(options.TargetProcessName));

        string outputPath = options.GetCalibrationOutputPath();
        _calibrationFile.Save(result, outputPath);
        _log.WriteLine($"Calibration JSON saved: {outputPath}");
        TrySaveRegionPreview(screenshot, result.RegionPixels);
        return result;
    }

    private void TrySaveRegionPreview(Bitmap screenshot, OcrRegion region)
    {
        try
        {
            string previewPath = _previewSaver.SavePreview(screenshot, region);
            _log.WriteLine($"Calibration region preview saved: {previewPath}");
        }
        catch (Exception exception) when (exception is CalibrationException or ArgumentException or ArgumentOutOfRangeException or IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        {
            _log.WriteLine($"Warning: Calibration region preview was not saved: {exception.Message}");
        }
    }

    private static OcrRegion? SelectRegion(Bitmap screenshot)
    {
        OcrRegion? selectedRegion = null;
        Exception? uiException = null;

        Thread thread = new(() =>
        {
            try
            {
                TrySetHighDpiMode();
                Application.EnableVisualStyles();
                TrySetCompatibleTextRenderingDefault();
                using Bitmap uiBitmap = new(screenshot);
                using CalibrationSelectionForm form = new(uiBitmap);
                DialogResult result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    selectedRegion = form.SelectedRegion;
                }
            }
            catch (Exception exception)
            {
                uiException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (uiException is not null)
        {
            throw new CalibrationException($"Calibration window failed: {uiException.Message}", uiException);
        }

        return selectedRegion;
    }

    private static void TrySetHighDpiMode()
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        }
        catch (InvalidOperationException)
        {
            // GUI may have already initialized DPI mode; reuse the current process setting.
        }
    }

    private static void TrySetCompatibleTextRenderingDefault()
    {
        try
        {
            Application.SetCompatibleTextRenderingDefault(false);
        }
        catch (InvalidOperationException)
        {
            // The main GUI may have created controls already; keep the existing process setting.
        }
    }
}
