using Editor.Configuration;
using Editor.Windows;
using Library.Packaging;
using Raylib_cs;

namespace Editor;

public class EditorControllerData
{
    public DataFileExplorerData DataFileExplorerData { get; set; } = new();
    

    public MaterialPackage MaterialPackage = new();

    /// <summary>
    /// null if new material
    /// </summary>
    public string? MaterialFilePath { get; set; }

    public RenderTexture2D ViewTexture;

    public readonly Dictionary<EditorConfiguration.ModelType, ToolConfig> Tools = new()
    {
        { EditorConfiguration.ModelType.Cube, new ToolConfig("cube", "cube.png") },
        { EditorConfiguration.ModelType.Plane, new ToolConfig("plane", "plane.png") },
        { EditorConfiguration.ModelType.Sphere, new ToolConfig("sphere", "sphere.png") },
        { EditorConfiguration.ModelType.Model, new ToolConfig("model", "model.png") }
    };

    public List<string> BuiltInModels = [];


    public readonly Dictionary<EditorConfiguration.BackgroundType, BackgroundConfig> Backgrounds = new()
    {
        { EditorConfiguration.BackgroundType.Cloud, new BackgroundConfig("clouds", "clouds.jpg") },
        { EditorConfiguration.BackgroundType.WildPark, new BackgroundConfig("wild park", "wildpark.png") },
        { EditorConfiguration.BackgroundType.Space, new BackgroundConfig("space", "space.jpg") },
    };

    public EditorControllerData()
    {
    }
}