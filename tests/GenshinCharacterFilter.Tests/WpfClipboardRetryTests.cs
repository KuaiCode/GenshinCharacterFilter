using System.Runtime.InteropServices;
using GenshinCharacterFilter.Wpf;

namespace GenshinCharacterFilter.Tests;

public sealed class WpfClipboardRetryTests
{
    [Fact]
    public async Task TrySetTextAsync_RetriesBusyClipboardAndSucceeds()
    {
        int attempts = 0;
        List<TimeSpan> delays = [];

        bool copied = await WpfClipboardRetry.TrySetTextAsync(
            "log",
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new COMException("Clipboard busy");
                }
            },
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        Assert.True(copied);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150)], delays);
    }

    [Fact]
    public async Task TrySetTextAsync_ReturnsFalseWhenClipboardStaysBusy()
    {
        int attempts = 0;

        bool copied = await WpfClipboardRetry.TrySetTextAsync(
            "log",
            _ =>
            {
                attempts++;
                throw new COMException("Clipboard busy");
            },
            (_, _) => Task.CompletedTask);

        Assert.False(copied);
        Assert.Equal(3, attempts);
    }
}
