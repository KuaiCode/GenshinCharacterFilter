using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Coordination;
using GenshinCharacterFilter.Speakers;

AppSettings settings;

try
{
    AppCommandLineOptions commandLineOptions = AppCommandLineOptions.Parse(args);
    AppSettingsLoader settingsLoader = new();
    AppSettings loadedSettings = commandLineOptions.ConfigPath is null
        ? settingsLoader.LoadDefault()
        : settingsLoader.LoadFromFile(commandLineOptions.ConfigPath);

    settings = commandLineOptions.ApplyOverrides(loadedSettings);
}
catch (Exception exception) when (exception is AppSettingsException or ArgumentException)
{
    Console.Error.WriteLine($"Configuration error: {exception.Message}");
    return;
}

using CancellationTokenSource appCancellation = new();
ManualSpeakerDetector speakerDetector = new();
IAudioMuteService audioMuteService = settings.RealAudioEnabled
    ? new WindowsAudioMuteService(settings.TargetProcessName, Console.Out, settings.AudioFilter)
    : new LoggingAudioMuteService(Console.Out, settings.AudioFilter);
MuteCoordinator coordinator = new(
    speakerDetector,
    audioMuteService,
    new MuteCoordinatorOptions
    {
        TargetSpeakers = new HashSet<string>(settings.TargetSpeakers)
    });

Console.WriteLine("GenshinCharacterFilter v0.2 Local JSON Configuration");
Console.WriteLine(settings.RealAudioEnabled
    ? $"REAL audio mode enabled for process '{settings.TargetProcessName}'."
    : "Simulation mode; this run does not control real system audio.");
Console.WriteLine($"Audio mode: {settings.AudioFilter.Mode}, volume percent: {settings.AudioFilter.VolumePercent}");
Console.WriteLine($"Target speakers: {string.Join(", ", settings.TargetSpeakers)}");
Console.WriteLine("Enter a speaker name, blank/unknown for no target speaker, or q/quit/exit to leave.");

try
{
    while (true)
    {
        Console.Write("speaker> ");
        string? input = Console.ReadLine();

        if (input is null || IsExitCommand(input))
        {
            break;
        }

        string? speaker = string.Equals(input.Trim(), "unknown", StringComparison.OrdinalIgnoreCase)
            ? null
            : input;

        speakerDetector.SetSpeaker(speaker);
        await coordinator.TickAsync(appCancellation.Token);
        Console.WriteLine($"State: {coordinator.State}");
    }
}
finally
{
    Console.WriteLine("Exiting; attempting restore.");
    await coordinator.RestoreForShutdownAsync(CancellationToken.None);
    Console.WriteLine($"State: {coordinator.State}");
}

static bool IsExitCommand(string input)
{
    string command = input.Trim();

    return string.Equals(command, "q", StringComparison.OrdinalIgnoreCase)
        || string.Equals(command, "quit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase);
}
