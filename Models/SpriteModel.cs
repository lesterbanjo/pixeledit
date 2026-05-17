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

    public void Invert()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Grid[y, x] = !Grid[y, x];
            }
        }
    }

    public void SaveJson(string filePath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static SpriteModel? LoadJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SpriteModel>(json);
    }
}
