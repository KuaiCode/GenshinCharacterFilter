using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace GenshinCharacterFilter.Audio;

/// <summary>
/// Controls Windows Core Audio sessions for a target process.
/// </summary>
public sealed class WindowsAudioMuteService : IAudioMuteService
{
    private readonly string _targetProcessName;
    private readonly TextWriter _log;
    private readonly AudioFilterOptions _options;
    private readonly Dictionary<string, SessionSnapshot> _snapshots = new(StringComparer.Ordinal);

    public WindowsAudioMuteService(string targetProcessName, TextWriter log, AudioFilterOptions? options = null)
    {
        _targetProcessName = NormalizeProcessName(targetProcessName);
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _options = options ?? new AudioFilterOptions();
        _options.Validate();
    }

    /// <summary>
    /// Normalizes a process name for comparison by removing a trailing .exe extension.
    /// </summary>
    public static string NormalizeProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Target process name is required.", nameof(processName));
        }

        string normalized = processName.Trim();

        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Target process name is required.", nameof(processName));
        }

        return normalized;
    }

    /// <inheritdoc />
    public Task MuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();

        IReadOnlyList<MatchedAudioSession> sessions = FindMatchingSessions();

        if (sessions.Count == 0)
        {
            _log.WriteLine($"[REAL AUDIO] No audio sessions found for process '{_targetProcessName}'.");
            return Task.CompletedTask;
        }

        _log.WriteLine($"[REAL AUDIO] Matched {sessions.Count} audio session(s) for process '{_targetProcessName}'.");

        try
        {
            foreach (MatchedAudioSession session in sessions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_snapshots.TryGetValue(session.Key, out SessionSnapshot? snapshot))
                {
                    snapshot = new SessionSnapshot(
                        session.Key,
                        session.ProcessId,
                        session.ProcessName,
                        session.Volume.Mute,
                        session.Volume.Volume);
                    _snapshots[session.Key] = snapshot;
                }

                ApplyFilter(session, snapshot);
            }
        }
        finally
        {
            foreach (MatchedAudioSession session in sessions)
            {
                session.Dispose();
            }
        }

        _log.WriteLine($"[REAL AUDIO] {_options.Mode} requested for {sessions.Count} session(s).");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RestoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();

        if (_snapshots.Count == 0)
        {
            _log.WriteLine($"[REAL AUDIO] Restore skipped; no stored session state for process '{_targetProcessName}'.");
            return Task.CompletedTask;
        }

        IReadOnlyList<MatchedAudioSession> sessions = FindMatchingSessions();
        int restoredCount = 0;

        try
        {
            foreach (SessionSnapshot snapshot in _snapshots.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MatchedAudioSession? session = sessions.FirstOrDefault(candidate => candidate.Key == snapshot.Key);

                if (session is null)
                {
                    _log.WriteLine($"[REAL AUDIO] Session for process '{snapshot.ProcessName}' pid {snapshot.ProcessId} disappeared before restore.");
                    continue;
                }

                session.Volume.Volume = snapshot.Volume;
                session.Volume.Mute = snapshot.Mute;
                restoredCount++;
            }

            _snapshots.Clear();
        }
        finally
        {
            foreach (MatchedAudioSession session in sessions)
            {
                session.Dispose();
            }
        }

        _log.WriteLine($"[REAL AUDIO] Restore requested for {restoredCount} session(s).");
        return Task.CompletedTask;
    }

    private IReadOnlyList<MatchedAudioSession> FindMatchingSessions()
    {
        using MMDeviceEnumerator enumerator = new();
        using MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        SessionCollection sessions = device.AudioSessionManager.Sessions;
        List<MatchedAudioSession> matches = [];

        for (int i = 0; i < sessions.Count; i++)
        {
            AudioSessionControl? session = null;

            try
            {
                session = sessions[i];
                uint processId = session.GetProcessID;
                string? processName = TryGetProcessName(processId);

                if (processName is null ||
                    !string.Equals(processName, _targetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    session.Dispose();
                    continue;
                }

                matches.Add(new MatchedAudioSession(
                    session,
                    session.SimpleAudioVolume,
                    CreateSessionKey(session, processId),
                    processId,
                    processName));
                session = null;
            }
            catch (Exception exception) when (IsRecoverableSessionAccessException(exception))
            {
                session?.Dispose();
                _log.WriteLine($"[REAL AUDIO] Skipped audio session {i}; unable to read session details: {exception.Message}");
            }
        }

        return matches;
    }

    private void ApplyFilter(MatchedAudioSession session, SessionSnapshot snapshot)
    {
        if (_options.Mode == AudioFilterMode.ReduceVolume)
        {
            float reducedVolume = snapshot.Volume * _options.VolumePercent / 100.0f;
            session.Volume.Volume = Math.Clamp(reducedVolume, 0.0f, 1.0f);
            return;
        }

        session.Volume.Mute = true;
    }

    private static string CreateSessionKey(AudioSessionControl session, uint processId)
    {
        string sessionKey = string.IsNullOrWhiteSpace(session.GetSessionInstanceIdentifier)
            ? session.GetSessionIdentifier
            : session.GetSessionInstanceIdentifier;

        return $"{processId}:{sessionKey}";
    }

    private static string? TryGetProcessName(uint processId)
    {
        if (processId == 0 || processId > int.MaxValue)
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return NormalizeProcessName(process.ProcessName);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static bool IsRecoverableSessionAccessException(Exception exception)
    {
        return exception is COMException
            or InvalidOperationException
            or ArgumentException
            or Win32Exception
            or UnauthorizedAccessException;
    }

    private static void EnsureWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Real audio mode requires Windows Core Audio.");
        }
    }

    private sealed record SessionSnapshot(
        string Key,
        uint ProcessId,
        string ProcessName,
        bool Mute,
        float Volume);

    private sealed class MatchedAudioSession : IDisposable
    {
        private readonly AudioSessionControl _session;

        public MatchedAudioSession(
            AudioSessionControl session,
            SimpleAudioVolume volume,
            string key,
            uint processId,
            string processName)
        {
            _session = session;
            Volume = volume;
            Key = key;
            ProcessId = processId;
            ProcessName = processName;
        }

        public SimpleAudioVolume Volume { get; }

        public string Key { get; }

        public uint ProcessId { get; }

        public string ProcessName { get; }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
