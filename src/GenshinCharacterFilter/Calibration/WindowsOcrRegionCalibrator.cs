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
    private readonly TextWriter _log;

    public WindowsOcrRegionCalibrator(
        IGameWindowCapture windowCapture,
        TextWriter? log = null,
        OcrRegionCalibrationFile? calibrationFile = null)
    {
        _windowCapture = windowCapture;
        _log = log ?? TextWriter.Null;
        _calibrationFile = calibrationFile ?? new OcrRegionCalibrationFile();
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
        return result;
    }

    private static OcrRegion? SelectRegion(Bitmap screenshot)
    {
        OcrRegion? selectedRegion = null;
        Exception? uiException = null;

        Thread thread = new(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
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
}
