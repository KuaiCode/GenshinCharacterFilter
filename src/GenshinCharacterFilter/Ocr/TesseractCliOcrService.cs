using System.ComponentModel;
using System.Diagnostics;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Runs OCR by invoking an installed tesseract CLI executable.
/// </summary>
public sealed class TesseractCliOcrService : IOcrService
{
    public async Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (options.OcrEngine != OcrEngine.TesseractCli)
        {
            throw new OcrException($"OCR engine '{options.OcrEngine}' is not supported by {nameof(TesseractCliOcrService)}.");
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = options.TesseractExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in BuildArguments(options))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                throw new OcrException($"Could not start tesseract executable '{options.TesseractExecutablePath}'.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new OcrException(
                $"Tesseract executable '{options.TesseractExecutablePath}' was not found or could not be started. Install Tesseract or pass --tesseract-path <path>.",
                exception);
        }

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消 OCR 时尽量结束外部进程，避免 tesseract CLI 残留在后台。
            TryKill(process);
            throw;
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            string details = string.IsNullOrWhiteSpace(stderr)
                ? "No stderr output was produced."
                : stderr.Trim();
            throw new OcrException($"Tesseract OCR failed with exit code {process.ExitCode}. {details}");
        }

        return new OcrResult(stdout, "TesseractCli", options.GetFullInputImagePath());
    }

    /// <summary>
    /// Builds tesseract CLI arguments for pure validation and tests.
    /// </summary>
    public static IReadOnlyList<string> BuildArguments(OcrOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        return
        [
            options.GetFullInputImagePath(),
            "stdout",
            "-l",
            options.Language.Trim(),
            "--psm",
            options.PageSegmentationMode.ToString()
        ];
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }
}
