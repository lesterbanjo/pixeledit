using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using pixeledit2.Models;

namespace pixeledit2.ViewModels;

public enum DrawingTool
{
    Pen,
    Eraser
}

public class MainWindowViewModel : ViewModelBase
{
    private SpriteModel _currentSprite;
    private decimal _newWidth = 32;
    private decimal _newHeight = 32;
    private DrawingTool _currentTool = DrawingTool.Pen;

    public SpriteModel CurrentSprite
    {
        get => _currentSprite;
        set => RaiseAndSetIfChanged(ref _currentSprite, value);
    }

    public DrawingTool CurrentTool
    {
        get => _currentTool;
        set => RaiseAndSetIfChanged(ref _currentTool, value);
    }

    public decimal NewWidth
    {
        get => _newWidth;
        set
        {
            if (RaiseAndSetIfChanged(ref _newWidth, Math.Clamp(value, 8, 1024)))
            {
                ApplyDimensions();
            }
        }
    }

    public decimal NewHeight
    {
        get => _newHeight;
        set
        {
            if (RaiseAndSetIfChanged(ref _newHeight, Math.Clamp(value, 8, 64)))
            {
                ApplyDimensions();
            }
        }
    }

    public ICommand NewCommand { get; }
    public ICommand InvertCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ExportCommand { get; }

    public MainWindowViewModel()
    {
        _currentSprite = new SpriteModel((int)_newWidth, (int)_newHeight);
        
        NewCommand = new SimpleCommand(CreateNew);
        InvertCommand = new SimpleCommand(Invert);
        SaveCommand = new SimpleCommand(async () => await SaveAsync());
        LoadCommand = new SimpleCommand(async () => await LoadAsync());
        ExportCommand = new SimpleCommand(async () => await ExportAsync());
    }

    private void CreateNew()
    {
        CurrentSprite = new SpriteModel((int)NewWidth, (int)NewHeight);
    }

    private void ApplyDimensions()
    {
        CurrentSprite = CurrentSprite.Resized((int)NewWidth, (int)NewHeight);
    }

    private void Invert()
    {
        CurrentSprite = CurrentSprite.Inverted();
    }

    private async Task SaveAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var suggestedFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Sprite Sheet",
            FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            SuggestedStartLocation = suggestedFolder,
            SuggestedFileName = "sprite.json"
        });

        if (file != null)
        {
            try
            {
                using var stream = await file.OpenWriteAsync();
                CurrentSprite.SaveJson(stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
            }
        }
    }

    private async Task LoadAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var suggestedFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Sprite Sheet",
            FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder
        });

        if (files.Count > 0)
        {
            try
            {
                using var stream = await files[0].OpenReadAsync();
                var sprite = SpriteModel.LoadJson(stream);
                if (sprite != null)
                {
                    CurrentSprite = sprite;
                    NewWidth = sprite.Width;
                    NewHeight = sprite.Height;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
            }
        }
    }

    private async Task ExportAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var suggestedFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PNG",
            FileTypeChoices = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } },
            SuggestedStartLocation = suggestedFolder,
            SuggestedFileName = "sprite.png"
        });

        if (file != null)
        {
            try
            {
                using var stream = await file.OpenWriteAsync();
                ExportToPng(stream, 8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
            }
        }
    }

    private unsafe void ExportToPng(Stream stream, int scale)
    {
        int w = CurrentSprite.Width;
        int h = CurrentSprite.Height;
        var pixelSize = new PixelSize(w * scale, h * scale);
        
        // Ensure we are using a consistent pixel format
        using var bitmap = new WriteableBitmap(pixelSize, new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Premul);
        
        using (var buffer = bitmap.Lock())
        {
            uint* ptr = (uint*)buffer.Address;
            int stride = buffer.RowBytes / 4;
            
            for (int y = 0; y < h * scale; y++)
            {
                int srcY = y / scale;
                int rowOffset = y * stride;
                for (int x = 0; x < w * scale; x++)
                {
                    int srcX = x / scale;
                    bool isBlack = CurrentSprite.Grid[srcY, srcX];
                    // Rgba8888 on Little Endian uint is 0xAABBGGRR
                    // Black: A=FF, B=00, G=00, R=00 -> 0xFF000000
                    // White: A=FF, B=FF, G=FF, R=FF -> 0xFFFFFFFF
                    ptr[rowOffset + x] = isBlack ? 0xFF000000 : 0xFFFFFFFF;
                }
            }
        }
        bitmap.Save(stream);
    }

    private TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public void PaintPixel(int x, int y)
    {
        if (x >= 0 && x < CurrentSprite.Width && y >= 0 && y < CurrentSprite.Height)
        {
            bool newValue = CurrentTool == DrawingTool.Pen;
            CurrentSprite = CurrentSprite.WithPixel(x, y, newValue);
        }
    }

    private class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        public SimpleCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}