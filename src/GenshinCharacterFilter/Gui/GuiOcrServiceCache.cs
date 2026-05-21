using System.Diagnostics;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Keeps the selected OCR backend alive across GUI Start/Stop cycles.
/// </summary>
public sealed class GuiOcrServiceCache : IDisposable
{
    private IOcrService? _service;
    private OcrEngine? _engine;

    public IOcrService Get(OcrEngine engine)
    {
        if (_service is not null && _engine == engine)
        {
            return _service;
        }

        DisposeCurrentService();
        _service = OcrServiceFactory.Create(engine);
        _engine = engine;
        return _service;
    }

    public bool IsWarm(OcrEngine engine)
    {
        if (_service is null || _engine != engine)
        {
            return false;
        }

        return _service is IOcrBackendWarmup warmup
            ? warmup.IsWarm
            : true;
    }

    public async Task<GuiOcrWarmupResult> WarmUpAsync(
        OcrEngine engine,
        OcrOptions options,
        CancellationToken cancellationToken)
    {
        IOcrService service = Get(engine);
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (service is IOcrBackendWarmup warmup)
        {
            await warmup.WarmUpAsync(options, cancellationToken);
        }

        stopwatch.Stop();
        return new GuiOcrWarmupResult(engine, IsWarm(engine), stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        DisposeCurrentService();
    }

    private void DisposeCurrentService()
    {
        if (_service is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _service = null;
        _engine = null;
    }
}
