using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using pixeledit2.Models;
using pixeledit2.ViewModels;

namespace pixeledit2.Views;

public class PixelCanvas : Control
{
    public static readonly StyledProperty<SpriteModel> CurrentSpriteProperty =
        AvaloniaProperty.Register<PixelCanvas, SpriteModel>(nameof(CurrentSprite));

    public static readonly StyledProperty<bool> IsPreviewProperty =
        AvaloniaProperty.Register<PixelCanvas, bool>(nameof(IsPreview));

    public SpriteModel CurrentSprite
    {
        get => GetValue(CurrentSpriteProperty);
        set => SetValue(CurrentSpriteProperty, value);
    }

    public bool IsPreview
    {
        get => GetValue(IsPreviewProperty);
        set => SetValue(IsPreviewProperty, value);
    }

    private const int CellSize = 16;
    private bool _isDrawing;
    private (int x, int y)? _lastToggledPixel;

    static PixelCanvas()
    {
        AffectsRender<PixelCanvas>(CurrentSpriteProperty);
        AffectsMeasure<PixelCanvas>(CurrentSpriteProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (CurrentSprite == null) return new Size(0, 0);
        double scale = IsPreview ? 1 : CellSize;
        return new Size(CurrentSprite.Width * scale, CurrentSprite.Height * scale);
    }

    public override void Render(DrawingContext context)
    {
        if (CurrentSprite == null) return;

        double scale = IsPreview ? 1 : CellSize;
        var grid = CurrentSprite.Grid;

        // Draw Pixels
        for (int y = 0; y < CurrentSprite.Height; y++)
        {
            for (int x = 0; x < CurrentSprite.Width; x++)
            {
                if (grid[y, x])
                {
                    context.DrawRectangle(Brushes.Black, null, new Rect(x * scale, y * scale, scale, scale));
                }
                else if (!IsPreview)
                {
                    context.DrawRectangle(Brushes.White, null, new Rect(x * scale, y * scale, scale, scale));
                }
            }
        }

        // Draw Gridlines
        if (!IsPreview)
        {
            var pen = new Pen(Brushes.LightGray, 0.5);
            for (int i = 0; i <= CurrentSprite.Width; i++)
            {
                context.DrawLine(pen, new Point(i * scale, 0), new Point(i * scale, CurrentSprite.Height * scale));
            }
            for (int i = 0; i <= CurrentSprite.Height; i++)
            {
                context.DrawLine(pen, new Point(0, i * scale), new Point(CurrentSprite.Width * scale, i * scale));
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsPreview) return;
        
        _isDrawing = true;
        HandlePointer(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDrawing)
        {
            HandlePointer(e);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDrawing = false;
        _lastToggledPixel = null;
    }

    private void HandlePointer(PointerEventArgs e)
    {
        if (CurrentSprite == null || IsPreview) return;

        var pos = e.GetPosition(this);
        int x = (int)(pos.X / CellSize);
        int y = (int)(pos.Y / CellSize);

        if (_lastToggledPixel == (x, y)) return;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.TogglePixel(x, y);
            _lastToggledPixel = (x, y);
            InvalidateVisual();
        }
    }
}
