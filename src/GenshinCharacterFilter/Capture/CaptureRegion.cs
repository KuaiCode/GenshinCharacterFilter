namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Describes a rectangular capture region relative to the target window.
/// </summary>
public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Validates that this region fits within the provided window bounds.
    /// </summary>
    public void ValidateWithin(int windowWidth, int windowHeight)
    {
        if (windowWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowWidth), "Window width must be positive.");
        }

        if (windowHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowHeight), "Window height must be positive.");
        }

        if (X < 0 || Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(X), "Capture region coordinates cannot be negative.");
        }

        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "Capture region size must be positive.");
        }

        if ((long)X + Width > windowWidth || (long)Y + Height > windowHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "Capture region must fit within the target window.");
        }
    }
}
