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

public class MainWindowViewModel : ViewModelBase
{
    private SpriteModel _currentSprite;
    private int _newWidth = 32;
    private int _newHeight = 32;

    public SpriteModel CurrentSprite
    {
        get => _currentSprite;
        set => RaiseAndSetIfChanged(ref _currentSprite, value);
    }

    public int NewWidth
    {
        get => _newWidth;
        set => RaiseAndSetIfChanged(ref _newWidth, Math.Clamp(value, 8, 1024));
    }

    public int NewHeight
    {
        get => _newHeight;
        set => RaiseAndSetIfChanged(ref _newHeight, Math.Clamp(value, 8, 64));
    }

    public ICommand NewCommand { get; }
    public ICommand InvertCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ExportCommand { get; }

    public MainWindowViewModel()
    {
        _currentSprite = new SpriteModel(_newWidth, _newHeight);
        
        NewCommand = new SimpleCommand(CreateNew);
        InvertCommand = new SimpleCommand(Invert);
        SaveCommand = new SimpleCommand(async () => await SaveAsync());
        LoadCommand = new SimpleCommand(async () => await LoadAsync());
        ExportCommand = new SimpleCommand(async () => await ExportAsync());
    }

    private void CreateNew()
    {
        CurrentSprite = new SpriteModel(NewWidth, NewHeight);
    }

    private void Invert()
    {
        CurrentSprite.Invert();
        OnPropertyChanged(nameof(CurrentSprite));
    }

    private async Task SaveAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Sprite Sheet",
            FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
        });

        if (file != null)
        {
            CurrentSprite.SaveJson(file.Path.LocalPath);
        }
    }

    private async Task LoadAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Sprite Sheet",
            FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var sprite = SpriteModel.LoadJson(files[0].Path.LocalPath);
            if (sprite != null)
            {
                CurrentSprite = sprite;
                NewWidth = sprite.Width;
                NewHeight = sprite.Height;
            }
        }
    }

    private async Task ExportAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PNG",
            FileTypeChoices = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
        });

        if (file != null)
        {
            ExportToPng(file.Path.LocalPath, 8); // Default 8x upscale
        }
    }

    private unsafe void ExportToPng(string path, int scale)
    {
        int w = CurrentSprite.Width;
        int h = CurrentSprite.Height;
        var pixelSize = new PixelSize(w * scale, h * scale);
        using var bitmap = new WriteableBitmap(pixelSize, new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Premul);
        
        using (var buffer = bitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)buffer.Address;
                for (int y = 0; y < h * scale; y++)
                {
                    int srcY = y / scale;
                    for (int x = 0; x < w * scale; x++)
                    {
                        int srcX = x / scale;
                        bool isBlack = CurrentSprite.Grid[srcY, srcX];
                        ptr[y * (buffer.RowBytes / 4) + x] = isBlack ? 0xFF000000 : 0xFFFFFFFF;
                    }
                }
            }
        }
        bitmap.Save(path);
    }

    private TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public void TogglePixel(int x, int y)
    {
        if (x >= 0 && x < CurrentSprite.Width && y >= 0 && y < CurrentSprite.Height)
        {
            CurrentSprite.Grid[y, x] = !CurrentSprite.Grid[y, x];
            OnPropertyChanged(nameof(CurrentSprite));
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