using System.Diagnostics;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Keeps the selected OCR backend alive across GUI Start/Stop cycles.
/// </summary>
public sealed class GuiOcrServiceCache : IDisposable
{
    private readonly Func<OcrEngine, IOcrService> _serviceFactory;
    private readonly Dictionary<GuiOcrBackendCacheKey, IOcrService> _services = [];

    public GuiOcrServiceCache()
        : this(OcrServiceFactory.Create)
    {
    }

    public GuiOcrServiceCache(Func<OcrEngine, IOcrService> serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    public IOcrService Get(GuiOcrBackendCacheKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_services.TryGetValue(key, out IOcrService? service))
        {
            return service;
        }

        service = _serviceFactory(key.Engine);
        _services.Add(key, service);
        return service;
    }

    public bool IsWarm(GuiOcrBackendCacheKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_services.TryGetValue(key, out IOcrService? service))
        {
            return false;
        }

        return service is IOcrBackendWarmup warmup
            ? warmup.IsWarm
            : true;
    }

    public async Task<GuiOcrWarmupResult> WarmUpAsync(
        GuiOcrBackendCacheKey key,
        OcrOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        IOcrService service = Get(key);
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (service is IOcrBackendWarmup warmup)
        {
            await warmup.WarmUpAsync(options, cancellationToken);
        }

        stopwatch.Stop();
        return new GuiOcrWarmupResult(key.Engine, IsWarm(key), stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        foreach (IOcrService service in _services.Values)
        {
            if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _services.Clear();
    }
}
