using PaddleOCRSharp;
using System.Runtime.InteropServices;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Runs OCR through the in-process PaddleOCRSharp engine and reuses the loaded model.
/// </summary>
public sealed class PaddleOcrLocalService : IOcrService, IOcrBackendWarmup, IDisposable
{
    private readonly SemaphoreSlim _engineLock = new(1, 1);
    private PaddleOCREngine? _engine;
    private PaddleRuntimeKey? _runtimeKey;
    private bool _disposed;

    public bool IsInitialized => _engine is not null;

    public bool IsWarm => IsInitialized;

    public async Task WarmUpAsync(OcrOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (options.OcrEngine != OcrEngine.PaddleOcrLocal)
        {
            throw new OcrException($"OCR engine '{options.OcrEngine}' is not supported by {nameof(PaddleOcrLocalService)}.");
        }

        await GetEngineAsync(options, cancellationToken);
    }

    public async Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (options.OcrEngine != OcrEngine.PaddleOcrLocal)
        {
            throw new OcrException($"OCR engine '{options.OcrEngine}' is not supported by {nameof(PaddleOcrLocalService)}.");
        }

        PaddleOCREngine engine = await GetEngineAsync(options, cancellationToken);
        try
        {
            OCRResult result = await Task.Run(
                () => engine.DetectText(options.GetFullInputImagePath()),
                cancellationToken);
            return new OcrResult(result.Text ?? string.Empty, "PaddleOcrLocal", options.GetFullInputImagePath());
        }
        catch (DllNotFoundException exception)
        {
            throw CreateInitializationException("native DLL missing", options, exception);
        }
        catch (BadImageFormatException exception)
        {
            throw CreateInitializationException("unsupported architecture or invalid native DLL", options, exception);
        }
        catch (TypeInitializationException exception)
        {
            throw CreateInitializationException("PaddleOCR runtime initialization failed", options, exception);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ExternalException)
        {
            throw new OcrException($"PaddleOCR local OCR failed: {exception.Message}", exception);
        }
    }

    public static void ValidateRuntimeOptions(OcrOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.PaddleRuntimeDirectory))
        {
            string runtimeDirectory = options.PaddleRuntimeDirectory.Trim();
            if (!Directory.Exists(runtimeDirectory))
            {
                throw new OcrException($"PaddleOCR runtime directory was not found: {runtimeDirectory}");
            }

            string paddleOcrDllPath = Path.Combine(runtimeDirectory, "PaddleOCR.dll");
            if (!File.Exists(paddleOcrDllPath))
            {
                throw new OcrException($"PaddleOCR native runtime is missing '{paddleOcrDllPath}'. Check PaddleRuntimeDirectory or remove it to use the bundled runtime.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.PaddleModelDirectory))
        {
            string modelDirectory = options.PaddleModelDirectory.Trim();
            if (!Directory.Exists(modelDirectory))
            {
                throw new OcrException($"PaddleOCR model directory was not found: {modelDirectory}");
            }

            string[] requiredDirectories =
            [
                "det",
                "cls",
                "rec"
            ];
            foreach (string directory in requiredDirectories)
            {
                string path = Path.Combine(modelDirectory, directory);
                if (!Directory.Exists(path))
                {
                    throw new OcrException($"PaddleOCR model directory is missing '{path}'. Expected det/cls/rec model subdirectories or leave PaddleModelDirectory empty to use bundled models.");
                }
            }
        }
    }

    private async Task<PaddleOCREngine> GetEngineAsync(OcrOptions options, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        PaddleRuntimeKey requestedKey = PaddleRuntimeKey.From(options);
        if (_engine is not null && _runtimeKey == requestedKey)
        {
            return _engine;
        }

        await _engineLock.WaitAsync(cancellationToken);
        try
        {
            if (_engine is not null && _runtimeKey == requestedKey)
            {
                return _engine;
            }

            ValidateRuntimeOptions(options);
            _engine?.Dispose();
            _engine = CreateEngine(options);
            _runtimeKey = requestedKey;
            return _engine;
        }
        catch (DllNotFoundException exception)
        {
            throw CreateInitializationException("native DLL missing", options, exception);
        }
        catch (BadImageFormatException exception)
        {
            throw CreateInitializationException("unsupported architecture or invalid native DLL", options, exception);
        }
        catch (TypeInitializationException exception)
        {
            throw CreateInitializationException("PaddleOCR runtime initialization failed", options, exception);
        }
        finally
        {
            _engineLock.Release();
        }
    }

    private static PaddleOCREngine CreateEngine(OcrOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PaddleRuntimeDirectory))
        {
            EngineBase.PaddleOCRdllPath = Path.Combine(options.PaddleRuntimeDirectory.Trim(), "PaddleOCR.dll");
        }

        OCRModelConfig? modelConfig = string.IsNullOrWhiteSpace(options.PaddleModelDirectory)
            ? null
            : CreateModelConfig(options.PaddleModelDirectory.Trim());
        OCRParameter parameter = new()
        {
            cls = false,
            rec = true,
            det = true,
            enable_mkldnn = true
        };
        return modelConfig is null
            ? new PaddleOCREngine(OCRModelConfig.Default, parameter)
            : new PaddleOCREngine(modelConfig, parameter);
    }

    private static OCRModelConfig CreateModelConfig(string modelDirectory)
    {
        return new OCRModelConfig(
            Path.Combine(modelDirectory, "det"),
            Path.Combine(modelDirectory, "cls"),
            Path.Combine(modelDirectory, "rec"),
            Path.Combine(modelDirectory, "ppocr_keys.txt"));
    }

    private static OcrException CreateInitializationException(string reason, OcrOptions options, Exception innerException)
    {
        string modelDirectory = string.IsNullOrWhiteSpace(options.PaddleModelDirectory)
            ? "bundled PaddleOCRSharp models"
            : options.PaddleModelDirectory.Trim();
        string runtimeDirectory = string.IsNullOrWhiteSpace(options.PaddleRuntimeDirectory)
            ? "bundled PaddleOCRSharp runtime"
            : options.PaddleRuntimeDirectory.Trim();
        return new OcrException(
            $"PaddleOCR local backend initialization failed ({reason}). Model: {modelDirectory}; runtime: {runtimeDirectory}. " +
            "Check native DLL/model files, Windows x64 compatibility, or switch Ocr.Engine back to TesseractCli.",
            innerException);
    }

    public void Dispose()
    {
        _disposed = true;
        _engine?.Dispose();
        _engineLock.Dispose();
    }

    private sealed record PaddleRuntimeKey(string? ModelDirectory, string? RuntimeDirectory)
    {
        public static PaddleRuntimeKey From(OcrOptions options)
        {
            return new PaddleRuntimeKey(
                string.IsNullOrWhiteSpace(options.PaddleModelDirectory)
                    ? null
                    : Path.GetFullPath(options.PaddleModelDirectory.Trim()),
                string.IsNullOrWhiteSpace(options.PaddleRuntimeDirectory)
                    ? null
                    : Path.GetFullPath(options.PaddleRuntimeDirectory.Trim()));
        }
    }
}
