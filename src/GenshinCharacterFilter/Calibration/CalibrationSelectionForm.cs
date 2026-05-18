using System.Drawing;
using System.Windows.Forms;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Calibration;

internal sealed class CalibrationSelectionForm : Form
{
    private const double InitialWorkingAreaRatio = 0.9;

    private readonly Image _image;
    private bool _dragging;
    private Point _dragStart;
    private Point _dragEnd;

    public CalibrationSelectionForm(Image image)
    {
        _image = image;

        Text = "OCR Region Calibration - drag to select, Enter to save, Esc to cancel";
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        DoubleBuffered = true;
        KeyPreview = true;
        MinimumSize = new Size(640, 420);
        ClientSize = CalculateInitialClientSize(image.Size, Screen.FromPoint(Cursor.Position).WorkingArea);
        StartPosition = FormStartPosition.CenterScreen;
    }

    public OcrRegion? SelectedRegion { get; private set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Rectangle displayRectangle = GetImageDisplayRectangle(ClientSize, _image.Size);
        e.Graphics.Clear(Color.Black);
        e.Graphics.DrawImage(_image, displayRectangle);

        if (TryGetCurrentRegion(out OcrRegion region))
        {
            Rectangle displaySelection = ImageToDisplay(region, displayRectangle, _image.Size);
            using Pen pen = new(Color.Lime, 2);
            e.Graphics.DrawRectangle(pen, displaySelection);
        }

        DrawStatus(e.Graphics, displayRectangle);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Rectangle displayRectangle = GetImageDisplayRectangle(ClientSize, _image.Size);
        if (!displayRectangle.Contains(e.Location))
        {
            return;
        }

        _dragging = true;
        _dragStart = MapClientToImage(e.Location, displayRectangle);
        _dragEnd = _dragStart;
        SelectedRegion = null;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            return;
        }

        _dragEnd = MapClientToImage(e.Location, GetImageDisplayRectangle(ClientSize, _image.Size));
        UpdateSelectedRegion();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_dragging)
        {
            return;
        }

        _dragging = false;
        _dragEnd = MapClientToImage(e.Location, GetImageDisplayRectangle(ClientSize, _image.Size));
        UpdateSelectedRegion();
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            if (SelectedRegion is null)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void UpdateSelectedRegion()
    {
        SelectedRegion = TryGetCurrentRegion(out OcrRegion region)
            ? region
            : null;
    }

    private bool TryGetCurrentRegion(out OcrRegion region)
    {
        int x = Math.Min(_dragStart.X, _dragEnd.X);
        int y = Math.Min(_dragStart.Y, _dragEnd.Y);
        int width = Math.Abs(_dragEnd.X - _dragStart.X);
        int height = Math.Abs(_dragEnd.Y - _dragStart.Y);
        region = new OcrRegion(x, y, width, height);

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        region.ValidateWithin(_image.Width, _image.Height);
        return true;
    }

    private Point MapClientToImage(Point clientPoint, Rectangle displayRectangle)
    {
        int clampedX = Math.Clamp(clientPoint.X, displayRectangle.Left, displayRectangle.Right - 1);
        int clampedY = Math.Clamp(clientPoint.Y, displayRectangle.Top, displayRectangle.Bottom - 1);

        // 图片可能按比例缩放显示；鼠标坐标必须反算回原图像素坐标。
        double xRatio = (clampedX - displayRectangle.Left) / (double)displayRectangle.Width;
        double yRatio = (clampedY - displayRectangle.Top) / (double)displayRectangle.Height;
        int imageX = Math.Clamp((int)Math.Floor(xRatio * _image.Width), 0, _image.Width - 1);
        int imageY = Math.Clamp((int)Math.Floor(yRatio * _image.Height), 0, _image.Height - 1);
        return new Point(imageX, imageY);
    }

    private static Rectangle GetImageDisplayRectangle(Size clientSize, Size imageSize)
    {
        double scale = Math.Min(
            clientSize.Width / (double)imageSize.Width,
            clientSize.Height / (double)imageSize.Height);
        int width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        int height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        int x = (clientSize.Width - width) / 2;
        int y = (clientSize.Height - height) / 2;
        return new Rectangle(x, y, width, height);
    }

    private static Rectangle ImageToDisplay(OcrRegion region, Rectangle displayRectangle, Size imageSize)
    {
        double scaleX = displayRectangle.Width / (double)imageSize.Width;
        double scaleY = displayRectangle.Height / (double)imageSize.Height;
        return new Rectangle(
            displayRectangle.Left + (int)Math.Round(region.X * scaleX),
            displayRectangle.Top + (int)Math.Round(region.Y * scaleY),
            Math.Max(1, (int)Math.Round(region.Width * scaleX)),
            Math.Max(1, (int)Math.Round(region.Height * scaleY)));
    }

    private static Size CalculateInitialClientSize(Size imageSize, Rectangle workingArea)
    {
        int maxWidth = Math.Max(640, (int)Math.Round(workingArea.Width * InitialWorkingAreaRatio));
        int maxHeight = Math.Max(420, (int)Math.Round(workingArea.Height * InitialWorkingAreaRatio));
        double scale = Math.Min(maxWidth / (double)imageSize.Width, maxHeight / (double)imageSize.Height);
        return new Size(
            Math.Max(640, (int)Math.Round(imageSize.Width * scale)),
            Math.Max(420, (int)Math.Round(imageSize.Height * scale)));
    }

    private void DrawStatus(Graphics graphics, Rectangle displayRectangle)
    {
        string regionText = SelectedRegion is null
            ? "Drag to select OCR speaker-name region. Enter saves, Esc cancels."
            : $"Selected: x={SelectedRegion.Value.X}, y={SelectedRegion.Value.Y}, width={SelectedRegion.Value.Width}, height={SelectedRegion.Value.Height}. Enter saves, Esc cancels.";
        using Font font = new(FontFamily.GenericSansSerif, 10);
        SizeF textSize = graphics.MeasureString(regionText, font);
        RectangleF background = new(
            displayRectangle.Left + 8,
            displayRectangle.Top + 8,
            Math.Min(textSize.Width + 16, displayRectangle.Width - 16),
            textSize.Height + 12);
        using SolidBrush backgroundBrush = new(Color.FromArgb(190, Color.Black));
        using SolidBrush textBrush = new(Color.White);
        graphics.FillRectangle(backgroundBrush, background);
        graphics.DrawString(regionText, font, textBrush, background.Left + 8, background.Top + 6);
    }
}
