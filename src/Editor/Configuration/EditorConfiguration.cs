using System.Drawing;
using System.Numerics;
using Editor.Helpers;
using Library.Lighting;
using Newtonsoft.Json;
using Color = Raylib_cs.Color;

namespace Editor.Configuration;


public class EditorConfiguration
{
    [JsonProperty("ResourcesPath")] public string ResourcesPath;

    [JsonProperty("EditorResourcesPath")] public string EditorResourcesPath;

    [JsonProperty("FavouriteDirectories")] public List<string> FavouriteDirectories = [];
    [JsonIgnore] public const int MaxFavouriteDirectories = 5;

    [JsonIgnore] public string ResourceUiPath => $"{EditorResourcesPath}/ui";
    [JsonIgnore] public string ResourceSkyBoxesFolderPath => $"{ResourceUiPath}/skybox";
    [JsonIgnore] public string ResourceToolBoarFolderPath => $"{ResourceUiPath}/toolbar";

    [JsonIgnore] public string ResourceModelsPath => $"{ResourcesPath}/models";
    [JsonIgnore] public string ResourceShaderFolderPath => $"{ResourcesPath}/shaders";
    [JsonIgnore] public string ResourceImageFolderPath => $"{ResourcesPath}/images";

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


    [JsonProperty("SkyBox")] public string? SkyBox { get; set; }
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
        FourWhiteLights,
        YellowRedGreenBlue,
    }

    [JsonProperty("CurrentLightingPreset")] public LightingPreset CurrentLightingPreset = LightingPreset.SingleWhiteLight;

    public Dictionary<LightingPreset, List<Light>> LightingPresets = new()
    {
        { LightingPreset.SingleWhiteLight, [
                new Light(
                    LightType.Point,
                    new Vector3(-2, 1, -2),
                    Vector3.Zero,
                    Color.White,
                    4.0f)
            ]
        },
        { LightingPreset.FourWhiteLights, [
            new Light(LightType.Point,
                new Vector3(-2.0f, 1.0f, -2.0f),
                Vector3.Zero,
                Color.White,
                8f),
            new Light(LightType.Point,
                new Vector3(2.0f, 1.0f, 2.0f),
                Vector3.Zero,
                Color.White,
                8f),
            new Light(LightType.Point,
                new Vector3(-2.0f, 1.0f, 2.0f),
                Vector3.Zero,
                Color.White,
                8f),
            new Light(LightType.Point,
                new Vector3(2.0f, 1.0f, -2.0f),
                Vector3.Zero,
                Color.White,
                8f)
        ]},
        { LightingPreset.YellowRedGreenBlue, [
            new Light(LightType.Point,
                new Vector3(-1.0f, 1.0f, -2.0f),
                Vector3.Zero,
                Color.Yellow,
                4.0f),
            new Light(LightType.Point,
                new Vector3(2.0f, 1.0f, 1.0f),
                Vector3.Zero,
                Color.Green,
                3.3f),
            new Light(LightType.Point,
                new Vector3(-2.0f, 1.0f, 1.0f),
                Vector3.Zero,
                Color.Red,
                8.3f),
            new Light(LightType.Point,
                new Vector3(1.0f, 1.0f, -2.0f),
                Vector3.Zero,
                Color.Blue,
                2.0f)
            ]}
    };

    [JsonProperty("CameraSettings")] public CameraSettings CameraSettings = new();

    [JsonProperty("OutputDirectoryPath")] public string OutputDirectoryPath = "";


    public void AddRecentFile(string filePath) =>
        CollectionHelpers.AddEntryToHistory(RecentFiles, filePath, MaxRecentFiles);

    public void AddCustomModel(string filePath) =>
        CollectionHelpers.AddEntryToHistory(CustomModels, filePath, MaxCustomModels);

    public void AddToFavourite(string path)
    {
        if (FavouriteDirectories.Contains(path))
            return;

        if(FavouriteDirectories.Count >= MaxFavouriteDirectories)
            return;

        FavouriteDirectories.Add(path);
    }

    public void RemoveFavourite(int index)
    {
        if (index < 1)        // 0=editor resource path
            return;

        FavouriteDirectories.RemoveAt(index);
    }
}
