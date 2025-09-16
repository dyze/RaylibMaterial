using Newtonsoft.Json;

namespace Editor.Configuration;

public class DataFileExplorerConfiguration
{
    /// <summary>
    /// Path of folder containing reference data files
    /// </summary>
    [JsonProperty("DataFolderPath")] public string? DataFolderPath;

    /// <summary>
    /// Used to restore view state of the folders
    /// </summary>
    [JsonProperty("OpenFolders")] public HashSet<string> OpenFolders = [];

    

    public bool IsFolderOpen(string path) => OpenFolders.Contains(path);

    public void AddRemoveOpenFolder(string path,
        bool add)
    {
        if (add)
            OpenFolders.Add(path);
        else
            OpenFolders.Remove(path);
    }
}