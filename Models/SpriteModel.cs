using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pixeledit2.Models;

public class SpriteModel
{
    public int Width { get; set; }
    public int Height { get; set; }
    
    [JsonIgnore]
    public bool[,] Grid { get; set; }

    [JsonPropertyName("Grid")]
    public bool[][] JaggedGrid
    {
        get
        {
            var jagged = new bool[Height][];
            for (int y = 0; y < Height; y++)
            {
                jagged[y] = new bool[Width];
                for (int x = 0; x < Width; x++)
                {
                    jagged[y][x] = Grid[y, x];
                }
            }
            return jagged;
        }
        set
        {
            Grid = new bool[Height, Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Grid[y, x] = value[y][x];
                }
            }
        }
    }

    public SpriteModel() { }

    public SpriteModel(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new bool[height, width];
    }

    public SpriteModel Inverted()
    {
        var newSprite = new SpriteModel(Width, Height);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                newSprite.Grid[y, x] = !Grid[y, x];
            }
        }
        return newSprite;
    }

    public SpriteModel Resized(int newWidth, int newHeight)
    {
        var newSprite = new SpriteModel(newWidth, newHeight);
        int copyWidth = Math.Min(Width, newWidth);
        int copyHeight = Math.Min(Height, newHeight);

        for (int y = 0; y < copyHeight; y++)
        {
            for (int x = 0; x < copyWidth; x++)
            {
                newSprite.Grid[y, x] = Grid[y, x];
            }
        }
        return newSprite;
    }

    public SpriteModel Toggled(int x, int y)
    {
        var newSprite = new SpriteModel(Width, Height);
        Array.Copy(Grid, newSprite.Grid, Grid.Length);
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            newSprite.Grid[y, x] = !newSprite.Grid[y, x];
        }
        return newSprite;
    }

    public SpriteModel WithPixel(int x, int y, bool value)
    {
        var newSprite = new SpriteModel(Width, Height);
        Array.Copy(Grid, newSprite.Grid, Grid.Length);
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            newSprite.Grid[y, x] = value;
        }
        return newSprite;
    }

    public void SaveJson(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        writer.Write(json);
    }

    public static SpriteModel? LoadJson(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<SpriteModel>(json);
    }
}
