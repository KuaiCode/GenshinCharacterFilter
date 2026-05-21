using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class AppPreflightValidatorTests
{
    [Fact]
    public void ValidateConfig_PassesForExistingRegionConfigAndDefaultTesseract()
    {
        AppSettings settings = CreateSettings();
        using TempFile regionConfig = TempFile.Create("json");
        settings.Ocr.RegionConfigPath = regionConfig.Path;
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--validate-config"]);
        AppPreflightValidator validator = new(fileExists: File.Exists, commandExists: _ => false);

        RuntimePreflightResult result = validator.Validate(settings, options, AppPreflightMode.ValidateConfig);

        Assert.True(result.Passed);
    }

    [Fact]
    public void ValidateConfig_PassesForConfigExample()
    {
        string configPath = Path.Combine(FindRepositoryRoot(), "config.example.json");
        AppSettings settings = new AppSettingsLoader().LoadFromFile(configPath);
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--config", configPath, "--validate-config"]);
        AppPreflightValidator validator = new(fileExists: File.Exists, commandExists: _ => false);

        RuntimePreflightResult result = validator.Validate(settings, options, AppPreflightMode.ValidateConfig);

        Assert.True(result.Passed);
    }

    [Fact]
    public void ValidateConfig_ReportsMissingRegionConfig()
    {
        AppSettings settings = CreateSettings();
        settings.Ocr.RegionConfigPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--validate-config"]);
        AppPreflightValidator validator = new(fileExists: _ => false, commandExists: _ => true);

        RuntimePreflightResult result = validator.Validate(settings, options, AppPreflightMode.ValidateConfig);

        RuntimePreflightIssue issue = Assert.Single(result.Issues);
        Assert.Equal("OCR preflight error", issue.Category);
        Assert.Contains("OCR region config", issue.Message);
    }

    [Fact]
    public void ValidateConfig_ReportsMissingExplicitTesseractPath()
    {
        AppSettings settings = CreateSettings();
        settings.Ocr.TesseractExecutablePath = @"C:\Missing\tesseract.exe";
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--validate-config"]);
        AppPreflightValidator validator = new(fileExists: _ => false, commandExists: _ => true);

        RuntimePreflightResult result = validator.Validate(settings, options, AppPreflightMode.ValidateConfig);

        RuntimePreflightIssue issue = Assert.Single(result.Issues);
        Assert.Equal("OCR preflight error", issue.Category);
        Assert.Contains("Tesseract executable", issue.Message);
    }

    [Fact]
    public void Validate_OcrOnceReportsMissingInputImage()
    {
        AppSettings settings = CreateSettings();
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--ocr-once", "--ocr-input", "missing.png"]);
        AppPreflightValidator validator = new(fileExists: _ => false, commandExists: _ => true);

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Message.Contains("OCR input image"));
    }

    [Fact]
    public void Validate_OcrRequestReportsMissingTesseractOnPath()
    {
        AppSettings settings = CreateSettings();
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--ocr-once", "--ocr-input", "input.png"]);
        AppPreflightValidator validator = new(
            fileExists: path => path == "input.png",
            commandExists: _ => false);

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Message.Contains("Tesseract executable"));
    }

    [Fact]
    public void Validate_PaddleOcrReportsMissingRuntimeDirectory()
    {
        AppSettings settings = CreateSettings();
        settings.Ocr.Engine = OcrEngine.PaddleOcrLocal;
        settings.Ocr.PaddleRuntimeDirectory = @"C:\Missing\PaddleRuntime";
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--ocr-once", "--ocr-input", "input.png"]);
        AppPreflightValidator validator = new(fileExists: path => path == "input.png");

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Message.Contains("PaddleOCR runtime directory"));
    }

    [Fact]
    public void Validate_PaddleOcrReportsMissingModelDirectory()
    {
        AppSettings settings = CreateSettings();
        settings.Ocr.Engine = OcrEngine.PaddleOcrLocal;
        settings.Ocr.PaddleModelDirectory = @"C:\Missing\PaddleModels";
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--ocr-once", "--ocr-input", "input.png"]);
        AppPreflightValidator validator = new(fileExists: path => path == "input.png");

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Message.Contains("PaddleOCR model directory"));
    }

    [Fact]
    public void Validate_WindowDetectLoopReportsMissingTargetProcess()
    {
        AppSettings settings = CreateSettings();
        settings.TargetProcessName = "notepad";
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--detect-loop", "--process", "notepad"]);
        AppPreflightValidator validator = new(processExists: _ => false);

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Category == "Capture preflight error");
    }

    [Fact]
    public void Validate_GuardedRealAudioReportsMissingRegionSource()
    {
        AppSettings settings = CreateSettings();
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--real-audio", "--allow-real-audio-from-detection", "--ocr-input", "input.png"]);
        AppPreflightValidator validator = new(
            fileExists: path => path == "input.png",
            commandExists: _ => true);

        RuntimePreflightResult result = validator.Validate(settings, options);

        Assert.Contains(result.Issues, issue => issue.Category == "Audio safety error");
    }

    private static AppSettings CreateSettings()
    {
        return new AppSettings
        {
            TargetSpeakers = ["Wanderer"],
            Ocr = new AppOcrSettings
            {
                TesseractExecutablePath = OcrOptions.DefaultTesseractExecutablePath
            }
        };
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GenshinCharacterFilter.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFile Create(string extension)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.{extension}");
            File.WriteAllText(path, "{}");
            return new TempFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
