using System.Drawing;
using Editor.Helpers;
using Newtonsoft.Json;
using System.Numerics;

namespace Editor.Configuration;


public class EditorConfiguration
{
    [JsonProperty("DataFileExplorer")] public DataFileExplorerConfiguration DataFileExplorerConfiguration = new();

    [JsonProperty("WorkspaceConfiguration")] public WorkspaceConfiguration WorkspaceConfiguration = new();

    [JsonProperty("RecentFiles")] public List<string> RecentFiles = [];

    private const int MaxRecentFiles = 5;

    /// <summary>
    /// List of models once used by user
    /// </summary>
    [JsonProperty("CustomModels")] public List<string> CustomModels = [];

    private const int MaxCustomModels = 5;

    /// <summary>
    /// Model file to load if CurrentModelType is CurrentModelType.Model
    /// Can be either an entry from _builtInModels or from _editorConfiguration.CustomModels
    /// </summary>
    public string CurrentModelFilePath = "";

    public readonly Vector2 ScreenSize = new(1600, 900); // initial size of window

    public readonly Vector2 OutputSize = new(1600 / 2, 900 / 2);

    public enum BackgroundType
    {
        Cloud = 0,
        WildPark,
        Space
    }

    [JsonProperty("Background")] public BackgroundType Background { get; set; } = BackgroundType.Cloud;
    [JsonProperty("WindowPosition")] public Point WindowPosition { get; set; } = new Point(40, 40);
    [JsonProperty("MonitorIndex")] public int MonitorIndex { get; set; } = 0;

    public enum ModelType
    {
        Cube = 0,
        Plane,
        Sphere,
        Model
    }

    public ModelType CurrentModelType = ModelType.Cube;

    public void AddRecentFile(string filePath) =>
        CollectionHelpers.AddEntryToHistory(RecentFiles, filePath, MaxRecentFiles);

    public void AddCustomModel(string filePath) =>
        CollectionHelpers.AddEntryToHistory(CustomModels, filePath, MaxCustomModels);
}
