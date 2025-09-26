using Editor.Helpers;
using Newtonsoft.Json;

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

    public void AddRecentFile(string filePath) =>
        CollectionHelpers.AddEntryToHistory(RecentFiles, filePath, MaxRecentFiles);

    public void AddCustomModel(string filePath) =>
        CollectionHelpers.AddEntryToHistory(CustomModels, filePath, MaxCustomModels);

    /// <summary>
    /// Model file to load if ModelType is ModelType.Model
    /// Can be either an entry from _builtInModels or from _editorConfiguration.CustomModels
    /// </summary>
    public string SelectedModelFilePath = "";
}
