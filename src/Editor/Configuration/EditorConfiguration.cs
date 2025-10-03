using System.Drawing;
using Editor.Helpers;
using Newtonsoft.Json;

namespace Editor.Configuration;


public class EditorConfiguration
{
    [JsonProperty("DataFileExplorer")] public DataFileExplorerConfiguration DataFileExplorerConfiguration = new();

    public string ResourcesPath => DataFileExplorerConfiguration.DataFolderPath;

    public string ResourceUiPath => $"{ResourcesPath}/ui";
    public string ResourceSkyBoxesFolderPath => $"{ResourceUiPath}/skybox";
    public string ResourceToolBoarFolderPath => $"{ResourceUiPath}/toolbar";


    public string ResourceModelsPath => $"{ResourcesPath}/models";
    public string ResourceShaderFolderPath => $"{ResourcesPath}/shaders";
    private string ResourceImageFolderPath => $"{ResourcesPath}/images";

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


    [JsonProperty("Background")] public string Background { get; set; }
    [JsonProperty("WindowPosition")] public Point WindowPosition { get; set; } = new(40, 40);
    [JsonProperty("WindowSize")] public Size WindowSize = new(1600, 900);
    [JsonProperty("MonitorIndex")] public int MonitorIndex { get; set; } = 0;
    [JsonProperty("IsInDebugMode")] public bool IsInDebugMode { get; set; }

    public enum ModelType
    {
        Cube = 0,
        Plane,
        Sphere,
        Model
    }

    [JsonProperty("CurrentModelType")] public ModelType CurrentModelType = ModelType.Cube;

    [JsonProperty("ModelScale")] public float ModelScale = 1f;

    public enum LightingPreset
    {
        SingleWhiteLight = 0,
        YellowRedGreenBlue,
    }

    [JsonProperty("CurrentLightingPreset")] public LightingPreset CurrentLightingPreset = LightingPreset.SingleWhiteLight;

    [JsonProperty("CameraSettings")] public CameraSettings CameraSettings = new();

    [JsonProperty("OutputDirectoryPath")] public string OutputDirectoryPath = "";


    public void AddRecentFile(string filePath) =>
        CollectionHelpers.AddEntryToHistory(RecentFiles, filePath, MaxRecentFiles);

    public void AddCustomModel(string filePath) =>
        CollectionHelpers.AddEntryToHistory(CustomModels, filePath, MaxCustomModels);
}
