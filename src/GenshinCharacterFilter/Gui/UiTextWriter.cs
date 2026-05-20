using System.Text;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Forwards TextWriter output to a UI log surface.
/// </summary>
public sealed class UiTextWriter : TextWriter
{
    private readonly Action<string> _write;

    public UiTextWriter(Action<string> write)
    {
        _write = write;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        _write(value.ToString());
    }

    public override void Write(string? value)
    {
        if (value is not null)
        {
            _write(value);
        }
    }

    public override void WriteLine(string? value)
    {
        _write((value ?? string.Empty) + Environment.NewLine);
    }
}
