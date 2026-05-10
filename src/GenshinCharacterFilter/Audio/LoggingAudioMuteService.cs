namespace GenshinCharacterFilter.Audio;

/// <summary>
/// Logs simulated mute and restore requests without changing system audio.
/// </summary>
public sealed class LoggingAudioMuteService : IAudioMuteService
{
    private readonly TextWriter _writer;
    private readonly AudioFilterOptions _options;

    /// <summary>
    /// Creates a simulated audio service that writes requests to the supplied writer.
    /// </summary>
    public LoggingAudioMuteService(TextWriter writer, AudioFilterOptions? options = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _options = options ?? new AudioFilterOptions();
        _options.Validate();
    }

    /// <inheritdoc />
    public Task MuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_options.Mode == AudioFilterMode.ReduceVolume)
        {
            _writer.WriteLine($"[SIMULATED] Reduce volume to {_options.VolumePercent}% requested");
        }
        else
        {
            _writer.WriteLine("[SIMULATED] Mute requested");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RestoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _writer.WriteLine("[SIMULATED] Restore requested");
        return Task.CompletedTask;
    }
}
